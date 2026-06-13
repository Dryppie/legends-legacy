using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class TriggerFilterConverter : JsonConverter<ITriggerFilter>
{
    public override ITriggerFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var filterType = root.GetProperty("Type").GetString();
        return filterType switch
        {
            "AbilityIdFilter" => new AbilityIdFilter
            {
                AllowedIds = root.GetProperty("AllowedIds").EnumerateArray().Select(x => x.GetString()!).ToList()
            },

            "StatusIdFilter" => new StatusIdFilter
            {
                StatusIds = root.GetProperty("StatusIds").EnumerateArray().Select(x => x.GetString()!).ToList()
            },

            "SourceIsSelfFilter" => new SourceIsSelfFilter(null!), // injected later

            _ => throw new JsonException($"Unknown filter type: {filterType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ITriggerFilter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case AbilityIdFilter ability:
                writer.WriteString("Type", "AbilityIdFilter");
                writer.WritePropertyName("AllowedIds");
                JsonSerializer.Serialize(writer, ability.AllowedIds, options);
                break;

            case StatusIdFilter status:
                writer.WriteString("Type", "StatusIdFilter");
                writer.WritePropertyName("StatusIds");
                JsonSerializer.Serialize(writer, status.StatusIds, options);
                break;

            case SourceIsSelfFilter:
                writer.WriteString("Type", "SourceIsSelfFilter");
                break;

            default:
                throw new JsonException($"Unsupported filter type: {value.GetType()}");
        }

        writer.WriteEndObject();
    }
}