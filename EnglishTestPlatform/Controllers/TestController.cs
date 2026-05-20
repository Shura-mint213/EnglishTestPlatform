using Data;
using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using EnglishTestPlatform.Services;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnglishTestPlatform.Controllers
{
    /// <summary>
    /// Контроллер для управления тестами
    /// </summary>
    public class TestController : Controller
    {
        private readonly ITestLoaderService _testLoader;
        private readonly TestEvaluatorService _evaluator;
        private readonly AppDbContext _context;

        public TestController(ITestLoaderService testLoader, 
            TestEvaluatorService evaluator,
            AppDbContext appDbContext)
        {
            _testLoader = testLoader;
            _evaluator = evaluator;
            _context = appDbContext;
        }

        /// <summary>
        /// Отображает страницу с вопросами теста
        /// </summary>
        /// <param name="testName">Имя файла теста (без расширения)</param>
        /// <param name="id">ID теста в базе данных</param>
        /// <returns>Представление с вопросами теста</returns>
        [HttpGet]
        [Route("Take")]
        public async Task<IActionResult> Take(string testName, int? id)
        {
            // Проверяем, что имя теста или ID указаны
            if (string.IsNullOrEmpty(testName) && !id.HasValue)
                return RedirectToAction("Index", "Home");

            try
            {
                TestModel test;

                // Если передан ID, загружаем тест из БД (с контентом или файлом)
                if (id.HasValue)
                {
                    var testEntity = await _context.Tests
                        .Include(t => t.File)
                        .FirstOrDefaultAsync(t => t.Id == id.Value);

                    if (testEntity == null)
                        return NotFound();

                    // Если есть контент (введен вручную), используем его
                    if (!string.IsNullOrEmpty(testEntity.Content))
                    {
                        test = JsonSerializer.Deserialize<TestModel>(testEntity.Content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    // Иначе загружаем из файла
                    else if (testEntity.File != null)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), testEntity.File.FilePath);
                        if (!System.IO.File.Exists(filePath))
                            throw new FileNotFoundException($"Файл теста не найден: {filePath}");

                        var json = await System.IO.File.ReadAllTextAsync(filePath);
                        test = JsonSerializer.Deserialize<TestModel>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new QuestionConverterService() }
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException("Содержимое теста не найдено");
                    }

                    ViewBag.TestName = testName ?? testEntity.Name ?? $"Test_{id.Value}";
                    ViewBag.TestId = id.Value;
                }
                else
                {
                    // Загружаем тест из JSON файла по имени (старый способ)
                    test = await _testLoader.LoadTestFromDatabaseAsync(testName);
                    ViewBag.TestName = testName;
                }

                return View(test);
            }
            catch (Exception ex)
            {
                // В случае ошибки показываем страницу с ошибкой
                Console.WriteLine($"Ошибка загрузки теста {testName}: {ex.Message}");
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        /// <summary>
        /// Обрабатывает отправку формы с ответами пользователя
        /// </summary>
        /// <param name="testName">Имя теста</param>
        /// <param name="userAnswers">Список ответов пользователя из формы</param>
        /// <returns>Перенаправление на страницу с результатами</returns>
        [HttpPost]
        [Route("Test/SubmitForm")]
        public async Task<IActionResult> SubmitForm(string testName, List<UserAnswerForm> userAnswers)
        {
            try
            {
                // Валидация входных данных
                if (string.IsNullOrEmpty(testName))
                    return BadRequest("Имя теста не указано");

                if (userAnswers == null || userAnswers.Count == 0)
                    return BadRequest("Ответы не были получены");

                // Загружаем тест из JSON файла
                var test = await _testLoader.LoadTestFromDatabaseAsync(testName);

                // Преобразуем ответы из формы в формат, понятный evaluator'у
                var userAnswersList = ConvertFormAnswersToUserAnswers(test, userAnswers);

                // Вычисляем результат теста
                var result = _evaluator.Evaluate(test, userAnswersList);

                // Сохраняем результат в TempData с уникальным ключом
                string resultId = Guid.NewGuid().ToString();
                string resultJson = JsonSerializer.Serialize(result);
                TempData[resultId] = resultJson;

                // Перенаправляем на страницу с результатами
                return RedirectToAction("ShowResult", new { id = resultId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке теста: {ex.Message}");
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        /// <summary>
        /// Отображает страницу с результатами теста
        /// </summary>
        /// <param name="id">Уникальный идентификатор результата в TempData</param>
        /// <returns>Представление с результатами теста</returns>
        [HttpGet]
        [Route("Test/ShowResult")]
        public IActionResult ShowResult(string id)
        {
            // Проверяем, что ID указан
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index", "Home");

            // Извлекаем результат из TempData
            string resultJson = TempData[id] as string;

            if (string.IsNullOrEmpty(resultJson))
                return RedirectToAction("Index", "Home");

            // Удаляем результат из TempData после прочтения
            TempData.Remove(id);

            // Десериализуем результат
            var result = JsonSerializer.Deserialize<ResultViewModel>(resultJson);

            if (result == null)
                return RedirectToAction("Index", "Home");

            return View("Result", result);
        }

        /// <summary>
        /// Преобразует ответы из формы в формат UserAnswer
        /// </summary>
        /// <param name="test">Модель теста</param>
        /// <param name="formAnswers">Ответы из формы</param>
        /// <returns>Список ответов в формате UserAnswer</returns>
        private List<UserAnswer> ConvertFormAnswersToUserAnswers(TestModel test, List<UserAnswerForm> formAnswers)
        {
            var userAnswersList = new List<UserAnswer>();

            foreach (var formAnswer in formAnswers)
            {
                // Получаем тип вопроса
                var question = test.Questions[formAnswer.QuestionIndex];
                object answer = null;

                // В зависимости от типа вопроса формируем ответ
                switch (question)
                {
                    case MultipleChoiceQuestion:
                        // Одиночный выбор - просто строка
                        answer = formAnswer.Answer;
                        break;

                    case MultipleSelectQuestion:
                        // Множественный выбор - список строк
                        answer = formAnswer.SelectedOptions ?? new List<string>();
                        break;

                    case MatchingQuestion:
                        // Сопоставление - словарь "ключ -> значение"
                        var matches = new Dictionary<string, string>();
                        if (formAnswer.Matches != null)
                        {
                            foreach (var match in formAnswer.Matches)
                            {
                                if (!string.IsNullOrEmpty(match.Value))
                                {
                                    matches[match.Key] = match.Value;
                                }
                            }
                        }
                        answer = matches;
                        break;

                    case FillInQuestion:
                        // Ввод текста - строка
                        answer = formAnswer.Answer ?? "";
                        break;
                }

                userAnswersList.Add(new UserAnswer
                {
                    questionIndex = formAnswer.QuestionIndex,
                    answer = answer
                });
            }

            return userAnswersList;
        }

        /// <summary>
        /// Получает теорию, связанную с тестом (для шпаргалки)
        /// </summary>
        /// <param name="testName">Имя теста</param>
        /// <returns>HTML теорию или null</returns>
        [HttpGet]
        [Route("Test/GetTheoryCheatSheet")]
        public async Task<IActionResult> GetTheoryCheatSheet(string testName)
        {
            try
            {
                Console.WriteLine($"=== GetTheoryCheatSheet ===");
                Console.WriteLine($"testName: {testName}");

                if (string.IsNullOrEmpty(testName))
                {
                    return Json(new { hasTheory = false, error = "testName is empty" });
                }

                // Сначала загружаем все тесты с файлами (без фильтрации)
                var allTests = await _context.Tests
                    .Include(t => t.File)
                    .Include(t => t.TheoryTestRelations)
                        .ThenInclude(ttr => ttr.Theory)
                        .ThenInclude(th => th.File)
                    .ToListAsync();

                Console.WriteLine($"Всего тестов в БД: {allTests.Count}");

                // Теперь фильтруем в памяти (клиентская оценка)
                Test selectedTest = null;
                foreach (var test in allTests)
                {
                    if (test.File == null) continue;

                    var fileName = Path.GetFileNameWithoutExtension(test.File.FilePath);
                    Console.WriteLine($"Проверяем тест: Id={test.Id}, FilePath={test.File.FilePath}, FileName={fileName}");

                    if (fileName == testName)
                    {
                        selectedTest = test;
                        Console.WriteLine($"Тест найден: {test.Id}");
                        break;
                    }

                    // Также пробуем поиск по содержанию пути
                    if (test.File.FilePath.Contains(testName))
                    {
                        selectedTest = test;
                        Console.WriteLine($"Тест найден по содержанию: {test.Id}");
                        break;
                    }
                }

                if (selectedTest == null)
                {
                    Console.WriteLine($"Тест не найден для testName: {testName}");
                    return Json(new { hasTheory = false, error = $"Тест '{testName}' не найден в БД" });
                }

                Console.WriteLine($"Найден тест: ID={selectedTest.Id}, Name={selectedTest.File?.Name}");
                Console.WriteLine($"Связанных теорий: {selectedTest.TheoryTestRelations?.Count ?? 0}");

                if (selectedTest.TheoryTestRelations == null || !selectedTest.TheoryTestRelations.Any())
                {
                    Console.WriteLine("Нет связанных теорий");
                    return Json(new { hasTheory = false, error = "Нет связанных теорий" });
                }

                // Берем первую связанную теорию
                var theory = selectedTest.TheoryTestRelations.First().Theory;
                if (theory == null)
                {
                    Console.WriteLine("Теория null");
                    return Json(new { hasTheory = false, error = "Теория не найдена" });
                }

                Console.WriteLine($"Найдена теория: ID={theory.Id}, Name={theory.Name}");
                Console.WriteLine($"Файл теории: {theory.File?.FilePath}");

                // Читаем содержимое .md файла
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), theory.File.FilePath);
                Console.WriteLine($"Полный путь к файлу: {filePath}");

                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine($"Файл не найден: {filePath}");
                    return Json(new { hasTheory = false, error = "Файл теории не найден" });
                }

                var markdown = await System.IO.File.ReadAllTextAsync(filePath);
                Console.WriteLine($"Markdown прочитан, длина: {markdown.Length}");

                // Конвертируем Markdown в HTML
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                var htmlContent = Markdown.ToHtml(markdown, pipeline);

                Console.WriteLine("Теория успешно загружена");

                return Json(new
                {
                    hasTheory = true,
                    theoryName = theory.Name,
                    content = htmlContent
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки теории: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { hasTheory = false, error = ex.Message });
            }
        }
    }
}