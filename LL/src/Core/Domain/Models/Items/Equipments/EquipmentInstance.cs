using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments.Tools;

namespace Domain.Models.Items.Equipments;
public class EquipmentInstance : ItemInstance
{
    public Rarity Rarity { get; set; } = Rarity.Common;
    public ItemQuality Quality { get; set; } = ItemQuality.Standard;
    public string? BaseRecipeId { get; set; }
    public string? BlueprintId { get; set; }
    public string? CraftedName { get; set; }
    public int Tier { get; set; } = 1;
    public int? Potential { get; set; } = null;
    public int? MaxPotential { get; set; } = null;
    public int TemperingProgress { get; set; } = 0;
    public uint Version { get; set; }
    public int ItemXp { get; set; } = 0;
    public bool IsMasterpiece { get; set; } = false;
    public bool IsLevelingItem { get; set; } = false;
    [NotMapped]
    public EquipmentBase EquipmentBase => (EquipmentBase)ItemBase;

    [NotMapped]
    public string DisplayName => EquipmentBase.EquipmentType == EquipmentType.Tool
        ? ToolInstanceNaming.GetDisplayName(EquipmentBase.Name, Rarity)
        : CraftedName ?? EquipmentBase.Name;

    [NotMapped]
    public bool UsesRecipeStatBudget => !string.IsNullOrWhiteSpace(BaseRecipeId);

    /// <summary>
    /// Authored item-base modifiers are retained for legacy and directly granted equipment.
    /// Crafted equipment receives its complete combat budget through recipe-generated instance
    /// modifiers, so applying these modifiers as well would budget the same item twice.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers =>
        UsesRecipeStatBudget
            ? []
            : EquipmentBase?.AttributeModifiers
            .Select(attr => new ItemAttributeModifier(
                attr.AttributeType,
                GetBoostedBaseModifierAmount(
                    attr.AttributeType,
                    attr.Amount,
                    Rarity),
                attr.ModifierType))
            .ToList()
              ?? [];


    /// <summary>Modifiers that were added to *this* item as it levelled up.</summary>
    public List<InstanceAttributeModifier> InstanceModifiers { get; set; } = [];
    public List<ToolBonusModifier> ToolAffixes { get; set; } = [];
    public List<string> AffinityTags { get; set; } = [];

    [NotMapped]
    public List<AttributeModifierBase> AttributeModifiers =>
    [
        .. BaseModifiers,
        .. InstanceModifiers,
    ];

    [NotMapped]
    public IReadOnlyList<ToolBonusModifier> EffectiveToolBonuses =>
    [
        .. EquipmentBase.ToolBonuses,
        .. ToolAffixes,
    ];

    public float Boost => GetRarityBoost(Rarity);

    public static float GetRarityBoost(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1.0f,
        Rarity.Uncommon => 1.25f,
        Rarity.Rare => 1.75f,
        Rarity.Epic => 2.5f,
        Rarity.Unique => 3.50f,
        Rarity.Legendary => 4.75f,
        Rarity.Legacy => 6.0f,
        _ => 1.0f
    };

    public static int GetBoostedBaseModifierAmount(
        AttributeType attribute,
        float amount,
        Rarity rarity)
    {
        var definition = AttributeCatalog.Get(attribute);
        if (definition.Unit == AttributeUnit.PercentagePoints
            && definition.CapKind == AttributeCapKind.Fixed)
        {
            return Math.Min(
                (int)Math.Ceiling(amount),
                (int)Math.Floor(definition.MaximumValue!.Value));
        }

        var boosted = (int)Math.Ceiling(amount * GetRarityBoost(rarity));
        return definition.CapKind == AttributeCapKind.Fixed
               && definition.MaximumValue is { } maximum
            ? Math.Min(boosted, (int)Math.Floor(maximum))
            : boosted;
    }
}
