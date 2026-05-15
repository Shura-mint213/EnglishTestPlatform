using System.Text.Json.Serialization;

namespace EnglishTestPlatform.Models
{
    public abstract class Question
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string Explanation { get; set; }
    }
}
