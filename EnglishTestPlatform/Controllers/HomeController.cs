using Data;
using Data.Entities;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EnglishTestPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITestLoaderService _testLoader;
        private readonly AppDbContext _context;

        public HomeController(ITestLoaderService testLoader, AppDbContext context)
        {
            _testLoader = testLoader;
            _context = context;
        }

        public async Task<IActionResult> Index(int? sectionId)
        {
            // Если sectionId не указан, показываем корневые разделы (ParentSectionId == null)
            ViewBag.CurrentSectionId = sectionId;

            var sections = await _context.Sections
                .Where(s => s.ParentSectionId == sectionId)
                .OrderBy(s => s.OrderBy)
                .ToListAsync();

            // Теории и тесты, принадлежащие текущему разделу
            var theories = await _context.Theories
                .Include(t => t.File)
                .Where(t => t.SectionId == sectionId)
                .ToListAsync();

            var tests = await _context.Tests
                .Include(t => t.File)
                .Where(t => t.SectionId == sectionId)
                .ToListAsync();

            ViewBag.Sections = sections;
            ViewBag.Theories = theories;
            ViewBag.Tests = tests;

            // Хлебные крошки (breadcrumbs)
            var breadcrumbs = new List<Section>();
            if (sectionId.HasValue)
            {
                var current = await _context.Sections.FindAsync(sectionId);
                while (current != null)
                {
                    breadcrumbs.Insert(0, current);
                    current = await _context.Sections.FindAsync(current.ParentSectionId);
                }
            }
            ViewBag.Breadcrumbs = breadcrumbs;

            return View();
        }
    }
}
