using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class EffectTagConverter : JsonConverter<EffectTag>
{
    public override EffectTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "Slashing" => EffectTag.Slashing,
            "Blunt" => EffectTag.Blunt,
            "Piercing" => EffectTag.Piercing,
            "Arrows" => EffectTag.Arrows,
            "Spells" => EffectTag.Spells,
            "SummonExpiration" => EffectTag.SummonExpiration,
            _ => throw new JsonException($"Unknown effect tag: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, EffectTag value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            EffectTag.Slashing => "Slashing",
            EffectTag.Blunt => "Blunt",
            EffectTag.Piercing => "Piercing",
            EffectTag.Arrows => "Arrows",
            EffectTag.Spells => "Spells",
            EffectTag.SummonExpiration => "SummonExpiration",
            _ => throw new JsonException($"Unknown effect tag: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}