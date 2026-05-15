using EnglishTestPlatform.Models;

namespace EnglishTestPlatform.Interfaces
{
    public interface ITestLoaderService
    {
        Task<TestModel> LoadTestFromDatabaseAsync(string testName);
        Task<string> GetTestFilePathAsync(string testName);
    }
}
