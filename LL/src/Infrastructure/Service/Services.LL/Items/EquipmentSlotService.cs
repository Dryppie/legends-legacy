using Application.Interfaces.Services.LL.Items;
using Domain.Models.Items.Equipments;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.CombatStyles;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Inventories;

namespace Services.LL.Items;
public class EquipmentSlotService : IEquipmentSlotService
{
    private readonly IEquipmentSlotRepository _equipmentSlotRepository;
    private readonly IInventoryRepository _inventory;
    private readonly ICombatStyleMutationBoundary? _buildBoundary;
    public EquipmentSlotService(IEquipmentSlotRepository equipmentSlotRepository,
        IInventoryRepository inventory, ICombatStyleMutationBoundary? buildBoundary = null)
    {
        _equipmentSlotRepository = equipmentSlotRepository;
        _inventory = inventory;
        _buildBoundary = buildBoundary;
    }

    public async Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken);

    public async Task<EquipmentEquipResult> EquipEquipmentAsync(Guid entityId, Guid equipmentId, EquipmentSlotType? slotType, CancellationToken cancellationToken)
    {
        var item = await _inventory.GetInventoryItemAsync(entityId, equipmentId, cancellationToken);
        if (item?.ItemInstance is not EquipmentInstance { ProgressionData: not null })
            return EquipmentEquipResult.Fail("Choose equipment from your inventory.");
        if (_buildBoundary is not null && await _buildBoundary.PrepareMutationAsync(entityId, cancellationToken) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        return await _equipmentSlotRepository.EquipEquipmentAsync(entityId, equipmentId, slotType, cancellationToken);
    }

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken)
    {
        if (_buildBoundary is not null && await _buildBoundary.PrepareMutationAsync(entityId, cancellationToken) is { } blocked)
            throw new CombatStyleConfigurationException(blocked);
        return await _equipmentSlotRepository.UnequipEquipmentAsync(entityId, slotType, cancellationToken);
    }

}
