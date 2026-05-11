namespace EnglishTestPlatform.Models
{
    public class UserAnswer
    {
        public int questionIndex { get; set; }
        public object answer { get; set; }  // string, List<string>, Dictionary<string,string> и т.д.
    }
}
