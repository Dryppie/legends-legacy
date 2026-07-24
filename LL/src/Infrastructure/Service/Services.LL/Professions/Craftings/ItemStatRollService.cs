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

    public IReadOnlyList<InstanceAttributeModifier> RollBaseStats(
        EquipmentBase equipment,
        EquipmentCraftingDesign design,
        int targetTier,
        ItemQuality quality,
        Random rng)
    {
        var profile = design.InitialStatProfile;
        if (profile.Count == 0) return [];

        var budget = _options.GetTierPowerBudget(targetTier)
            * _options.GetSlotBudgetWeight(equipment.EquipmentType)
            * _options.GetQualityStatMultiplier(quality);
        var variance = 0.95d + (rng.NextDouble() * 0.10d);
        var allocation = EquipmentBudgetAllocator.Allocate(
            targetTier,
            budget * variance,
            profile);

        return allocation.AddedPoints
            .Select(x => new InstanceAttributeModifier(
                x.Key,
                (float)x.Value,
                ModifierType.Flat))
            .ToList();
    }

    public IReadOnlyList<CraftedAttributeRange> GetBaseStatRanges(
        EquipmentBase equipment,
        EquipmentCraftingDesign design,
        int targetTier,
        IReadOnlyCollection<ItemQuality> possibleQualities)
    {
        if (design.InitialStatProfile.Count == 0 || possibleQualities.Count == 0)
            return [];

        var tierAndSlotBudget = _options.GetTierPowerBudget(targetTier)
            * _options.GetSlotBudgetWeight(equipment.EquipmentType);
        var qualityMultipliers = possibleQualities
            .Select(_options.GetQualityStatMultiplier)
            .ToList();
        var minimumBudget = tierAndSlotBudget * qualityMultipliers.Min();
        var maximumBudget = tierAndSlotBudget * qualityMultipliers.Max();
        var minimum = EquipmentBudgetAllocator.Allocate(
            targetTier,
            minimumBudget * 0.95d,
            design.InitialStatProfile);
        var maximum = EquipmentBudgetAllocator.Allocate(
            targetTier,
            maximumBudget * 1.05d,
            design.InitialStatProfile);

        return design.InitialStatProfile
            .Where(x => x.Value > 0)
            .Select(x => new CraftedAttributeRange(
                x.Key,
                (float)minimum.AddedPoints.GetValueOrDefault(x.Key),
                (float)maximum.AddedPoints.GetValueOrDefault(x.Key)))
            .ToList();
    }
}
