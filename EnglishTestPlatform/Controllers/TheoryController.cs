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

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), theory.File.FilePath);
            if (!System.IO.File.Exists(filePath))
                return Content("Файл теории не найден.");

            var markdown = await System.IO.File.ReadAllTextAsync(filePath);
            var html = Markdown.ToHtml(markdown,
                new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UsePipeTables() 
                    .UseBootstrap()
                    .Build());

            ViewBag.HtmlContent = html;
            ViewBag.RelatedTests = theory.TheoryTestRelations.Select(r => r.Test).ToList();

            return View(theory);

        }
    }
}