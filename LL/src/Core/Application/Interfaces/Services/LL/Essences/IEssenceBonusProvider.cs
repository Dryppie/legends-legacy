using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceBonusProvider
{
    Task<IReadOnlyList<AttributeModifierBase>> GetAttunedAttributeModifiersAsync(Guid characterId, CancellationToken cancellationToken);
    IReadOnlyList<AttributeModifierBase> GetAttunedAttributeModifiers(IEnumerable<PlayerEssence> essences);
}
