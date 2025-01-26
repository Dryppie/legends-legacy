using Application.Interfaces.Services.LL;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Services.LL.Interfaces;

namespace Services.LL.Inventories;
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEssenceDescriptionService _essenceDescriptionService;
    private readonly IAttributeService _attributeService;
    public InventoryService(IInventoryRepository inventoryRepository, IEssenceDescriptionService essenceDescriptionService, IAttributeService attributeService)
    {
        _inventoryRepository = inventoryRepository;
        _essenceDescriptionService = essenceDescriptionService;
        _attributeService = attributeService;
    }

    public async Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetInventoryByIdAsync(characterId, cancellationToken);
        var stats = await _attributeService.GetAttributesByCharacterIdAsync(characterId, cancellationToken);
        foreach (var inventoryItem in inventory.InventoryItems)
        {
            if (inventoryItem.Item is EssenceItem essenceItem)
            {
                essenceItem.Essence.Active.Description = _essenceDescriptionService.BuildAbilityDescription(essenceItem.Essence.Active, stats);
                essenceItem.Essence.Passive.Description = _essenceDescriptionService.BuildAbilityDescription(essenceItem.Essence.Passive, stats);
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

}