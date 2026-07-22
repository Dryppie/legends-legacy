using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCombatLoadoutResolver
{
    Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken);
    EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences);
}

public sealed record EssenceCombatLoadout(
    Guid CharacterId,
    IReadOnlyList<PlayerEssence> EquippedEssences,
    IReadOnlyList<AttributeModifierBase> AttributeModifiers,
    IReadOnlySet<string> Tags,
    int AbilityCombatRating);
