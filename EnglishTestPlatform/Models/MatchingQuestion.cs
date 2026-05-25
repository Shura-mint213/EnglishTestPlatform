namespace EnglishTestPlatform.Models
{
    public class MatchingPair
    {
        public string Left { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;
    }

    public class MatchingQuestion : Question
    {
        public List<MatchingPair> Pairs { get; set; } = new(); // Единое свойство вместо LeftItems/RightItems
    }
}
