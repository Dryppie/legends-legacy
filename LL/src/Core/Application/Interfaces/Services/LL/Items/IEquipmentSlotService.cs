using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;

namespace Application.Interfaces.Services.LL.Items;
public interface IEquipmentSlotService
{
    Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken);
    Task<bool> EquipEquipmentAsync(Guid entityId, Guid equipmentId, EquipmentSlotType? slotType, CancellationToken cancellationToken);
    Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken);
}
