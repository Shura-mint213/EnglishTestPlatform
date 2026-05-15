using Data;
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

        public async Task<IActionResult> Index()
        {
            // Загружаем все тесты из БД (с файлами)
            var dbTests = await _context.Tests
                .Include(t => t.File)
                .ToListAsync();

            // Загружаем все теории из БД
            var theories = await _context.Theories
                .Include(t => t.File)
                .ToListAsync();

            ViewBag.Theories = theories;
            return View(dbTests);
        }
    }
}
