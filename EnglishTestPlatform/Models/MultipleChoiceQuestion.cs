namespace EnglishTestPlatform.Models
{
    public class MultipleChoiceQuestion : Question
    {
        public List<string> Options { get; set; } = new();
        public string Correct { get; set; } = string.Empty; // Индекс правильного ответа (0-based)
    }
}
