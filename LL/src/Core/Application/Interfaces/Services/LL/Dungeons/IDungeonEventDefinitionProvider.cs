using Domain.Models.Dungeons.Definitions.Events;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonEventDefinitionProvider
{
    DungeonEventDefinition GetDefinition(string dungeonDefinitionId, EventOutcomeType outcomeType);
    IReadOnlyList<DungeonEventDefinition> GetAll();
}
