using Domain.Models.Dungeons.Definitions;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonDelveDefinitionProvider
{
    DungeonDelveDefinition GetForDungeon(string dungeonDefinitionId);
    IReadOnlyList<DungeonDelveDefinition> GetAll();
}
