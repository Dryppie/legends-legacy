using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Services.LL.Interfaces;

namespace Services.LL.Professions.Craftings;
public class CraftingService : ICraftingService
{
    private readonly ICraftingRepository _craftingRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IProfessionService _professionService;
    private readonly IRecipeService _recipeService;
    private readonly ITemperingService _temperingService;

    public CraftingService(ICraftingRepository cr, IInventoryService invS, IProfessionService ps, IRecipeService rs, ITemperingService ts)
    {
        _craftingRepository = cr;
        _inventoryService = invS;
        _professionService = ps;
        _recipeService = rs;
        _temperingService = ts;
    }

    public async Task<InventoryItem?> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken)
    {
        // Load the recipe
        var recipe = await _recipeService.GetRecipeByIdAsync(recipeId, cancellationToken);
        if (recipe == null) return null;

        // Check inventory for required materials
        var removedMaterials = await _inventoryService.TryRemoveItemsAsync(characterId, [.. recipe.Materials], cancellationToken);
        if (!removedMaterials)return null;

        var professionType = recipe.CraftType switch
        {
            CraftType.ArmorForging => ProfessionType.ArmorForging,
            CraftType.JewelryCrafting => ProfessionType.JewelryCrafting,
            CraftType.WeaponSmithing => ProfessionType.WeaponSmithing,
            _ => throw new NotImplementedException()
        };

        // Check profession level
        var professionLevel = await _professionService.GetProfessionLevelAsync(characterId, professionType, cancellationToken);
        if (professionLevel < recipe.LevelRequirement) return null;

        var equipmentInstance = new EquipmentInstance()
        {
            Id = Guid.NewGuid(),
            ItemBaseId = recipe.ItemId,
            ItemBase = recipe.Item,
            Potential = 1000 + (10 * professionLevel),

        };
        var inventoryItem = new InventoryItem()
        {
            InventoryId = characterId,
            ItemInstanceId = equipmentInstance.Id,
            Quantity = 1,
            ItemInstance = equipmentInstance,
        };

        await _inventoryService.AddItemsToInventory(characterId, [inventoryItem], cancellationToken);

        return inventoryItem;
    }

    public async Task PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        var actionDetails = (characterAction.ActionDetails as CraftingActionDetails)!;
        var produced = new List<InventoryItem>();
        
        while (actionsToPerform > 0 && actionDetails.CraftingQueueItems.Count > 0)
        {
            var current = actionDetails.CraftingQueueItems.First();
            var spend = Math.Min(actionsToPerform, current.EquipmentInstance.Potential ?? 0);

            current.EquipmentInstance.Potential -= spend;
            actionsToPerform -= spend;
            var rng = Random.Shared;
            for (int i = 0; i < spend; i++)
            {
                _temperingService.HandleTempering(current, rng);
            }

            if (current.EquipmentInstance.Potential == 0)
            {
                actionDetails.CraftingQueueItems.Remove(current); // next item slides up
                await _inventoryService.AddItemInstanceBackToInventory(characterAction.CharacterId, current.EquipmentInstance, cancellationToken);
            }
        }
        if (actionDetails.CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
            characterAction.ActionDetails = null;
            return;
        }
    }

    public async Task<bool> RemoveCraftingQueueItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken)
    {
        var equipmentInstance = await _craftingRepository.RemoveCraftingQueueItemAndReturnItemAsync(characterId, queueItemId, cancellationToken);
        if (equipmentInstance == null) return false;

        var itemAdded = await _inventoryService.AddItemInstanceBackToInventory(characterId, equipmentInstance, cancellationToken);
        if (itemAdded)
        {
            await _craftingRepository.SaveChangesAsync(cancellationToken);
        }
        return itemAdded;
    }
}