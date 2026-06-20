using Domain.Models.Combat.Abilities.V2;
using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceDefinitionRepository
{
    IReadOnlyList<EssenceDefinition> GetAll();
    IReadOnlyList<AbilitySpec> GetAllAbilities();
    EssenceDefinition? GetById(string essenceDefinitionId);
    EssenceDefinition? GetByMonsterId(string monsterId);
    AbilitySpec? GetAbilityById(string abilityId);
}
