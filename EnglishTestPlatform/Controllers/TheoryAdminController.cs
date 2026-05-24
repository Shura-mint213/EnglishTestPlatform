using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Services;
using EnglishTestPlatform.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Markdig;

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
            // Удаляем лишние ошибки валидации
            ModelState.Remove("Sections");

            // Проверяем валидацию
            if (ModelState.IsValid)
            {
                string? markdownContent = null;

                try
                {
                    // Вариант 1: Загрузка файла
                    if (model.File != null)
                    {
                        if (Path.GetExtension(model.File.FileName).ToLower() != ".md")
                        {
                            ModelState.AddModelError("File", "Загрузите файл в формате .md");
                            model.Sections = await _context.Sections.OrderBy(s => s.Name).ToListAsync();
                            return View(model);
                        }

                        using (var reader = new StreamReader(model.File.OpenReadStream()))
                        {
                            markdownContent = await reader.ReadToEndAsync();
                        }
                    }
                    // Вариант 2: Ввод Markdown через редактор
                    else if (!string.IsNullOrEmpty(model.MarkdownContent))
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

            var model = new TheoryViewModel
            {
                Id = theory.Id,
                Name = theory.Name,
                SectionId = theory.SectionId,
                ExistingFileId = theory.FileId, // Сохраняем ID существующего файла
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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (theory == null) return NotFound();

            ModelState.Remove("Sections");
            ModelState.Remove("File");
            ModelState.Remove("ExistingFileId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Обновляем название и раздел
                    theory.Name = model.Name;
                    theory.SectionId = model.SectionId;

                    // Если загружен новый файл
                    if (file != null)
                    {
                        if (Path.GetExtension(file.FileName).ToLower() != ".md")
                        {
                            ModelState.AddModelError("File", "Файл должен быть .md");
                        }
                        else
                        {
                            // Удаляем старый файл
                            if (theory.File != null)
                            {
                                _fileService.DeleteFile(theory.File);
                                _context.Files.Remove(theory.File);
                            }

                            // Сохраняем новый
                            var newFile = await _fileService.SaveFileAsync(file, "Theories");
                            theory.File = newFile;
                            theory.FileId = newFile.Id;
                        }
                    }
                    // Если файл не загружен, но ExistingFileId есть - сохраняем существующий файл
                    else if (model.ExistingFileId.HasValue && theory.FileId != model.ExistingFileId.Value)
                    {
                        var existingFile = await _context.Files.FindAsync(model.ExistingFileId.Value);
                        if (existingFile != null)
                        {
                            theory.File = existingFile;
                            theory.FileId = existingFile.Id;
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        await _context.SaveChangesAsync();
                        TempData["Success"] = $"Теория «{model.Name}» успешно обновлена!";
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