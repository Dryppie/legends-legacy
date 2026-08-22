using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Items.Equipments;
public interface IEquipmentSlotRepository
{
    Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken);
    Task<EquipmentEquipResult> EquipEquipmentAsync(Guid entityId, Guid equipmentId, EquipmentSlotType? slotType, CancellationToken cancellationToken);
    Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken);
}
