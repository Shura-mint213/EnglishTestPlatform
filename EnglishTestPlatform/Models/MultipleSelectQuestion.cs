namespace EnglishTestPlatform.Models
{
    public class MultipleSelectQuestion : Question
    {
        public List<string> Options { get; set; } = new();
        public List<string> Correct { get; set; } = new(); // Список правильных ответов (текст)
    }
}
