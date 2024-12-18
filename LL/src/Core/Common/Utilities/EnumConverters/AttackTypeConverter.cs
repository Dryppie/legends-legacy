using Domain.Models.Damages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class AttackTypeConverter : JsonConverter<AttackType>
{
    public override AttackType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "None" => AttackType.None,
            "Melee" => AttackType.Melee,
            "Ranged" => AttackType.Ranged,
            "DamageOverTime" => AttackType.DamageOverTime,
            _ => throw new JsonException($"Unknown attack type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, AttackType value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            AttackType.None => "None",
            AttackType.Melee => "Melee",
            AttackType.Ranged => "Ranged",
            AttackType.DamageOverTime => "DamageOverTime",
            _ => throw new JsonException($"Unknown attack type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}