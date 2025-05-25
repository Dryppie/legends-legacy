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
        var hasRequiredLevel = await _professionService.CanPerformProfession(characterId, professionType, recipe.LevelRequirement, cancellationToken);
        if (!hasRequiredLevel) return null;

        var equipmentInstance = new EquipmentInstance()
        {
            Id = Guid.NewGuid(),
            ItemBaseId = recipe.ItemId,
            ItemBase = recipe.Item,
            Potential = 1000,

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
                //produced.Add(await _craftService.FinishAsync(current, cancellationToken));
                actionDetails.CraftingQueueItems.Remove(current); // next item slides up
                var inventoryItem = new InventoryItem()
                {
                    InventoryId = characterAction.CharacterId,
                    ItemInstanceId = current.EquipmentInstance.Id,
                    ItemInstance = current.EquipmentInstance,
                    Quantity = 1,
                };

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
}