using Domain.Models.Abilities.Effects.EffectModifications;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class EffectModificationTypeConverter : JsonConverter<EffectModificationType>
{
    public override EffectModificationType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (Enum.TryParse<EffectModificationType>(value, true, out var result))
        {
            return result;
        }
        throw new JsonException($"Invalid EffectModificationType: {value}");
    }

    public override void Write(Utf8JsonWriter writer, EffectModificationType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}