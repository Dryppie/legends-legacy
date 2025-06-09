using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Application.UseCases._AdminDashboard.Items.Dtos;
public class ItemBaseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public List<ItemAttributeModifier> AttributeModifiers { get; set; } = [];
}