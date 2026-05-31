using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Services;
using EnglishTestPlatform.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Markdig;
using Data;

namespace EnglishTestPlatform.Controllers
{
    public class TheoryAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _env;

        public TheoryAdminController(AppDbContext context, IFileService fileService, IWebHostEnvironment env)
        {
            _context = context;
            _fileService = fileService;
            _env = env;
        }

        /// <summary>
        /// Список всех теоретических материалов
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var theories = await _context.Theories
                .Include(t => t.File)
                .Include(t => t.Section)
                .ToListAsync();
            return View(theories);
        }

        /// <summary>
        /// Форма создания теории
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(int? sectionId)
        {
            var model = new TheoryViewModel
            {
                SectionId = sectionId,
                Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync()
            };
            return View(model);
        }

        /// <summary>
        /// Предпросмотр Markdown
        /// </summary>
        [HttpPost]
        public IActionResult Preview(string markdownContent)
        {
            if (string.IsNullOrEmpty(markdownContent))
                return Content("");

            try
            {
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions() 
                    .UsePipeTables()
                    .UseBootstrap()
                    .Build();

                var html = Markdown.ToHtml(markdownContent, pipeline);
                return Content(html);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Ошибка при обработке Markdown: {ex.Message}</div>");
            }
        }

        /// <summary>
        /// Обработка создания теории
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TheoryViewModel model)
        {

            // Удаляем лишние ошибки
            ModelState.Remove("Sections");
            ModelState.Remove("ExistingFileId");

            // ✅ Проверяем наличие контента ДО ModelState.IsValid
            bool hasFile = model.File != null;
            bool hasMarkdown = !string.IsNullOrWhiteSpace(model.MarkdownContent);

            if (!hasFile && !hasMarkdown)
            {
                ModelState.AddModelError("MarkdownContent", "Загрузите файл или введите контент в редакторе Markdown");
            }

            if (!ModelState.IsValid)
            {
                model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            string? markdownContent = null;

            try
            {

                if (hasFile)
                {
                    if (Path.GetExtension(model.File.FileName).ToLower() != ".md")
                    {
                        ModelState.AddModelError("File", "Загрузите файл в формате .md");
                        model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                        return View(model);
                    }
                    using var reader = new StreamReader(model.File.OpenReadStream());
                    markdownContent = await reader.ReadToEndAsync();
                }
                else if (hasMarkdown)
                {
                    markdownContent = model.MarkdownContent;
                }
                else
                {
                    ModelState.AddModelError("", "Загрузите файл или введите контент в редакторе Markdown");
                    model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                    return View(model);
                }

                // Сохраняем Markdown в файл
                var fileName = $"{Guid.NewGuid()}_{model.Name}.md".Replace(" ", "_");
                var uploadsDir = Path.Combine(_env.ContentRootPath, "Source", "Theories");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var filePath = Path.Combine(uploadsDir, fileName);
                await System.IO.File.WriteAllTextAsync(filePath, markdownContent);

                var savedFile = new FileP
                {
                    Name = model.Name,
                    FilePath = Path.Combine("Source", "Theories", fileName)
                };

                // Создаем сущность Theory
                var theory = new Theory
                {
                    Name = model.Name,
                    File = savedFile,
                    FileId = savedFile.Id,
                    SectionId = model.SectionId
                };

                _context.Files.Add(savedFile);
                _context.Theories.Add(theory);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Теория «{model.Name}» успешно добавлена!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
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
        /// Форма редактирования теории
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var theory = await _context.Theories
                .Include(t => t.File)
                .Include(t => t.Section)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (theory == null) return NotFound();

            // Читаем содержимое существующего файла
            string markdownContent = "";
            if (theory.File != null && !string.IsNullOrEmpty(theory.File.FilePath))
            {
                var fullPath = Path.Combine(_env.ContentRootPath, theory.File.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    markdownContent = await System.IO.File.ReadAllTextAsync(fullPath);
                }
            }

            var model = new TheoryViewModel
            {
                Id = theory.Id,
                Name = theory.Name,
                SectionId = theory.SectionId,
                ExistingFileId = theory.FileId, // Сохраняем ID существующего файла
                MarkdownContent = markdownContent, // Загружаем существующий контент
                Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync()
            };

            return View(model);
        }

        /// <summary>
        /// Обработка редактирования теории
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TheoryViewModel model, IFormFile? file)
        {
            if (id != model.Id) return NotFound();

            var theory = await _context.Theories
                .Include(t => t.File)
                .Include(t => t.Section)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (theory == null) return NotFound();

            Console.WriteLine($"🔍 THEORY #{id}:");
            Console.WriteLine($"   SectionId (из БД): {theory.SectionId?.ToString() ?? "NULL"}");
            Console.WriteLine($"   Section.Name: {theory.Section?.Name ?? "NULL"}");

            // Очищаем поля, которые не должны валидироваться автоматически
            ModelState.Remove("Sections");
            ModelState.Remove("File");
            ModelState.Remove("ExistingFileId");
            ModelState.Remove("MarkdownContent");

            // Определяем источник контента
            bool hasNewFile = file != null && file.Length > 0;
            bool hasEditedContent = !string.IsNullOrWhiteSpace(model.MarkdownContent);

            // Валидация: должен быть хотя бы один источник контента
            if (!hasNewFile && !hasEditedContent && theory.File == null)
            {
                ModelState.AddModelError("", "Загрузите файл или введите контент в редакторе");
            }

            if (!ModelState.IsValid)
            {
                model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                return View(model);
            }

            try
            {
                // Обновляем базовые поля
                theory.Name = model.Name;
                theory.SectionId = model.SectionId;

                string? newContent = null;

                // 1️⃣ Если загружен новый файл
                if (hasNewFile)
                {
                    if (Path.GetExtension(file.FileName).ToLower() != ".md")
                    {
                        ModelState.AddModelError("File", "Файл должен быть в формате .md");
                        model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                        return View(model);
                    }
                    using var reader = new StreamReader(file.OpenReadStream());
                    newContent = await reader.ReadToEndAsync();
                }
                // 2️⃣ Если контент изменён в редакторе
                else if (hasEditedContent)
                {
                    newContent = model.MarkdownContent;
                }

                // 🔄 Если есть новый контент И есть существующий файл — ОБНОВЛЯЕМ контент в том же файле
                if (newContent != null && theory.File != null)
                {
                    var fullPath = Path.Combine(_env.ContentRootPath, theory.File.FilePath);

                    // Создаём директорию если нет
                    var directory = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // ✅ ПЕРЕЗАПИСЫВАЕМ контент в существующий файл (не создаём новый!)
                    await System.IO.File.WriteAllTextAsync(fullPath, newContent);

                    // Обновляем отображаемое имя файла если изменилось название теории
                    theory.File.Name = model.Name;
                    // FilePath остаётся прежним — это тот же файл, просто с новым контентом
                }
                // 🆕 Если контента нет, но пользователь загрузил новый файл (теория была без файла)
                else if (newContent != null && theory.File == null)
                {
                    var fileName = $"{Guid.NewGuid()}_{SanitizeFileName(model.Name)}.md";
                    var uploadsDir = Path.Combine(_env.ContentRootPath, "Source", "Theories");
                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    var filePath = Path.Combine(uploadsDir, fileName);
                    await System.IO.File.WriteAllTextAsync(filePath, newContent);

                    var savedFile = new FileP
                    {
                        Name = model.Name,
                        FilePath = Path.Combine("Source", "Theories", fileName).Replace("\\", "/")
                    };

                    _context.Files.Add(savedFile);
                    await _context.SaveChangesAsync(); // Сохраняем чтобы получить Id

                    theory.File = savedFile;
                    theory.FileId = savedFile.Id;
                }
                // 📦 Если ничего не изменилось — оставляем файл как есть (ничего не делаем)

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Теория «{model.Name}» успешно обновлена!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обновлении: {ex.Message}");
                Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
            }

            // Возвращаем форму с ошибками
            model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        // Вспомогательный метод для безопасных имён файлов
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        /// <summary>
        /// Удаление теории
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var theory = await _context.Theories
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (theory != null)
            {
                _fileService.DeleteFile(theory.File);
                _context.Files.Remove(theory.File);
                _context.Theories.Remove(theory);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Теория успешно удалена!";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Управление связанными тестами
        /// </summary>
        public async Task<IActionResult> ManageTests(int id)
        {
            var theory = await _context.Theories
                .Include(t => t.TheoryTestRelations)
                    .ThenInclude(ttr => ttr.Test)
                    .ThenInclude(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (theory == null) return NotFound();

            var allTests = await _context.Tests
                .Include(t => t.File)
                .ToListAsync();

            ViewBag.AllTests = allTests;
            return View(theory);
        }

        /// <summary>
        /// Добавление связи с тестом
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddTest(int theoryId, int testId)
        {
            // Проверяем, существует ли уже связь
            var exists = await _context.TheoryTestRelations
                .AnyAsync(r => r.TheoryId == theoryId && r.TestId == testId);

            if (!exists)
            {
                var relation = new TheoryTestRelation { TheoryId = theoryId, TestId = testId };
                _context.TheoryTestRelations.Add(relation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Тест успешно привязан к теории!";
            }

            return RedirectToAction(nameof(ManageTests), new { id = theoryId });
        }

        /// <summary>
        /// Удаление связи с тестом
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveTest(int theoryId, int testId)
        {
            var relation = await _context.TheoryTestRelations
                .FirstOrDefaultAsync(r => r.TheoryId == theoryId && r.TestId == testId);

            if (relation != null)
            {
                _context.TheoryTestRelations.Remove(relation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Связь с тестом удалена!";
            }

            return RedirectToAction(nameof(ManageTests), new { id = theoryId });
        }
    }
}