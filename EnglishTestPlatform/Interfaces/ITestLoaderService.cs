using EnglishTestPlatform.Models;

namespace EnglishTestPlatform.Interfaces
{
    public interface ITestLoaderService
    {
        List<string> GetAvailableTests();
        TestModel LoadTest(string testFileName);
    }
}
