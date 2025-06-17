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
    private IReadOnlyCollection<ItemAttributeModifier> TemplateModifiers =>
        [.. EquipmentBase.AttributeModifiers];


    /// <summary>Modifiers that were added to *this* item as it levelled up.</summary>
    //public List<ItemAttributeModifier> InstanceModifiers { get; private set; } = new();

    [NotMapped]
    public List<ItemAttributeModifier> AttributeModifiers =>
    [
        .. TemplateModifiers.Select(attr => new ItemAttributeModifier(attr.AttributeType, (int)Math.Ceiling(attr.Amount * Boost), attr.ModifierType)),
        //.. InstanceModifiers,
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