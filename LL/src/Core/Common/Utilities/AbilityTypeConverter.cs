using Domain.Models.Abilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class AbilityTypeConverter : JsonConverter<AbilityType>
{
    public override AbilityType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "Active" => AbilityType.Active,
            "Passive" => AbilityType.Passive,
            _ => throw new JsonException($"Unknown ability type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, AbilityType value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            AbilityType.Active => "Active",
            AbilityType.Passive => "Passive",
            _ => throw new JsonException($"Unknown ability type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}