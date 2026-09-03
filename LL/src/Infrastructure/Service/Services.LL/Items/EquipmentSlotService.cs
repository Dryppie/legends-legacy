using Application.Interfaces.Services.LL.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Inventories;

namespace Services.LL.Items;
public class EquipmentSlotService : IEquipmentSlotService
{
    private readonly IEquipmentSlotRepository _equipmentSlotRepository;
    private readonly IInventoryRepository _inventory;
    public EquipmentSlotService(IEquipmentSlotRepository equipmentSlotRepository,
        IInventoryRepository inventory)
    {
        _equipmentSlotRepository = equipmentSlotRepository;
        _inventory = inventory;
    }

    public async Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken);

    public async Task<EquipmentEquipResult> EquipEquipmentAsync(Guid entityId, Guid equipmentId, EquipmentSlotType? slotType, CancellationToken cancellationToken)
    {
        var item = await _inventory.GetInventoryItemAsync(entityId, equipmentId, cancellationToken);
        if (item?.ItemInstance is not EquipmentInstance { ProgressionData: not null })
            return EquipmentEquipResult.Fail("Choose equipment from your inventory.");
        return await _equipmentSlotRepository.EquipEquipmentAsync(entityId, equipmentId, slotType, cancellationToken);
    }

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.UnequipEquipmentAsync(entityId, slotType, cancellationToken);

}
