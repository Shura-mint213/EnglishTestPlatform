using EnglishTestPlatform.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishTestPlatform.Services
{
    public class QuestionConverterService : JsonConverter<Question>
    {
        public override Question? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProperty))
                {
                    throw new JsonException("Missing 'type' property");
                }

                var type = typeProperty.GetString();

                return type switch
                {
                    "multiple_choice" => JsonSerializer.Deserialize<MultipleChoiceQuestion>(root.GetRawText(), options),
                    "multiple_select" => JsonSerializer.Deserialize<MultipleSelectQuestion>(root.GetRawText(), options),
                    "matching" => JsonSerializer.Deserialize<MatchingQuestion>(root.GetRawText(), options),
                    "fill_in" => JsonSerializer.Deserialize<FillInQuestion>(root.GetRawText(), options),
                    _ => throw new JsonException($"Unknown question type: {type}")
                };
            }
        }

        public override void Write(Utf8JsonWriter writer, Question value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
