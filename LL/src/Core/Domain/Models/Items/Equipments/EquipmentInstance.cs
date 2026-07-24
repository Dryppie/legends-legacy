using System.ComponentModel.DataAnnotations.Schema;
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
    public IReadOnlyCollection<ItemAttributeModifier> BaseModifiers =>
        EquipmentBase?.AttributeModifiers
            .Select(attr => new ItemAttributeModifier(attr.AttributeType, (int)Math.Ceiling(attr.Amount * Boost), attr.ModifierType))
            .ToList()
        ?? new List<ItemAttributeModifier>(0);


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

    public float Boost => Rarity switch
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
}
