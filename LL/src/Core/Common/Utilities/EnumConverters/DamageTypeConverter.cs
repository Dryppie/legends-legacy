using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class DamageTypeConverter : JsonConverter<DamageType>
{
    public override DamageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "None" => DamageType.None,
            "Physical" => DamageType.Physical,
            "Magical" => DamageType.Magical,
            "Bleed" => DamageType.Bleed,
            "Burn" => DamageType.Burn,
            "Poison" => DamageType.Poison,
            _ => throw new JsonException($"Unknown damage type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, DamageType value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            DamageType.None => "None",
            DamageType.Physical => "Physical",
            DamageType.Magical => "Magical",
            DamageType.Bleed => "Bleed",
            DamageType.Burn => "Burn",
            DamageType.Poison => "Poison",
            _ => throw new JsonException($"Unknown damage type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}