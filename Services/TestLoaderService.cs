using EnglishTestPlatform.Interfaces;
using EnglishTestPlatform.Models;
using System.Text.Json;

namespace EnglishTestPlatform.Services
{
    public class TestLoaderService : ITestLoaderService
    {
        private readonly string _testsDirectory;
        private readonly IWebHostEnvironment _env;

        public TestLoaderService(IWebHostEnvironment env)
        {
            _env = env;
            _testsDirectory = Path.Combine(env.ContentRootPath, "Tests");

            if (!Directory.Exists(_testsDirectory))
            {
                Directory.CreateDirectory(_testsDirectory);
                CreateSampleTest();
            }
        }

        private void CreateSampleTest()
        {
            var sampleTest = new
            {
                testTitle = "Sample Test",
                questions = new object[]
                {
                    new
                    {
                        type = "multiple_choice",
                        text = "What is the capital of France?",
                        options = new[] { "London", "Berlin", "Paris", "Madrid" },
                        correct = "Paris",
                        explanation = "Paris is the capital of France."
                    }
                }
            };

            var json = JsonSerializer.Serialize(sampleTest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(_testsDirectory, "sample_test.json"), json);
        }

        public List<string> GetAvailableTests()
        {
            if (!Directory.Exists(_testsDirectory))
                return new List<string>();

            return Directory.GetFiles(_testsDirectory, "*.json")
                            .Select(Path.GetFileNameWithoutExtension)
                            .ToList();
        }

        public TestModel LoadTest(string testFileName)
        {
            if (!testFileName.EndsWith(".json"))
                testFileName = testFileName + ".json";

            var filePath = Path.Combine(_testsDirectory, testFileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Тест {testFileName} не найден в папке {_testsDirectory}");
            }

            var json = File.ReadAllText(filePath);
            var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            var testTitle = root.GetProperty("testTitle").GetString();
            var questionsArray = root.GetProperty("questions");

            var questions = new List<Question>();

            foreach (var item in questionsArray.EnumerateArray())
            {
                var typeProperty = item.GetProperty("type").GetString();

                switch (typeProperty)
                {
                    case "multiple_choice":
                        questions.Add(new MultipleChoiceQuestion
                        {
                            Type = typeProperty,
                            Text = item.GetProperty("text").GetString(),
                            Explanation = item.TryGetProperty("explanation", out var exp) ? exp.GetString() : "",
                            Options = item.GetProperty("options").EnumerateArray().Select(o => o.GetString()).ToList(),
                            Correct = item.GetProperty("correct").GetString()
                        });
                        break;

                    case "multiple_select":
                        questions.Add(new MultipleSelectQuestion
                        {
                            Type = typeProperty,
                            Text = item.GetProperty("text").GetString(),
                            Explanation = item.TryGetProperty("explanation", out exp) ? exp.GetString() : "",
                            Options = item.GetProperty("options").EnumerateArray().Select(o => o.GetString()).ToList(),
                            Correct = item.GetProperty("correct").EnumerateArray().Select(c => c.GetString()).ToList()
                        });
                        break;

                    case "matching":
                        var pairs = new List<MatchingPair>();
                        var pairsArray = item.GetProperty("pairs");
                        foreach (var pair in pairsArray.EnumerateArray())
                        {
                            pairs.Add(new MatchingPair
                            {
                                Left = pair.GetProperty("left").GetString(),
                                Right = pair.GetProperty("right").GetString()
                            });
                        }
                        questions.Add(new MatchingQuestion
                        {
                            Type = typeProperty,
                            Text = item.GetProperty("text").GetString(),
                            Explanation = item.TryGetProperty("explanation", out exp) ? exp.GetString() : "",
                            Pairs = pairs
                        });
                        break;

                    case "fill_in":
                        questions.Add(new FillInQuestion
                        {
                            Type = typeProperty,
                            Text = item.GetProperty("text").GetString(),
                            Explanation = item.TryGetProperty("explanation", out exp) ? exp.GetString() : "",
                            Correct = item.GetProperty("correct").GetString()
                        });
                        break;

                    default:
                        throw new NotSupportedException($"Неизвестный тип вопроса: {typeProperty}");
                }
            }

            return new TestModel
            {
                TestTitle = testTitle,
                Questions = questions
            };
        }
    }
}