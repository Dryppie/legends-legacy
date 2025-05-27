using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Items.Equipments;
public class EquipmentInstance : ItemInstance
{
    public Rarity Rarity { get; set; } = Rarity.Common;
    public int? Potential { get; set; } = null;
    public int ItemXp { get; set; } = 0;
    public bool IsMasterpiece { get; set; } = false;
    public bool IsLevelingItem { get; set; } = false;
    [NotMapped]
    public EquipmentBase EquipmentBase => (EquipmentBase)ItemBase;
    [NotMapped]
    public List<ItemAttributeModifier> AttributeModifiers => EquipmentBase.AttributeModifiers
            .Select(attr => new ItemAttributeModifier(attr.AttributeType, (int)Math.Ceiling(attr.Amount * Boost), attr.ModifierType)).ToList();
    public float Boost => Rarity switch
    {
        Rarity.Common => 1.0f,
        Rarity.Uncommon => 1.10f,
        Rarity.Rare => 1.20f,
        Rarity.Epic => 1.35f,
        Rarity.Unique => 1.50f,
        Rarity.Legendary => 1.70f,
        Rarity.Legacy => 2.0f,
        _ => 1.0f
    };
}