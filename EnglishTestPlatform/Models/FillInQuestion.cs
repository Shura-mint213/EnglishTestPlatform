namespace EnglishTestPlatform.Models
{
    public class FillInQuestion : Question
    {
        public List<string> Answers { get; set; } = new(); // Правильные ответы для fill_in
    }
}
