using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;

namespace Services.LL.Professions.Craftings;

public class ItemStatRollService : IItemStatRollService
{
    private readonly CraftingBalanceOptions _options;

    public ItemStatRollService(IOptions<CraftingBalanceOptions>? options = null)
    {
        _options = options?.Value ?? new CraftingBalanceOptions();
    }

    public IReadOnlyList<InstanceAttributeModifier> RollBaseStats(EquipmentBase equipment, CraftingRecipeDefinition recipe, int targetTier, ItemQuality quality, Random rng)
    {
        var profile = recipe.BaseStatProfileOverride ?? recipe.BaseStatProfile;
        if (profile.Count == 0) return [];

        var budget = _options.GetTierPowerBudget(targetTier)
            * _options.GetSlotBudgetWeight(equipment.EquipmentType)
            * _options.GetQualityStatMultiplier(quality);
        var variance = 0.95d + (rng.NextDouble() * 0.10d);

        return profile
            .Where(x => x.Value > 0)
            .Select(x => new InstanceAttributeModifier(x.Key, (float)Math.Max(1, Math.Round(budget * x.Value * variance)), ModifierType.Flat))
            .ToList();
    }
}
