using Application.Interfaces.Services.LL;
using Domain.Components.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
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

    public async Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetInventoryByIdAsync(characterId, cancellationToken);
        var character = await _characterService.GetMyCharacterOverviewAsync(characterId, cancellationToken);
        //var stats = await _attributeService.GetAttributesByCharacterIdAsync(characterId, cancellationToken);
        AttributeCalculator.CalculateBaseAttributes(character);
        foreach (var inventoryItem in inventory.InventoryItems)
        {
            if (inventoryItem.Item is EssenceItem essenceItem)
            {
                essenceItem.Essence.Active.Description = _essenceDescriptionService.BuildAbilityDescription(essenceItem.Essence.Active, character.BaseCombatAttributes);
                essenceItem.Essence.Passive.Description = _essenceDescriptionService.BuildAbilityDescription(essenceItem.Essence.Passive, character.BaseCombatAttributes);
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