using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Professions.Crafting;
using Services.LL.Interfaces;

namespace Services.LL.Inventories;
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEssenceDescriptionService _essenceDescriptionService;
    private readonly ICharacterService _characterService;
    public InventoryService(IInventoryRepository inventoryRepository, IEssenceDescriptionService essenceDescriptionService, ICharacterService characterService)
    {
        _inventoryRepository = inventoryRepository;
        _essenceDescriptionService = essenceDescriptionService;
        _characterService = characterService;
    }

    public async Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetInventoryByIdAsync(characterId, cancellationToken);
        var character = await _characterService.GetMyCharacterOverviewAsync(characterId, cancellationToken); // Called to calculate correct description for abilities (X-Y damage / heal)
        if (character == null) return null;

        foreach (var inventoryItem in inventory.InventoryItems)
        {
            if (inventoryItem.ItemInstance is EssenceItemInstance ei && ei.ItemBase is EssenceItemBase eib)
            {
                eib.Essence.Active.Description = _essenceDescriptionService.BuildAbilityDescription(eib.Essence.Active, character.BaseCombatAttributes);
                eib.Essence.Passive.Description = _essenceDescriptionService.BuildAbilityDescription(eib.Essence.Passive, character.BaseCombatAttributes);
            }
        }

        return inventory;
    }

    public async Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        await _inventoryRepository.AddItemsToInventory(characterId, loot, cancellationToken);
    }
    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await _inventoryRepository.CreateInventoryAsync(characterId, cancellationToken);
    }

    public async Task<bool> TryRemoveMaterialsForCraftingAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken)
    {
        var requiredByItemId = materials
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Quantity));

        return await _inventoryRepository.TryRemoveItemsAsync(characterId, requiredByItemId, cancellationToken);
    }

    public async Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken)
    {
        var requiredByItemId = materials
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Quantity));

        return await _inventoryRepository.TryRemoveItemsAsync(characterId, requiredByItemId, cancellationToken);
    }

    public async Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken)
    {
        return await _inventoryRepository.AddItemInstanceBackToInventory(characterId, itemInstance, cancellationToken);
    }
}