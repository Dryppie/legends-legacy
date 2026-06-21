using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceAbilityProvider
{
    Task<IReadOnlyList<AbilitySpec>> GetAttunedAbilitiesAsync(Guid characterId, CancellationToken cancellationToken);
    IReadOnlyList<AbilitySpec> GetAttunedAbilities(IEnumerable<PlayerEssence> essences);
}
