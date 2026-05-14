namespace EnglishTestPlatform.Models
{
    public class MultipleSelectQuestion : Question
    {
        public List<string> Options { get; set; }
        public List<string> Correct { get; set; }
    }
}
