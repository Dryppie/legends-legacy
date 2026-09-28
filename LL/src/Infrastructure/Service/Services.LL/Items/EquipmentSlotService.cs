using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Outbox;
using Services.LL.Analytics;
using Domain.Models.Items.Equipments;
using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.CombatStyles;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Inventories;
using Domain.Models.Analytics;

namespace Services.LL.Items;
public class EquipmentSlotService : IEquipmentSlotService
{
    private readonly IEquipmentSlotRepository _equipmentSlotRepository;
    private readonly IInventoryRepository _inventory;
    private readonly IGameEventOutbox? _outbox;
    private readonly IItemizationChoiceRepository? _choices;
    private readonly ICombatStyleMutationBoundary? _buildBoundary;
    public EquipmentSlotService(IEquipmentSlotRepository equipmentSlotRepository,
        IInventoryRepository inventory, ICombatStyleMutationBoundary? buildBoundary = null, IGameEventOutbox? outbox = null, IItemizationChoiceRepository? choices = null)
    {
        _equipmentSlotRepository = equipmentSlotRepository;
        _inventory = inventory;
        _outbox = outbox;
        _choices = choices;
        _buildBoundary = buildBoundary;
    }

    public Task<EquipmentInstance?> GetLinkedEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken) =>
        _equipmentSlotRepository.GetLinkedEquipmentAsync(equipmentId, cancellationToken);

    public async Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(Guid entityId, CancellationToken cancellationToken) =>
        await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken);

    public async Task<EquipmentEquipResult> EquipEquipmentAsync(Guid entityId, Guid equipmentId, EquipmentSlotType? slotType, CancellationToken cancellationToken)
    {
        var item = await _inventory.GetInventoryItemAsync(entityId, equipmentId, cancellationToken);
        if (item?.ItemInstance is not EquipmentInstance { ProgressionData: not null })
            return EquipmentEquipResult.Fail("Choose equipment from your inventory.");
        if (_buildBoundary is not null && await _buildBoundary.PrepareMutationAsync(entityId, cancellationToken) is { } blocked)
            return EquipmentEquipResult.Fail(blocked);
        var before = await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken);
        var priorItems = before.Where(x => x.EquipmentInstance?.ProgressionData is not null).Select(x => x.EquipmentInstance!.ProgressionData!).DistinctBy(x => x.State.Id).ToArray();
        var priorIds = priorItems.Select(x => x.State.Id).ToArray();
        var choices = _outbox is not null && _choices is not null
            ? await _choices.CaptureAsync(entityId, cancellationToken) : null;
        var result = await _equipmentSlotRepository.EquipEquipmentAsync(entityId, equipmentId, slotType, cancellationToken);
        if (result.Succeeded && _outbox is not null)
        {
            var after = await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken);
            var afterIds = after
                .Where(x => x.EquipmentInstanceId.HasValue).Select(x => x.EquipmentInstanceId!.Value).ToHashSet();
            var equippedSlot = after.Where(x => x.EquipmentInstanceId == equipmentId)
                .OrderBy(x => x.EquipmentSlotType).Select(x => (EquipmentSlotType?)x.EquipmentSlotType).FirstOrDefault();
            var operation = Guid.NewGuid().ToString("N");
            var replaced = priorItems.Where(x => !afterIds.Contains(x.State.Id)).ToArray();
            foreach (var previous in replaced) await _outbox.RecordEquipmentAsync("unequipped", operation, entityId, previous, "manual", cancellationToken);
            if (!priorIds.Contains(equipmentId)) await _outbox.RecordEquipmentAsync("equipped", operation, entityId, ((EquipmentInstance)item.ItemInstance).ProgressionData!, "manual", cancellationToken, replaced.Select(x => x.State.Id).ToArray(), choices?.ForItem(equipmentId, equippedSlot));
        }
        return result;
    }

    public async Task<bool> UnequipEquipmentAsync(Guid entityId, EquipmentSlotType slotType, CancellationToken cancellationToken)
    {
        if (_buildBoundary is not null && await _buildBoundary.PrepareMutationAsync(entityId, cancellationToken) is { } blocked)
            throw new CombatStyleConfigurationException(blocked);
        var before = (await _equipmentSlotRepository.GetEquipmentSlotsByEntityIdAsync(entityId, cancellationToken))
            .FirstOrDefault(x => x.EquipmentSlotType == slotType)?.EquipmentInstance?.ProgressionData;
        var result = await _equipmentSlotRepository.UnequipEquipmentAsync(entityId, slotType, cancellationToken);
        if (result && before is not null && _outbox is not null)
            await _outbox.RecordEquipmentAsync("unequipped", Guid.NewGuid().ToString("N"), entityId, before, "manual", cancellationToken);
        return result;
    }

}
