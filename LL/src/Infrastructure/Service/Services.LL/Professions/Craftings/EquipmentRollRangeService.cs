using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;

namespace Services.LL.Professions.Craftings;

public sealed class EquipmentRollRangeService : IEquipmentRollRangeService
{
    private static readonly ItemQuality[] BaselineQualities =
    [
        ItemQuality.Crude,
        ItemQuality.Standard,
        ItemQuality.Fine,
        ItemQuality.Exceptional
    ];

    private readonly ICraftingDefinitionProvider _definitions;
    private readonly IItemStatRollService _statRollService;
    private readonly CraftingBalanceOptions _options;

    public EquipmentRollRangeService(
        ICraftingDefinitionProvider definitions,
        IItemStatRollService statRollService,
        IOptions<CraftingBalanceOptions>? options = null)
    {
        _definitions = definitions;
        _statRollService = statRollService;
        _options = options?.Value ?? new CraftingBalanceOptions();
    }

    public EquipmentRollRange? Resolve(EquipmentInstance equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.BaseRecipeId)) return null;

        var recipe = _definitions.GetRecipe(equipment.BaseRecipeId);
        var blueprint = string.IsNullOrWhiteSpace(equipment.BlueprintId)
            ? null
            : _definitions.GetBlueprint(equipment.BlueprintId);
        if (recipe is null || (!string.IsNullOrWhiteSpace(equipment.BlueprintId) && blueprint is null))
            return null;

        var design = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);
        var qualities = equipment.Quality == ItemQuality.Masterwork
            ? [.. BaselineQualities, ItemQuality.Masterwork]
            : BaselineQualities;
        var rarityUpgrades = TemperingConstants.GetRarityUpgradeCount(equipment.Rarity);
        var rarityStepBudget = ResolveRarityStepBudget(equipment, qualities);
        var attributes = _statRollService
            .GetBaseStatRanges(equipment.EquipmentBase, design, equipment.Tier, qualities)
            .Select(range => new EquipmentAttributeRollRange(
                range.AttributeType,
                range.MinimumAmount,
                ApplyRarityHeadroom(range, equipment.Tier, rarityUpgrades, rarityStepBudget)))
            .ToList();

        var (minimumPotential, maximumPotential) = ResolvePotentialRange(equipment, qualities);
        return new EquipmentRollRange(minimumPotential, maximumPotential, attributes);
    }

    /// <summary>
    /// Budget funding a single rarity step's directed stat improvement. Mirrors the
    /// budget that <c>TemperingMechanicsService.TryApplyDirectedImprovement</c> spends,
    /// except that the highest quality in the displayed band is used instead of the
    /// item's current quality, because the range spans that whole band and quality can
    /// still rise after the improvement was granted.
    /// </summary>
    private double ResolveRarityStepBudget(
        EquipmentInstance equipment,
        IReadOnlyCollection<ItemQuality> qualities)
    {
        var tier = Math.Max(EquipmentStatBudgetCatalog.MinimumTier, equipment.Tier);
        return TemperingConstants.GetDirectedImprovementBudget(tier)
            * _options.GetSlotBudgetWeight(equipment.EquipmentBase.EquipmentType)
            * qualities.Max(_options.GetQualityStatMultiplier);
    }

    /// <summary>
    /// Crafted ranges only model craft-time variance (tier x slot x quality x +/-5%), but
    /// every rarity step permanently adds a directed improvement to the instance modifiers.
    /// Without accounting for that growth a tempered item renders above its own maximum.
    /// Each step can land entirely on one attribute, so every attribute is granted the full
    /// headroom; the minimum is left alone because a rarity upgrade never lowers a roll.
    /// </summary>
    private static float ApplyRarityHeadroom(
        CraftedAttributeRange range,
        int tier,
        int rarityUpgrades,
        double stepBudget)
    {
        if (rarityUpgrades <= 0
            || stepBudget <= 0d
            || !EquipmentStatBudgetCatalog.IsKnown(range.AttributeType))
        {
            return range.MaximumAmount;
        }

        var effectiveTier = Math.Max(EquipmentStatBudgetCatalog.MinimumTier, tier);
        var costPerPoint = EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(
            range.AttributeType,
            effectiveTier);
        if (costPerPoint <= 0d) return range.MaximumAmount;

        // Matches how the improvement is materialized during tempering: direct
        // percentages buy fractional value, everything else rounds up to at least one
        // whole point per step.
        var rawIncrease = stepBudget / costPerPoint;
        var perStepIncrease = EquipmentStatBudgetCatalog.IsDirectPercentage(range.AttributeType)
            ? AttributeValueQuantizer.Quantize(range.AttributeType, rawIncrease)
            : Math.Max(1d, Math.Round(rawIncrease, MidpointRounding.AwayFromZero));

        return (float)AttributeValueQuantizer.Quantize(
            range.AttributeType,
            range.MaximumAmount + (rarityUpgrades * perStepIncrease));
    }

    private (int Minimum, int Maximum) ResolvePotentialRange(
        EquipmentInstance equipment,
        IReadOnlyCollection<ItemQuality> qualities)
    {
        var rolledPotential = equipment.MaxPotential ?? equipment.Potential ?? 0;
        var tierBase = 100 + (Math.Max(equipment.Tier, 1) * 100);
        var slotBase = tierBase * _options.GetPotentialSlotWeight(equipment.EquipmentBase.EquipmentType);
        var selectedQualityBase = (int)Math.Round(
            slotBase * _options.GetPotentialQualityMultiplier(equipment.Quality));
        var progressionBonus = rolledPotential - selectedQualityBase;
        var potentialValues = qualities
            .Select(quality => (int)Math.Round(
                slotBase * _options.GetPotentialQualityMultiplier(quality)) + progressionBonus)
            .ToList();

        return (potentialValues.Min(), potentialValues.Max());
    }
}
