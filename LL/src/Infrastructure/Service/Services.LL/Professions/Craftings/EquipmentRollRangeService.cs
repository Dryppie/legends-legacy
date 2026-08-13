using Application.Interfaces.Services.LL.Professions;
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
        var attributes = _statRollService
            .GetBaseStatRanges(equipment.EquipmentBase, design, equipment.Tier, qualities)
            .Select(range => new EquipmentAttributeRollRange(
                range.AttributeType,
                range.MinimumAmount,
                range.MaximumAmount))
            .ToList();

        var (minimumPotential, maximumPotential) = ResolvePotentialRange(equipment, qualities);
        return new EquipmentRollRange(minimumPotential, maximumPotential, attributes);
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
