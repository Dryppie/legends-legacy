using Domain.Interfaces;
using Domain.Models.Abilities.Effects.Conditions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class EffectConditionConverter : JsonConverter<IEffectCondition>
{
    public override IEffectCondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var root = jsonDoc.RootElement;
            var intervalType = root.GetProperty("Type").GetString();

            switch (intervalType)
            {
                case "HealthCondition":
                    var healthPercentage = root.GetProperty("HealthPercentage").GetInt32();
                    var comparisonType = Enum.Parse<ComparisonType>(root.GetProperty("ComparisonType").GetString()!);
                    return new HealthCondition(healthPercentage, comparisonType);
                case "NoCondition":
                    return new NoCondition();
                default:
                    return new NoCondition();
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, IEffectCondition value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}