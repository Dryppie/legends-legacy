using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Services.AdminDashboard.Items;
public class EquipmentToJsonDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Stackable { get; set; } = true;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public ICollection<ItemAttributeModifierToJsonDto> AttributeModifiers { get; set; } = [];
    public int AttackSpeed { get; set; } = 0;
    public int Magnitude { get; set; } = 0;
    public int MagnitudeRange { get; set; } = 0;
    public AttributeType ScalingAttribute { get; set; } = AttributeType.AttackPower;
    public float ScalingAmount { get; set; } = 0.1f;
}