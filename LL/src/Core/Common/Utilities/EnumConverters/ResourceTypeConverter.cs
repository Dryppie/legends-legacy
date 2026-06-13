using Domain.Models.Combat.Abilities.ResourceCosts;
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
            "MaxHealth" => ResourceType.Health,
            "Health" => ResourceType.Health,
            "Barrier" => ResourceType.Barrier,
            _ => throw new JsonException($"Unknown resource type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ResourceType value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            ResourceType.Health => "MaxHealth",
            ResourceType.Barrier => "Barrier",
            _ => throw new JsonException($"Unknown resource type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
