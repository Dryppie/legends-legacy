using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities;
public class ConditionConverter : JsonConverter<ICondition>
{
    public override ICondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var root = jsonDoc.RootElement;
            var intervalType = root.GetProperty("Type").GetString();

            switch (intervalType)
            {
                case "HealthCondition":
                    var healthPercentage = root.GetProperty("HealthPercentage").GetInt32();
                    var healthComparisonType = Enum.Parse<ComparisonType>(root.GetProperty("ComparisonType").GetString()!);
                    return new HealthCondition(healthPercentage, healthComparisonType);
                case "StatusEffectCondition":
                    var statusEffectType = Enum.Parse<StatusEffectType>(root.GetProperty("Status").GetString()!);
                    var stacksRequired = root.GetProperty("StacksRequired").GetInt32();
                    var statusEffectComparisonType = Enum.Parse<ComparisonType>(root.GetProperty("ComparisonType").GetString()!);
                    return new StatusEffectCondition(statusEffectType, stacksRequired, statusEffectComparisonType);
                case "NoCondition":
                    return new NoCondition();
                default:
                    return new NoCondition();
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, ICondition value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}