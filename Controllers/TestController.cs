using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using EnglishTestPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishTestPlatform.Controllers
{
    public class TestController : Controller
    {
        private readonly ITestLoaderService _testLoader;
        private readonly TestEvaluatorService _evaluator;
        private readonly IMemoryCache _cache;

        public TestController(ITestLoaderService testLoader, 
                            TestEvaluatorService evaluator,
                            IMemoryCache cache)
        {
            _testLoader = testLoader;
            _evaluator = evaluator;
            _cache = cache;
        }

        [HttpGet]
        [Route("Take")]
        public IActionResult Take(string testName)
        {
            Console.WriteLine($"Take {testName}");
            if (string.IsNullOrEmpty(testName))
                return RedirectToAction("Index", "Home");

            try
            {
                var test = _testLoader.LoadTest(testName);
                ViewBag.TestName = testName;
                return View(test);
            }
            catch (Exception ex)
            {
                // Логируем ошибку для отладки
                Console.WriteLine($"Ошибка загрузки теста {testName}: {ex.Message}");
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        [HttpPost]
        [Route("Test/Submit")]
        public IActionResult Submit([FromBody] SubmitRequest request)
        {
            Console.WriteLine("=== SUBMIT METHOD START ===");
            Console.WriteLine($"Time: {DateTime.Now}");
            Console.WriteLine($"Request content type: {Request.ContentType}");
            Console.WriteLine($"Request method: {Request.Method}");

            try
            {
                Console.WriteLine($"Submit: testName={request?.testName}");
                Console.WriteLine(JsonSerializer.Serialize(request));

                if (request == null || string.IsNullOrEmpty(request.answersJson))
                {
                    Console.WriteLine("ERROR: Request is null or answersJson is empty");
                    return BadRequest("Ответы не были получены");
                }

                Console.WriteLine("Loading test...");
                var test = _testLoader.LoadTest(request.testName);

                Console.WriteLine("Deserializing answers...");
                var userAnswers = JsonSerializer.Deserialize<List<UserAnswer>>(request.answersJson);


                if (userAnswers == null)
                {
                    Console.WriteLine("ERROR: Failed to deserialize userAnswers");
                    throw new InvalidOperationException("Не удалось десериализовать ответы");
                }

                Console.WriteLine("Evaluating test...");
                var result = _evaluator.Evaluate(test, userAnswers);

                Console.WriteLine(JsonSerializer.Serialize(userAnswers));

                // 🔥 Генерируем уникальный ключ и сохраняем в кэш
                var cacheKey = $"test_result_{Guid.NewGuid()}";
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                Console.WriteLine($"✅ Cached with key: {cacheKey}");

                // Редирект с ключом в query string
                return RedirectToAction("Result", new
                {
                    testName = request.testName,
                    cacheKey = cacheKey
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR IN SUBMIT ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }

        [HttpGet]
        [Route("Test/Result")]
        public IActionResult Result(string testName, string? cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey) ||
                !_cache.TryGetValue(cacheKey, out ResultViewModel result))
            {
                Console.WriteLine("❌ Cache miss or invalid key");
                return RedirectToAction("Index", "Home");
            }

            // Опционально: удалить после прочтения (одноразовый токен)
            //_cache.Remove(cacheKey);

            Console.WriteLine("✅ Result loaded from cache");
            return View("Result", result);
        }

        [HttpGet]
        [Route("Test/DebugSession")]
        public IActionResult DebugSession()
        {
            TempData["DebugTest"] = "Hello TempData!";
            var test = TempData["DebugTest"] as string;
            return Content($"TempData test: {test}");
        }
    }
}
