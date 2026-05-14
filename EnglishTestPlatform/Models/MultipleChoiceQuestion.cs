namespace EnglishTestPlatform.Models
{
    public class MultipleChoiceQuestion : Question
    {
        public List<string> Options { get; set; }
        public string Correct { get; set; }
    }
}
