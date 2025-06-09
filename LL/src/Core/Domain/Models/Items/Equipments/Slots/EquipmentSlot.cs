using Domain.Models.Entities;

namespace Domain.Models.Items.Equipments.Slots;
public class EquipmentSlot
{
    public Guid EntityId { get; set; }
    public Entity Entity { get; set; } = null!;
    public Guid? EquipmentInstanceId { get; set; }
    public EquipmentInstance? EquipmentInstance { get; set; }
    public EquipmentSlotType EquipmentSlotType { get; set; }
}