using System.Text.Json.Serialization;

namespace EnglishTestPlatform.Models
{
    [JsonDerivedType(typeof(MultipleChoiceQuestion), typeDiscriminator: "multiple_choice")]
    [JsonDerivedType(typeof(MultipleSelectQuestion), typeDiscriminator: "multiple_select")]
    [JsonDerivedType(typeof(MatchingQuestion), typeDiscriminator: "matching")]
    [JsonDerivedType(typeof(FillInQuestion), typeDiscriminator: "fill_in")]
    public abstract class Question
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string Explanation { get; set; }
    }
}
