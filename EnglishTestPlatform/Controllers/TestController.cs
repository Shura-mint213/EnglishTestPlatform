using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using EnglishTestPlatform.Services;
using Microsoft.AspNetCore.Mvc;
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

        public TestController(ITestLoaderService testLoader, TestEvaluatorService evaluator)
        {
            _testLoader = testLoader;
            _evaluator = evaluator;
        }

        /// <summary>
        /// Отображает страницу с вопросами теста
        /// </summary>
        /// <param name="testName">Имя файла теста (без расширения)</param>
        /// <returns>Представление с вопросами теста</returns>
        [HttpGet]
        [Route("Take")]
        public IActionResult Take(string testName)
        {
            // Проверяем, что имя теста указано
            if (string.IsNullOrEmpty(testName))
                return RedirectToAction("Index", "Home");

            try
            {
                // Загружаем тест из JSON файла
                var test = _testLoader.LoadTest(testName);

                // Передаём имя теста в представление через ViewBag
                ViewBag.TestName = testName;

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
        public IActionResult SubmitForm(string testName, List<UserAnswerForm> userAnswers)
        {
            try
            {
                // Валидация входных данных
                if (string.IsNullOrEmpty(testName))
                    return BadRequest("Имя теста не указано");

                if (userAnswers == null || userAnswers.Count == 0)
                    return BadRequest("Ответы не были получены");

                // Загружаем тест из JSON файла
                var test = _testLoader.LoadTest(testName);

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
    }
}