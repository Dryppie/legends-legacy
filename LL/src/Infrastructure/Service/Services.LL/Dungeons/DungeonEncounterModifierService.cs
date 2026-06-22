using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonEncounterModifierService : IDungeonEncounterModifierService
{
    public IReadOnlyList<AttributeModifierBase> GetActiveEnemyAttributeModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(room);

        run.State ??= new DungeonRunState { RunId = run.Id };
        if (room.Type is not (RoomType.Combat or RoomType.MiniBoss))
        {
            return [];
        }

        var modifierIds = dungeon.Mechanic.Thresholds.Count > 0
            ? dungeon.Mechanic.Thresholds
                .Where(x => run.State.Pressure >= x.Value)
                .SelectMany(x => x.EnemyModifierIds)
                .ToList()
            : GetDefaultPressureModifierIds(run.State.Pressure).ToList();

        return modifierIds
            .Select(CreateKnownModifier)
            .Where(x => x is not null)
            .GroupBy(x => $"{x!.AttributeType}:{x.ModifierType}:{x.Amount}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First()!)
            .Cast<AttributeModifierBase>()
            .ToList();
    }

    private static IEnumerable<string> GetDefaultPressureModifierIds(int pressure)
    {
        if (pressure >= 75)
        {
            yield return "enemy_pressure_enraged";
        }
        else if (pressure >= 50)
        {
            yield return "enemy_pressure_alert";
        }
        else if (pressure >= 25)
        {
            yield return "enemy_pressure_ready";
        }
    }

    private static DungeonAttributeModifier? CreateKnownModifier(string modifierId) =>
        modifierId switch
        {
            "enemy_pressure_ready" => new(AttributeType.Precision, 3, ModifierType.Additive),
            "enemy_pressure_alert" => new(AttributeType.Power, 5, ModifierType.Additive),
            "enemy_pressure_enraged" => new(AttributeType.Power, 10, ModifierType.Additive),
            "alarm_enemy_ready" => new(AttributeType.Precision, 4, ModifierType.Additive),
            "alarm_enemy_reinforced" => new(AttributeType.Armor, 8, ModifierType.Additive),
            "curse_enemy_shrouded" => new(AttributeType.Resistance, 6, ModifierType.Additive),
            "curse_enemy_empowered" => new(AttributeType.Spirit, 8, ModifierType.Additive),
            _ => null
        };
}
