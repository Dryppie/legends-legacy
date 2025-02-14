using Domain.Models.Abilities.ResourceCosts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class ResourceTypeConverter : JsonConverter<ResourceType>
{
    public override ResourceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "Mana" => ResourceType.Mana,
            "Health" => ResourceType.Health,
            _ => throw new JsonException($"Unknown resource type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ResourceType value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            ResourceType.Mana => "Mana",
            ResourceType.Health => "Health",
            _ => throw new JsonException($"Unknown resource type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}