using Domain.Interfaces;
using Domain.Models.Abilities.Effects.Interval;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class EffectIntervalConverter : JsonConverter<IEffectInterval>
{
    public override IEffectInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var root = jsonDoc.RootElement;
            var intervalType = root.GetProperty("Type").GetString();

            switch (intervalType)
            {
                case "Interval":
                    var interval = root.GetProperty("Interval").GetInt32();
                    return new Interval(interval);
                case "NoInterval":
                    return new NoInterval();
                default:
                    return new NoInterval();
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, IEffectInterval value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}