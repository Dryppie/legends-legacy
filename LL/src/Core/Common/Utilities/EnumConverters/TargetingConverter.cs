using Domain.Models.Abilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class TargetingConverter : JsonConverter<Targeting>
{
    public override Targeting Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "None" => Targeting.None,
            "CauseOfTrigger" => Targeting.CauseOfTrigger,
            "Self" => Targeting.Self,
            "SingleEnemy" => Targeting.SingleEnemy,
            "SingleAlly" => Targeting.SingleAlly,
            "TwoEnemies" => Targeting.TwoEnemies,
            "TwoAllies" => Targeting.TwoAllies,
            "SingleDeadEnemy" => Targeting.SingleDeadEnemy,
            "SingleDeadAlly" => Targeting.SingleDeadAlly,
            "SingleRandomEnemy" => Targeting.SingleRandomEnemy,
            "SingleRandomAlly" => Targeting.SingleRandomAlly,
            "SingleEnemyLowestHealth" => Targeting.SingleEnemyLowestHealth,
            "SingleAllyLowestHealth" => Targeting.SingleAllyLowestHealth,
            "AllEnemies" => Targeting.AllEnemies,
            "AllAllies" => Targeting.AllAllies,
            "AllAlliesAndSelf" => Targeting.AllAlliesAndSelf,
            _ => throw new JsonException($"Unknown targeting type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Targeting value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            Targeting.None => "None",
            Targeting.CauseOfTrigger => "CauseOfTrigger",
            Targeting.Self => "Self",
            Targeting.SingleEnemy => "SingleEnemy",
            Targeting.SingleAlly => "SingleAlly",
            Targeting.SingleDeadEnemy => "SingleDeadEnemy",
            Targeting.SingleDeadAlly => "SingleDeadAlly",
            Targeting.SingleRandomEnemy => "SingleRandomEnemy",
            Targeting.SingleRandomAlly => "SingleRandomAlly",
            Targeting.SingleEnemyLowestHealth => "SingleEnemyLowestHealth",
            Targeting.SingleAllyLowestHealth => "SingleAllyLowestHealth",
            Targeting.AllEnemies => "AllEnemies",
            Targeting.AllAllies => "AllAllies",
            Targeting.AllAlliesAndSelf => "AllAlliesAndSelf",
            _ => throw new JsonException($"Unknown targeting type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}