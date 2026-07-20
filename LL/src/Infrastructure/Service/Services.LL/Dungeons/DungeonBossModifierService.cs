using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;

namespace Services.LL.Dungeons;

public sealed class DungeonBossModifierService : IDungeonBossModifierService
{
    public IReadOnlyList<DungeonBossModifier> GetActiveBossModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(dungeon);
        ArgumentNullException.ThrowIfNull(room);

        run.State ??= new DungeonRunState { RunId = run.Id };
        if (room.Type != RoomType.Boss)
        {
            return [];
        }

        var modifiers = new List<DungeonBossModifier>();
        modifiers.AddRange(run.State.BossAspects
            .Where(aspect => !string.Equals(aspect.State, "Removed", StringComparison.OrdinalIgnoreCase))
            .Select(aspect => new DungeonBossModifier
            {
                Id = aspect.Id,
                Name = aspect.Name,
                Description = aspect.Description,
                Source = aspect.Source,
                AttributeType = aspect.AttributeType,
                Amount = string.Equals(aspect.State, "Weakened", StringComparison.OrdinalIgnoreCase)
                    ? aspect.Amount / 2f
                    : aspect.Amount,
                ModifierType = aspect.ModifierType
            }));

        return modifiers
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    public IReadOnlyList<AttributeModifierBase> GetActiveBossAttributeModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room) =>
        GetActiveBossModifiers(run, dungeon, room)
            .Select(x => new DungeonAttributeModifier(x.AttributeType, x.Amount, x.ModifierType))
            .Cast<AttributeModifierBase>()
            .ToList();
}
