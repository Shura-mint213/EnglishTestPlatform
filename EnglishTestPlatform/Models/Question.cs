using System.Text.Json.Serialization;

namespace EnglishTestPlatform.Models
{
    public abstract class Question
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? Explanation { get; set; }
    }
}
