using Data;
using Data.Entities;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestPlatform.Controllers
{
    /// <summary>
    /// Контроллер для работы с теоретическими материалами
    /// </summary>
    public class TheoryController : Controller
    {
        private readonly AppDbContext _context;

        public TheoryController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Список всех теоретических материалов
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var theories = await _context.Theories
                .Include(t => t.File)
                .ToListAsync();
            return View(theories);
        }

        /// <summary>
        /// Детальный просмотр теории (с преобразованием Markdown в HTML)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var theory = await _context.Theories
               .Include(t => t.File)
               .Include(t => t.TheoryTestRelations)
                   .ThenInclude(ttr => ttr.Test)
                   .ThenInclude(test => test.File)
               .FirstOrDefaultAsync(t => t.Id == id);

            if (theory == null) return NotFound();

            string html;

            // Если есть контент (введен вручную), используем его
            if (!string.IsNullOrEmpty(theory.Content))
            {
                html = Markdown.ToHtml(theory.Content, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
            }
            // Иначе читаем из файла
            else if (theory.File != null)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), theory.File.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return Content("Файл теории не найден.");

                var markdown = await System.IO.File.ReadAllTextAsync(filePath);
                html = Markdown.ToHtml(markdown, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
            }
            else
            {
                return Content("Содержимое теории не найдено.");
            }

            ViewBag.HtmlContent = html;
            ViewBag.RelatedTests = theory.TheoryTestRelations.Select(r => r.Test).ToList();

            return View(theory);

        }
    }
}