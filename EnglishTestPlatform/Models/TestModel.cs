namespace EnglishTestPlatform.Models
{
    public class TestModel
    {
        public string TestTitle { get; set; } = string.Empty;
        public List<Question> Questions { get; set; } = new();
    }
}
