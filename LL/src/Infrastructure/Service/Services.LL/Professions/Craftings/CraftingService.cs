using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;

namespace Services.LL.Professions.Craftings;
public class CraftingService : ICraftingService
{
    private readonly ICraftingRepository _craftingRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IProfessionService _professionService;
    private readonly IRecipeService _recipeService;

    public CraftingService(ICraftingRepository cr, IInventoryService invS, IProfessionService ps, IRecipeService rs)
    {
        _craftingRepository = cr;
        _inventoryService = invS;
        _professionService = ps;
        _recipeService = rs;
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
            Potential = 100
        };
        var inventoryItem = new InventoryItem()
        {
            InventoryId = characterId,
            ItemInstanceId = equipmentInstance.Id,
            Quantity = 1,
            ItemInstance = equipmentInstance,
        };

        await _inventoryService.AddItemsToInventory(characterId, [inventoryItem], cancellationToken);

        // Craft the item
        //var crafted = await _craftingRepository.CraftItemFromRecipeAsync(characterId, recipeId, cancellationToken);
        //if (!crafted) return null;

        return inventoryItem;
    }

    public async Task PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        var actionDetails = (characterAction.ActionDetails as CraftingActionDetails)!;
        var produced = new List<InventoryItem>();

        //while (actionsToPerform > 0 && actionDetails.CraftingQueueItems.Count > 0)
        //{
        //    var current = actionDetails.CraftingQueueItems.First();
        //    var spend = Math.Min(actionsToPerform, current.RemainingTicks);

        //    current.RemainingTicks -= spend;
        //    actionsToPerform -= spend;

        //    if (current.Potential is > 0)
        //        current.Potential = Math.Max(0, current.Potential.Value - spend);

        //    if (current.RemainingTicks == 0)
        //    {
        //        produced.Add(await _craftService.FinishAsync(current, cancellationToken));
        //        details.Queue.RemoveAt(0); // next item slides up
        //    }
        //}


    }
}