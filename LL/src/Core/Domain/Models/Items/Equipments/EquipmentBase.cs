using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Domain.Models.Items.Equipments;
public class EquipmentBase : ItemBase
{
    public EquipmentBase() : base()
    {
        ItemType = ItemType.Equipment;
        AttributeModifiers = [];
        Stackable = false;
    }
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
    public int AttackSpeed { get; set; } = 0;
    public int Magnitude { get; set; } = 0;
    public int MagnitudeRange { get; set; }
    public GatheringType? GatheringType { get; set; }
    public AttributeType ScalingAttribute { get; set; } = AttributeType.Power;
    public float ScalingAmount { get; set; } = 0.1f;
}