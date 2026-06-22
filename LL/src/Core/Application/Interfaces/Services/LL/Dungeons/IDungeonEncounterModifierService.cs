using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonEncounterModifierService
{
    IReadOnlyList<AttributeModifierBase> GetActiveEnemyAttributeModifiers(
        DungeonRun run,
        DungeonDefinition dungeon,
        RoomInstance room);
}
