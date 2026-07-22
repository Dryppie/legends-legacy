using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Services.LL.Essences;

public sealed class CombatRatingService : ICombatRatingService
{
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;

    public CombatRatingService(IEssenceCombatLoadoutResolver essenceLoadouts)
    {
        _essenceLoadouts = essenceLoadouts;
    }

    public CombatRatingProjection Calculate(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> nonEssenceModifiers,
        IEnumerable<PlayerEssence> equippedEssences)
    {
        var ordinaryModifiers = nonEssenceModifiers.ToList();
        var loadout = _essenceLoadouts.Resolve(Guid.Empty, equippedEssences);
        var withoutEssences = AttributeCalculator.CalculateProjectedAttributes(
            baseAttributes,
            ordinaryModifiers);
        var withEssences = AttributeCalculator.CalculateProjectedAttributes(
            baseAttributes,
            ordinaryModifiers.Concat(loadout.AttributeModifiers));

        var ordinaryRating = CombatRatingCalculator.Calculate(withoutEssences);
        var ratingWithEssenceAttributes = CombatRatingCalculator.Calculate(withEssences);
        var breakdown = new CombatRatingBreakdown(
            ordinaryRating,
            ratingWithEssenceAttributes - ordinaryRating,
            loadout.AbilityCombatRating);

        return new CombatRatingProjection(withEssences, breakdown);
    }
}
