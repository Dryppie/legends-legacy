using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Slots;

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
}