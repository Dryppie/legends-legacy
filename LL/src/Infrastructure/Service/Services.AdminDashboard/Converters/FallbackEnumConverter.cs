using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.AdminDashboard.Converters;
public class FallbackEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private readonly TEnum _defaultValue;

    public FallbackEnumConverter(TEnum defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? enumString = reader.GetString();
            if (Enum.TryParse<TEnum>(enumString, ignoreCase: true, out var value))
            {
                return value;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intValue))
        {
            if (Enum.IsDefined(typeof(TEnum), intValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
            }
        }

        return _defaultValue;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
