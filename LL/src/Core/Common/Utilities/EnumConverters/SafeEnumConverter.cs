using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class SafeEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var enumText = reader.GetString();
            if (Enum.TryParse(enumText, ignoreCase: true, out T value))
                return value;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var intValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(T), intValue))
                return (T)(object)intValue;
        }

        return default; // or return a known fallback like T.Unknown
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
