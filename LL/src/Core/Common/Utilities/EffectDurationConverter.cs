using Domain.Interfaces.Abilities;
using Domain.Models.Abilities.Effects.Duration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class EffectDurationConverter : JsonConverter<IEffectDuration>
{
    public override IEffectDuration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var root = jsonDoc.RootElement;
            var durationType = root.GetProperty("Type").GetString();

            switch (durationType)
            {
                case "NoDuration":
                    return new NoDuration();
                case "TimedDuration":
                    var duration = root.GetProperty("Duration").GetInt32();
                    return new TimedDuration(duration + 1); // This is to counteract the tick that happens when the effect is applied to a target
                case "IndefiniteDuration":
                    return new IndefiniteDuration();

                default:
                    return new NoDuration();
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, IEffectDuration value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}