using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceDefinitionRepository
{
    IReadOnlyList<EssenceDefinition> GetAll();
    IReadOnlyList<AbilityDefinition> GetAllAbilities();
    EssenceDefinition? GetById(string essenceDefinitionId);
    EssenceDefinition? GetByMonsterId(string monsterId);
    AbilityDefinition? GetAbilityById(string abilityId);
}
