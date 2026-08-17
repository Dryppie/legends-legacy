using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingItemPreviewDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public EquipmentType EquipmentType { get; init; }
    public Rarity Rarity { get; init; }
    public int Tier { get; init; }
    public int RequiredLevel { get; init; } = 1;
    public int StatModelVersion { get; init; } = EquipmentStatBudgetCatalog.BalanceVersion;
    public IReadOnlyList<CraftingAttributePreviewDto> Attributes { get; init; } = [];
    public IReadOnlyList<CraftingQualityChanceDto> QualityChances { get; init; } = [];
    public int MinimumStartingPotential { get; init; }
    public int MaximumStartingPotential { get; init; }
}

public sealed class CraftingAttributePreviewDto
{
    public AttributeType AttributeType { get; init; }
    public float BaseAmount { get; init; }
    public float MinimumCraftedAmount { get; init; }
    public float MaximumCraftedAmount { get; init; }
    public float MinimumTotalAmount => BaseAmount + MinimumCraftedAmount;
    public float MaximumTotalAmount => BaseAmount + MaximumCraftedAmount;
}

public sealed class CraftingQualityChanceDto
{
    public ItemQuality Quality { get; init; }
    public double ChancePercent { get; init; }
}
