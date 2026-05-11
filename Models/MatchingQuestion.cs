namespace EnglishTestPlatform.Models
{
    public class MatchingPair
    {
        public string Left { get; set; }
        public string Right { get; set; }
    }

    public class MatchingQuestion : Question
    {
        public List<MatchingPair> Pairs { get; set; }
    }
}
