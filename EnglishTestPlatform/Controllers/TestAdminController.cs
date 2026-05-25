using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Services;
using EnglishTestPlatform.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EnglishTestPlatform.Models;
using Data;

namespace EnglishTestPlatform.Controllers
{
    public class TestAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public TestAdminController(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        /// <summary>
        /// Список всех тестов
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var tests = await _context.Tests
                .Include(t => t.File)
                .Include(t => t.Section)
                .ToListAsync();

            // Отладка: выводим информацию о тестах
            Console.WriteLine($"=== Найдено тестов: {tests.Count} ===");
            foreach (var test in tests)
            {
                Console.WriteLine($"Test ID: {test.Id}, File: {test.File?.Name ?? "NULL"}, FileId: {test.FileId}");
            }

            return View(tests);
        }

        /// <summary>
        /// Форма создания теста
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(int? sectionId)
        {
            var model = new TestViewModel
            {
                SectionId = sectionId,
                Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync()
            };
            return View(model);
        }

        /// <summary>
        /// Обработка создания теста
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestViewModel model)
        {
            // Удаляем лишние ошибки валидации
            ModelState.Remove("Id");
            ModelState.Remove("Sections");
            ModelState.Remove("File");
            ModelState.Remove("Questions");

            // Добавляем отладку
            Console.WriteLine($"=== CREATE TEST ===");
            Console.WriteLine($"Model Name: {model.Name}");
            Console.WriteLine($"Model SectionId: {model.SectionId}");
            Console.WriteLine($"Has File: {model.File != null}");
            Console.WriteLine($"Has JsonContent: {!string.IsNullOrEmpty(model.JsonContent)}");
            Console.WriteLine($"Has Questions: {model.Questions?.Count > 0}");

            // Проверяем валидацию
            if (ModelState.IsValid)
            {
                string? jsonContent = null;

                try
                {
                    // Вариант 1: Загрузка файла
                    if (model.File != null)
                    {
                        if (Path.GetExtension(model.File.FileName).ToLower() != ".json")
                        {
                            ModelState.AddModelError("File", "Загрузите файл в формате .json");
                            model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                            return View(model);
                        }

                        using (var reader = new StreamReader(model.File.OpenReadStream()))
                        {
                            jsonContent = await reader.ReadToEndAsync();
                        }
                    }
                    // Вариант 2: Вставка JSON текстом
                    else if (!string.IsNullOrEmpty(model.JsonContent))
                    {
                        jsonContent = model.JsonContent;
                    }
                    // Вариант 3: Создание через форму с вопросами
                    else if (model.Questions != null && model.Questions.Count > 0)
                    {
                        var testModel = new TestModel
                        {
                            TestTitle = model.Name,
                            Questions = ConvertQuestions(model.Questions)
                        };
                        jsonContent = JsonSerializer.Serialize(testModel, new JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Загрузите файл, вставьте JSON или добавьте вопросы через форму");
                        model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                        return View(model);
                    }

                    // Проверяем валидность JSON
                    try
                    {
                        JsonSerializer.Deserialize<TestModel>(jsonContent);
                    }
                    catch (Exception jsonEx)
                    {
                        ModelState.AddModelError("", $"Неверный формат JSON: {jsonEx.Message}");
                        model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                        return View(model);
                    }

                    // Сохраняем JSON в файл
                    var fileName = $"{Guid.NewGuid()}_{model.Name}.json".Replace(" ", "_");
                    var uploadsDir = Path.Combine(_env.ContentRootPath, "Source", "Tests");
                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);
                    
                    var filePath = Path.Combine(uploadsDir, fileName);
                    await System.IO.File.WriteAllTextAsync(filePath, jsonContent);

                    var savedFile = new FileP
                    {
                        Name = model.Name,
                        FilePath = Path.Combine("Source", "Tests", fileName)
                    };

                    // Создаем сущность Test
                    var test = new Test
                    {
                        File = savedFile,
                        FileId = savedFile.Id,
                        SectionId = model.SectionId
                    };

                    _context.Files.Add(savedFile);
                    _context.Tests.Add(test);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Test saved with ID: {test.Id}");

                    TempData["Success"] = $"Тест «{model.Name}» успешно добавлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
                }
            }

            // Выводим ошибки для отладки
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== ModelState Errors ===");
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        /// <summary>
        /// Конвертация вопросов из формы в модель
        /// </summary>
        private List<Question> ConvertQuestions(List<QuestionFormModel> formQuestions)
        {
            var questions = new List<Question>();

            foreach (var fq in formQuestions)
            {
                switch (fq.Type)
                {
                    case "multiple_choice":
                        var correctIndex = fq.CorrectAnswers.IndexOf(true);
                        var mcq = new MultipleChoiceQuestion
                        {
                            Type = "multiple_choice",
                            Text = fq.Text,
                            Explanation = fq.Explanation,
                            Options = fq.Options.Where((_, i) => i < fq.CorrectAnswers.Count).ToList(),
                            Correct = correctIndex >= 0 ? fq.Options[correctIndex] : string.Empty // Сохраняем текст ответа, а не индекс
                        };
                        questions.Add(mcq);
                        break;

                    case "multiple_select":
                        var msq = new MultipleSelectQuestion
                        {
                            Type = "multiple_select",
                            Text = fq.Text,
                            Explanation = fq.Explanation,
                            Options = fq.Options.ToList(),
                            Correct = fq.Options.Where((_, i) => i < fq.CorrectAnswers.Count && fq.CorrectAnswers[i]).ToList()
                        };
                        questions.Add(msq);
                        break;

                    case "matching":
                        // Объединяем LeftItems и RightItems в пары
                        var pairs = new List<MatchingPair>();
                        var minCount = Math.Min(fq.LeftItems.Count, fq.RightItems.Count);
                        for (int i = 0; i < minCount; i++)
                        {
                            pairs.Add(new MatchingPair
                            {
                                Left = fq.LeftItems[i],
                                Right = fq.RightItems[i]
                            });
                        }

                        var mq = new MatchingQuestion
                        {
                            Type = "matching",
                            Text = fq.Text,
                            Explanation = fq.Explanation,
                            Pairs = pairs
                        };
                        questions.Add(mq);
                        break;

                    case "fill_in":
                        var fiq = new FillInQuestion
                        {
                            Type = "fill_in",
                            Text = fq.Text,
                            Explanation = fq.Explanation,
                            Answers = fq.CorrectAnswersList.Where(x => !string.IsNullOrEmpty(x)).ToList()
                        };
                        questions.Add(fiq);
                        break;
                }
            }

            return questions;
        }

        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Форма редактирования теста
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var test = await _context.Tests
                .Include(t => t.File)
                .Include(t => t.Section)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            var model = new TestViewModel
            {
                Id = test.Id,
                Name = test.File?.Name ?? "",
                SectionId = test.SectionId,
                ExistingFileId = test.FileId, // Сохраняем ID существующего файла
                Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync()
            };

            return View(model);
        }

        /// <summary>
        /// Обработка редактирования теста
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TestViewModel model, IFormFile? file)
        {
            if (id != model.Id) return NotFound();

            var test = await _context.Tests
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            ModelState.Remove("Sections");
            ModelState.Remove("File");
            ModelState.Remove("ExistingFileId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Если загружен новый файл
                    if (file != null)
                    {
                        if (Path.GetExtension(file.FileName).ToLower() != ".json")
                        {
                            ModelState.AddModelError("File", "Файл должен быть .json");
                        }
                        else
                        {
                            // Удаляем старый файл
                            if (test.File != null)
                            {
                                _fileService.DeleteFile(test.File);
                                _context.Files.Remove(test.File);
                            }

                            // Сохраняем новый
                            var newFile = await _fileService.SaveFileAsync(file, "Tests");
                            test.File = newFile;
                            test.FileId = newFile.Id;
                        }
                    }
                    // Если файл не загружен, но у модели есть имя - обновляем только название в File
                    else if (!string.IsNullOrEmpty(model.Name) && test.File != null)
                    {
                        // Обновляем только название файла в БД, сам файл не меняем
                        test.File.Name = model.Name;
                    }

                    // Обновляем раздел
                    test.SectionId = model.SectionId;

                    if (ModelState.IsValid)
                    {
                        await _context.SaveChangesAsync();
                        TempData["Success"] = $"Тест «{model.Name}» успешно обновлен!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обновлении: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при обновлении: {ex.Message}");
                }
            }

            model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        /// <summary>
        /// Удаление теста
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var test = await _context.Tests
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test != null)
            {
                _fileService.DeleteFile(test.File);
                _context.Files.Remove(test.File);
                _context.Tests.Remove(test);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Тест успешно удален!";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Управление связями теории с тестом
        /// </summary>
        public async Task<IActionResult> ManageTheories(int id)
        {
            var test = await _context.Tests
                .Include(t => t.TheoryTestRelations)
                    .ThenInclude(ttr => ttr.Theory)
                    .ThenInclude(th => th.File)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null) return NotFound();

            var allTheories = await _context.Theories
                .Include(th => th.File)
                .ToListAsync();

            ViewBag.AllTheories = allTheories;
            return View(test);
        }

        /// <summary>
        /// Добавление связи с теорией
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddTheory(int testId, int theoryId)
        {
            // Проверяем, существует ли уже связь
            var exists = await _context.TheoryTestRelations
                .AnyAsync(r => r.TestId == testId && r.TheoryId == theoryId);

            if (!exists)
            {
                var relation = new TheoryTestRelation { TestId = testId, TheoryId = theoryId };
                _context.TheoryTestRelations.Add(relation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Теория успешно привязана к тесту!";
            }

            return RedirectToAction(nameof(ManageTheories), new { id = testId });
        }

        /// <summary>
        /// Удаление связи с теорией
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveTheory(int testId, int theoryId)
        {
            var relation = await _context.TheoryTestRelations
                .FirstOrDefaultAsync(r => r.TestId == testId && r.TheoryId == theoryId);

            if (relation != null)
            {
                _context.TheoryTestRelations.Remove(relation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Связь с теорией удалена!";
            }

            return RedirectToAction(nameof(ManageTheories), new { id = testId });
        }
    }
}