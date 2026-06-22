using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonBossModifierService
{
    IReadOnlyList<DungeonBossModifier> GetActiveBossModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room);

    IReadOnlyList<AttributeModifierBase> GetActiveBossAttributeModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room);
}
