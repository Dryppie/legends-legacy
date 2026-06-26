using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Professions.Craftings;

public class ItemStatRollService : IItemStatRollService
{
    private static readonly IReadOnlyDictionary<ItemQuality, double> QualityMultipliers =
        new Dictionary<ItemQuality, double>
        {
            [ItemQuality.Crude] = 0.85d,
            [ItemQuality.Standard] = 1.00d,
            [ItemQuality.Fine] = 1.12d,
            [ItemQuality.Exceptional] = 1.28d,
            [ItemQuality.Masterwork] = 1.50d
        };

    public IReadOnlyList<InstanceAttributeModifier> RollBaseStats(CraftingRecipeDefinition recipe, int targetTier, ItemQuality quality, Random rng)
    {
        var profile = recipe.BaseStatProfileOverride ?? recipe.BaseStatProfile;
        if (profile.Count == 0) return [];

        var budget = (20 + (targetTier * 12)) * QualityMultipliers.GetValueOrDefault(quality, 1.0d);
        var variance = 0.95d + (rng.NextDouble() * 0.10d);

        return profile
            .Where(x => x.Value > 0)
            .Select(x => new InstanceAttributeModifier(x.Key, (float)Math.Max(1, Math.Round(budget * x.Value * variance)), ModifierType.Flat))
            .ToList();
    }
}
