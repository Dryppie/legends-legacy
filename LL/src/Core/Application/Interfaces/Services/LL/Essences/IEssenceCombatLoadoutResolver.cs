using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceCombatLoadoutResolver
{
    Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EssenceCombatLoadout> ResolveAsync(
        Guid characterId,
        EssenceCombatActivity activity,
        CancellationToken cancellationToken) =>
        ResolveAsync(characterId, cancellationToken);
    /// <summary>Preserves the supplied order; callers must provide occupied Essences in ascending visible SlotIndex order.</summary>
    EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences);
}

/// <summary>EquippedEssences contains occupied slots in ascending visible SlotIndex order, with empty slots omitted.</summary>
public sealed record EssenceCombatLoadout(
    Guid CharacterId,
    IReadOnlyList<PlayerEssence> EquippedEssences,
    IReadOnlyList<AttributeModifierBase> AttributeModifiers,
    IReadOnlySet<string> Tags);
