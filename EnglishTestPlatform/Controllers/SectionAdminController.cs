using Data;
using Data.Entities;
using EnglishTestPlatform.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestPlatform.Controllers
{
    public class SectionAdminController : Controller
    {
        private readonly AppDbContext _context;

        public SectionAdminController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Список всех разделов
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var sections = await _context.Sections
                .Include(s => s.ParentSection)
                .Include(s => s.ChildSections)
                .OrderBy(s => s.ParentSectionId)
                .ThenBy(s => s.OrderBy)
                .ToListAsync();
            return View(sections);
        }

        /// <summary>
        /// Форма создания раздела
        /// </summary>
        [HttpGet]
        public IActionResult Create(int? parentId)
        {
            var model = new SectionViewModel
            {
                ParentSectionId = parentId,
                OrderBy = 0
            };
            return View(model);
        }

        /// <summary>
        /// Обработка создания раздела
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var section = new Section
                {
                    Name = model.Name,
                    Description = model.Description,
                    ParentSectionId = model.ParentSectionId,
                    OrderBy = model.OrderBy
                };

                _context.Sections.Add(section);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Раздел «{model.Name}» успешно создан!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        /// <summary>
        /// Форма редактирования раздела
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var section = await _context.Sections.FindAsync(id);
            if (section == null) return NotFound();

            var model = new SectionViewModel
            {
                Id = section.Id,
                Name = section.Name,
                Description = section.Description,
                ParentSectionId = section.ParentSectionId,
                OrderBy = section.OrderBy
            };

            return View(model);
        }

        /// <summary>
        /// Обработка редактирования раздела
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SectionViewModel model)
        {
            if (id != model.Id) return NotFound();

            var section = await _context.Sections.FindAsync(id);
            if (section == null) return NotFound();

            if (ModelState.IsValid)
            {
                section.Name = model.Name;
                section.Description = model.Description;
                section.OrderBy = model.OrderBy;

                // Проверяем, чтобы раздел не был сам себе родителем
                if (model.ParentSectionId == section.Id)
                {
                    ModelState.AddModelError("ParentSectionId", "Раздел не может быть родителем самого себя");
                    return View(model);
                }

                section.ParentSectionId = model.ParentSectionId;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Раздел «{model.Name}» успешно обновлён!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        /// <summary>
        /// Удаление раздела (с перемещением дочерних элементов)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var section = await _context.Sections
                .Include(s => s.ChildSections)
                .Include(s => s.Theories)
                .Include(s => s.Tests)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (section == null) return NotFound();

            // Перемещаем дочерние разделы к родительскому разделу
            if (section.ChildSections.Any())
            {
                foreach (var child in section.ChildSections)
                {
                    child.ParentSectionId = section.ParentSectionId;
                }
            }

            // Перемещаем теории к родительскому разделу
            if (section.Theories.Any())
            {
                foreach (var theory in section.Theories)
                {
                    theory.SectionId = section.ParentSectionId;
                }
            }

            // Перемещаем тесты к родительскому разделу
            if (section.Tests.Any())
            {
                foreach (var test in section.Tests)
                {
                    test.SectionId = section.ParentSectionId;
                }
            }

            _context.Sections.Remove(section);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Раздел «{section.Name}» успешно удалён! Дочерние элементы перемещены.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Получение дерева разделов в формате JSON
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTreeJson()
        {
            var sections = await _context.Sections
                .Select(s => new { id = s.Id, name = s.Name, parentId = s.ParentSectionId })
                .ToListAsync();
            return Json(sections);
        }
    }
}