using Domain.Models.Combat.Abilities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Utilities.EnumConverters;
public class CombatTargetingConverter : JsonConverter<CombatTargeting>
{
    public override CombatTargeting Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return stringValue switch
        {
            "None" => CombatTargeting.None,
            "CauseOfTrigger" => CombatTargeting.CauseOfTrigger,
            "AttackedEnemy" => CombatTargeting.AttackedEnemy,
            "Self" => CombatTargeting.Self,
            "SingleEnemy" => CombatTargeting.SingleEnemy,
            "SingleAlly" => CombatTargeting.SingleAlly,
            "TwoEnemies" => CombatTargeting.TwoEnemies,
            "TwoAllies" => CombatTargeting.TwoAllies,
            "SingleDeadEnemy" => CombatTargeting.SingleDeadEnemy,
            "SingleDeadAlly" => CombatTargeting.SingleDeadAlly,
            "SingleRandomEnemy" => CombatTargeting.SingleRandomEnemy,
            "SingleRandomAlly" => CombatTargeting.SingleRandomAlly,
            "SingleEnemyLowestHealth" => CombatTargeting.SingleEnemyLowestHealth,
            "SingleAllyLowestHealth" => CombatTargeting.SingleAllyLowestHealth,
            "AllyHighestMaxHealth" => CombatTargeting.AllyHighestMaxHealth,
            "AllEnemies" => CombatTargeting.AllEnemies,
            "AllAllies" => CombatTargeting.AllAllies,
            "YourTeam" => CombatTargeting.YourTeam,
            "EveryoneButYou" => CombatTargeting.EveryoneButYou,
            _ => throw new JsonException($"Unknown targeting type: {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, CombatTargeting value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            CombatTargeting.None => "None",
            CombatTargeting.CauseOfTrigger => "CauseOfTrigger",
            CombatTargeting.AttackedEnemy => "AttackedEnemy",
            CombatTargeting.Self => "Self",
            CombatTargeting.SingleEnemy => "SingleEnemy",
            CombatTargeting.SingleAlly => "SingleAlly",
            CombatTargeting.SingleDeadEnemy => "SingleDeadEnemy",
            CombatTargeting.SingleDeadAlly => "SingleDeadAlly",
            CombatTargeting.SingleRandomEnemy => "SingleRandomEnemy",
            CombatTargeting.SingleRandomAlly => "SingleRandomAlly",
            CombatTargeting.SingleEnemyLowestHealth => "SingleEnemyLowestHealth",
            CombatTargeting.SingleAllyLowestHealth => "SingleAllyLowestHealth",
            CombatTargeting.AllyHighestMaxHealth => "AllyHighestMaxHealth",
            CombatTargeting.AllEnemies => "AllEnemies",
            CombatTargeting.AllAllies => "AllAllies",
            CombatTargeting.YourTeam => "YourTeam",
            CombatTargeting.EveryoneButYou => "EveryoneButYou",
            _ => throw new JsonException($"Unknown targeting type: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
