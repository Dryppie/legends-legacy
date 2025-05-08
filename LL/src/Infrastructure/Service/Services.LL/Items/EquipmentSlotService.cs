using Application.Interfaces.Services.LL.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;

namespace Services.LL.Items;
public class EquipmentSlotService : IEquipmentSlotService
{
    private readonly IEquipmentSlotRepository _equipmentSlotRepository;
    public EquipmentSlotService(IEquipmentSlotRepository equipmentSlotRepository)
    {
        _equipmentSlotRepository = equipmentSlotRepository;
    }

    public async Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken);

    public async Task<bool> EquipEquipmentAsync(Guid entityId, Guid equipmentId, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.EquipEquipmentAsync(entityId, equipmentId, cancellationToken);

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentType equipmentType, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.UnequipEquipmentAsync(entityId, equipmentType, cancellationToken);

}