using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCombatAbilityFactory
{
    IReadOnlyList<ResolvedCombatAbility> CreateAbilities(EssenceDefinition definition, PlayerEssence essence);
}
