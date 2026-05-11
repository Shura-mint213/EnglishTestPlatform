using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EnglishTestPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITestLoaderService _testLoader;

        public HomeController(ITestLoaderService testLoader)
        {
            _testLoader = testLoader;
        }

        public IActionResult Index()
        {
            var tests = _testLoader.GetAvailableTests();
            return View(tests);
        }
    }
}
