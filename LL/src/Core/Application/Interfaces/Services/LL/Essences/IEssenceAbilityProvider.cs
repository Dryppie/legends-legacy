using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceAbilityProvider
{
    Task<IReadOnlyList<AbilityDefinition>> GetAttunedAbilitiesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CombatAbilityInstance>> GetAttunedCombatAbilitiesAsync(Guid characterId, CancellationToken cancellationToken);
    IReadOnlyList<CombatAbilityInstance> GetAttunedCombatAbilities(IEnumerable<PlayerEssence> essences);
}
