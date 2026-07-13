using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface ICreatureEssenceLootTableRepository
{
    IReadOnlyList<CreatureEssenceLootTableDefinition> GetAll();
    CreatureEssenceLootTableDefinition? GetByCreatureId(string creatureId);
    CreatureEssenceLootTableDefinition? GetByEssenceDefinitionId(string essenceDefinitionId);
}
