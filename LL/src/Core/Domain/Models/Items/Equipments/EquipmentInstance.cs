using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments.Tools;
using Domain.Models.Guilds;
using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Items.Equipments;
public class EquipmentInstance : ItemInstance
{
    public Rarity Rarity { get; set; } = Rarity.Common;
    public ItemQuality Quality { get; set; } = ItemQuality.Standard;
    public string? BaseRecipeId { get; set; }
    public string? BlueprintId { get; set; }
    public string? CraftedName { get; set; }
    public int Tier { get; set; } = 1;
    public int StatModelVersion { get; set; } = EquipmentStatBudgetCatalog.LegacyBalanceVersion;
    public int? Potential { get; set; } = null;
    public int? MaxPotential { get; set; } = null;
    public int TemperingProgress { get; set; } = 0;
    public uint Version { get; set; }
    public int ItemXp { get; set; } = 0;
    public bool IsMasterpiece { get; set; } = false;
    public bool IsLevelingItem { get; set; } = false;

    /// <summary>
    /// Carries the owning character's favorite preference while this item is equipped.
    /// The value is copied from the inventory row on equip and back to the new row on unequip.
    /// </summary>
    public bool IsFavorite { get; set; }

    [NotMapped]
    public EquipmentBase EquipmentBase => (EquipmentBase)ItemBase;

    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(CraftedName)
        ? CraftedName.Trim()
        : EquipmentBase.EquipmentType == EquipmentType.Tool
            ? ToolInstanceNaming.GetDisplayName(EquipmentBase.Name, Rarity)
            : EquipmentBase.Name;

    [NotMapped]
    public bool UsesRecipeStatBudget => !string.IsNullOrWhiteSpace(BaseRecipeId);

    [NotMapped]
    public bool UsesProgressionNormalizedRatings =>
        UsesRecipeStatBudget
        && StatModelVersion >= EquipmentStatBudgetCatalog.BalanceVersion;

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
    public GuildVaultItem? GuildVaultItem { get; set; }

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

    public static float GetRarityBoost(Rarity rarity) =>
        (float)CalculateRarityBoost(rarity);

    private static decimal CalculateRarityBoost(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1m,
        Rarity.Uncommon => 1.1m,
        Rarity.Rare => 1.3m,
        Rarity.Epic => 1.6m,
        Rarity.Unique => 2m,
        Rarity.Legendary => 2.5m,
        Rarity.Legacy => 3m,
        _ => 1m
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

        var boostedAmount = (decimal)amount * CalculateRarityBoost(rarity);
        var boosted = (int)decimal.Ceiling(boostedAmount);
        return definition.CapKind == AttributeCapKind.Fixed
               && definition.MaximumValue is { } maximum
            ? Math.Min(boosted, (int)Math.Floor(maximum))
            : boosted;
    }
}
