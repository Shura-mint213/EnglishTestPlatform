using Data;
using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EnglishTestPlatform.Services
{
    public class TestLoaderService : ITestLoaderService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly JsonSerializerOptions _jsonOptions;

        public TestLoaderService(AppDbContext context, 
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            // Добавляем кастомный конвертер
            _jsonOptions.Converters.Add(new QuestionConverterService());
        }

        public async Task<string> GetTestFilePathAsync(string testName)
        {
            // Ищем тест в БД
            var allTests = await _context.Tests
                .Include(t => t.File)
                .ToListAsync();

            var test = allTests.FirstOrDefault(t =>
                t.File != null &&
                Path.GetFileNameWithoutExtension(t.File.Name) == testName);

            if (test == null)
                throw new FileNotFoundException($"Тест '{testName}' не найден в базе данных");

            // Возвращаем полный путь к файлу
            return Path.Combine(_env.ContentRootPath, test.File.FilePath);
        }

        public async Task<TestModel> LoadTestFromDatabaseAsync(string testName)
        {
            var filePath = await GetTestFilePathAsync(testName);

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException($"Файл теста не найден: {filePath}");

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var test = JsonSerializer.Deserialize<TestModel>(json, _jsonOptions);

            if (test == null)
                throw new InvalidOperationException("Не удалось десериализовать тест");

            return test;
        }
    }
}