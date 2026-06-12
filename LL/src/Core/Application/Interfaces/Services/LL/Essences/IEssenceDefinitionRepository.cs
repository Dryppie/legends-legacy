using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceDefinitionRepository
{
    IReadOnlyList<EssenceDefinition> GetAll();
    EssenceDefinition? GetById(string essenceDefinitionId);
    EssenceDefinition? GetByMonsterId(string monsterId);
    IReadOnlyDictionary<string, EssenceProgressionTemplate> GetProgressionTemplates();
    AbilityDefinition? GetAbilityById(string abilityId);
}
