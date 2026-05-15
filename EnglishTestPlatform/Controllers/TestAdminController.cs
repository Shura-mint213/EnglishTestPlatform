using Data;
using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using EnglishTestPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index()
        {
            var tests = await _context.Tests
                .Include(t => t.File)
                .ToListAsync();
            return View(tests);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestViewModel model)
        {
            // Удаляем ошибки валидации для навигационных свойств
            ModelState.Remove("Id");

            // Проверяем валидацию
            if (ModelState.IsValid)
            {
                // Проверяем файл
                if (model.File == null || Path.GetExtension(model.File.FileName).ToLower() != ".json")
                {
                    ModelState.AddModelError("File", "Загрузите файл в формате .json");
                    return View(model);
                }

                try
                {
                    // Сохраняем файл
                    var savedFile = await _fileService.SaveFileAsync(model.File, "Tests");

                    // Создаем сущность Test
                    var test = new Test
                    {
                        File = savedFile,
                        FileId = savedFile.Id
                    };

                    _context.Tests.Add(test);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Тест «{model.Name}» успешно добавлен!";
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
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var test = await _context.Tests
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();
            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Test model, IFormFile? file)
        {
            if (id != model.Id) return NotFound();
            var test = await _context.Tests
                .Include(t => t.File)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (test == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    if (Path.GetExtension(file.FileName).ToLower() != ".json")
                        ModelState.AddModelError("file", "Файл должен быть .json");
                    else
                    {
                        _fileService.DeleteFile(test.File);
                        _context.Files.Remove(test.File);
                        var newFile = await _fileService.SaveFileAsync(file, "Tests");
                        test.File = newFile;
                    }
                }
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(test);
        }

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
            }
            return RedirectToAction(nameof(Index));
        }

        // Управление связями (теории, связанные с тестом)
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

        [HttpPost]
        public async Task<IActionResult> AddTheory(int testId, int theoryId)
        {
            var relation = new TheoryTestRelation { TestId = testId, TheoryId = theoryId };
            _context.TheoryTestRelations.Add(relation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageTheories), new { id = testId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveTheory(int testId, int theoryId)
        {
            var relation = await _context.TheoryTestRelations
                .FirstOrDefaultAsync(r => r.TestId == testId && r.TheoryId == theoryId);
            if (relation != null)
            {
                _context.TheoryTestRelations.Remove(relation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageTheories), new { id = testId });
        }
    }
}