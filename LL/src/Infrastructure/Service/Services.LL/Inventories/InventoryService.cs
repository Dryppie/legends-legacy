using Application.Interfaces.Services.LL;
using Services.LL.Analytics;
using Domain.Models.Items.Equipments;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Inventories;
using Domain.Models.Analytics;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;

namespace Services.LL.Inventories;
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IGameEventOutbox? _outbox;
    private readonly IItemizationChoiceRepository? _choices;

    public InventoryService(IInventoryRepository inventoryRepository, IGameEventOutbox? outbox = null, IItemizationChoiceRepository? choices = null)
    {
        _inventoryRepository = inventoryRepository;
        _outbox = outbox;
        _choices = choices;
    }

    public async Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _inventoryRepository.GetInventoryByIdAsync(characterId, cancellationToken);

    public async Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> loot,
        string acquisitionSource,
        CancellationToken cancellationToken)
    {
        var choices = await CaptureAwardChoicesAsync(characterId, loot, cancellationToken);
        await _inventoryRepository.AddItemsToInventory(characterId, loot, acquisitionSource, cancellationToken);
        await RecordEquipmentFoundAsync(characterId, loot, acquisitionSource, cancellationToken, choices);
    }

    public async Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> loot,
        string acquisitionSource,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var choices = await CaptureAwardChoicesAsync(characterId, loot, cancellationToken);
        await _inventoryRepository.AddItemsToInventory(
            characterId,
            loot,
            acquisitionSource,
            correlationId,
            cancellationToken);
        await RecordEquipmentFoundAsync(characterId, loot, acquisitionSource, cancellationToken, choices);
    }

    private async Task<ItemizationChoiceInventory?> CaptureAwardChoicesAsync(Guid characterId,
        IReadOnlyCollection<InventoryItem> loot, CancellationToken ct)
    {
        var awards = loot.Where(x => x.Quantity > 0).Select(x => x.ItemInstance).OfType<EquipmentInstance>()
            .Where(x => x.ProgressionData is not null).Select(x => x.ProgressionData!).ToArray();
        if (_outbox is null || _choices is null || awards.Length == 0) return null;
        return (await _choices.CaptureAsync(characterId, ct))?.WithAwards(awards);
    }

    private async Task RecordEquipmentFoundAsync(
        Guid characterId,
        IReadOnlyCollection<InventoryItem> loot,
        string acquisitionSource,
        CancellationToken cancellationToken, ItemizationChoiceInventory? choices)
    {
        if (_outbox is not null)
            foreach (var item in loot.Where(x => x.Quantity > 0).Select(x => x.ItemInstance).OfType<EquipmentInstance>().Where(x => x.ProgressionData is not null))
                await _outbox.RecordEquipmentAsync("awarded", item.ProgressionData!.State.Provenance.AwardId, characterId, item.ProgressionData, acquisitionSource, cancellationToken, choices: choices?.ForItem(item.Id));
        if (_outbox is null || acquisitionSource is not
            (ItemAcquisitionSources.CombatReward or ItemAcquisitionSources.DungeonReward or ItemAcquisitionSources.RaidReward))
            return;

        var quantity = loot.Where(item => item.ItemInstance.ItemBase.ItemType == ItemType.Equipment)
            .Sum(item => Math.Max(0, item.Quantity));
        if (quantity > 0)
            await _outbox.EnqueueAsync(GameEventTypes.EquipmentFound,
                new EquipmentFoundPayload(characterId, quantity), characterId, null, cancellationToken);
    }

    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await _inventoryRepository.CreateInventoryAsync(characterId, cancellationToken);
    }

    public async Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken)
    {
        var inventoryItem = await _inventoryRepository.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);
        if (inventoryItem == null) return false;
        if (inventoryItem.ItemInstance.ItemBaseId == Domain.Models.Nobility.NobilityBenefits.SignetItemId) return false;

        inventoryItem.Quantity--;
        if (inventoryItem.Quantity <= 0)
            _inventoryRepository.RemoveInventoryItem(inventoryItem);

        return true;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
        await _inventoryRepository.GetInventoryItemAsync(characterId, itemInstanceId, cancellationToken);

    public async Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
        await _inventoryRepository.MarkItemSeenAsync(characterId, itemInstanceId, cancellationToken);

    public async Task<bool> SetItemFavoriteAsync(
        Guid characterId,
        Guid itemInstanceId,
        bool isFavorite,
        CancellationToken cancellationToken) =>
        await _inventoryRepository.SetItemFavoriteAsync(characterId, itemInstanceId, isFavorite, cancellationToken);

    public async Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketplaceListing, CancellationToken cancellationToken)
    {
        var removed = await _inventoryRepository.TryRemoveItemsForMarketPlaceListingAsync(characterId, marketplaceListing, cancellationToken);
        if (removed && _outbox is not null && marketplaceListing.ItemInstance is EquipmentInstance { ProgressionData: { } data })
            await _outbox.RecordEquipmentAsync("listed", marketplaceListing.Id.ToString("N"), characterId, data, "marketplace", cancellationToken);
        return removed;
    }

    public async Task<InventoryItem?> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken)
    {
        return await _inventoryRepository.AddItemInstanceBackToInventory(characterId, itemInstance, cancellationToken);
    }

    public async Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem inventoryItem, CancellationToken cancellationToken)
    {
        await _inventoryRepository.AddItemToInventoryFromMarketPlace(characterId, inventoryItem, cancellationToken);
    }

    public async Task<InventoryTransferResult> TransferItemAsync(
        Guid senderCharacterId,
        Guid recipientCharacterId,
        Guid itemInstanceId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryRepository.TransferItemAsync(
            senderCharacterId,
            recipientCharacterId,
            itemInstanceId,
            quantity,
            cancellationToken);
        if (result.IsSuccess && _outbox is not null && result.TransferredItem!.ItemInstance is EquipmentInstance { ProgressionData: { } data })
            await _outbox.RecordEquipmentAsync("transferred", result.TransferRecord!.Id.ToString("N"), senderCharacterId,
                data, "player-transfer", cancellationToken);
        return result;
    }
}
