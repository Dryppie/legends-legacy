using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface ICombatRatingService
{
    CombatRatingProjection Calculate(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> nonEssenceModifiers,
        IEnumerable<PlayerEssence> equippedEssences);
}

public sealed record CombatRatingProjection(
    IReadOnlyDictionary<AttributeType, float> Attributes,
    CombatRatingBreakdown Breakdown);
