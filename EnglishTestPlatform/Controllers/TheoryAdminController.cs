using Data;
using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Services;
using EnglishTestPlatform.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestPlatform.Controllers
{
    public class TheoryAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public TheoryAdminController(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // Список теорий
        public async Task<IActionResult> Index()
        {
            var theories = await _context.Theories
                .Include(t => t.File)
                .ToListAsync();
            return View(theories);
        }

        // Форма создания
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TheoryViewModel model)
        {
            // Проверяем валидацию
            if (ModelState.IsValid)
            {
                // Проверяем файл
                if (model.File == null || Path.GetExtension(model.File.FileName).ToLower() != ".md")
                {
                    ModelState.AddModelError("File", "Загрузите файл в формате .md");
                    return View(model);
                }

                try
                {
                    // Сохраняем файл
                    var savedFile = await _fileService.SaveFileAsync(model.File, "Theories");

                    // Создаем сущность Theory
                    var theory = new Theory
                    {
                        Name = model.Name,
                        File = savedFile,
                        FileId = savedFile.Id
                    };

                    _context.Theories.Add(theory);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Теория успешно добавлена!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при сохранении: {ex.Message}");
                }
            }

            // Выводим ошибки для отладки
            Console.WriteLine("=== ModelState Errors ===");
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key].Errors;
                foreach (var error in errors)
                {
                    Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                }
            }

            return View(model);
        }

        // Редактирование
        public async Task<IActionResult> Edit(int id)
        {
            var theory = await _context.Theories
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (theory == null) return NotFound();
            return View(theory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Theory model, IFormFile? file)
        {
            if (id != model.Id) return NotFound();
            var theory = await _context.Theories
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (theory == null) return NotFound();

            if (ModelState.IsValid)
            {
                theory.Name = model.Name;
                if (file != null)
                {
                    if (Path.GetExtension(file.FileName).ToLower() != ".md")
                        ModelState.AddModelError("file", "Файл должен быть .md");
                    else
                    {
                        // Удаляем старый файл
                        _fileService.DeleteFile(theory.File);
                        _context.Files.Remove(theory.File);

                        // Сохраняем новый
                        var newFile = await _fileService.SaveFileAsync(file, "Theories");
                        theory.File = newFile;
                    }
                }
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(theory);
        }

        // Удаление
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
            }
            return RedirectToAction(nameof(Index));
        }

        // Управление связанными тестами
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

        [HttpPost]
        public async Task<IActionResult> AddTest(int theoryId, int testId)
        {
            var relation = new TheoryTestRelation { TheoryId = theoryId, TestId = testId };
            _context.TheoryTestRelations.Add(relation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageTests), new { id = theoryId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveTest(int theoryId, int testId)
        {
            var relation = await _context.TheoryTestRelations
                .FirstOrDefaultAsync(r => r.TheoryId == theoryId && r.TestId == testId);
            if (relation != null)
            {
                _context.TheoryTestRelations.Remove(relation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageTests), new { id = theoryId });
        }
    }
}

