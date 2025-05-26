using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Services.LL.Interfaces;
using Services.LL.Levels;

namespace Services.LL.Professions.Craftings;
public class CraftingService : ICraftingService
{
    private readonly ICraftingRepository _craftingRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IProfessionService _professionService;
    private readonly IRecipeService _recipeService;
    private readonly ITemperingService _temperingService;
    private readonly ILevelingService _levelingService;

    public CraftingService(ICraftingRepository cr, IInventoryService invS, IProfessionService ps, IRecipeService rs, ITemperingService ts, ILevelingService ls)
    {
        _craftingRepository = cr;
        _inventoryService = invS;
        _professionService = ps;
        _recipeService = rs;
        _temperingService = ts;
        _levelingService = ls;
    }

    public async Task<InventoryItem?> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken)
    {
        // Load the recipe
        var recipe = await _recipeService.GetRecipeByIdAsync(recipeId, cancellationToken);
        if (recipe == null) return null;

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

        // Check inventory for required materials
        var removedMaterials = await _inventoryService.TryRemoveItemsAsync(characterId, [.. recipe.Materials], cancellationToken);
        if (!removedMaterials) return null;

        var equipmentInstance = new EquipmentInstance()
        {
            Id = Guid.NewGuid(),
            ItemBaseId = recipe.ItemId,
            ItemBase = recipe.Item,
            Potential = 500 + (10 * professionLevel),

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
        var temperingSession = new TemperingSession();
        while (actionsToPerform > 0 && actionDetails.CraftingQueueItems.Count > 0)
        {
            var current = actionDetails.CraftingQueueItems.First();
            var spend = Math.Min(actionsToPerform, current.EquipmentInstance.Potential ?? 0);

            current.EquipmentInstance.Potential -= spend;
            actionsToPerform -= spend;
            var rng = Random.Shared;
            for (int i = 0; i < spend; i++)
            {
                var newResult = _temperingService.HandleTempering(current, rng);
                AllocateExpBasedOnCraftingProfession(temperingSession, newResult, current.CraftType);
            }

            if (current.EquipmentInstance.Potential == 0)
            {
                actionDetails.CraftingQueueItems.Remove(current); // next item slides up
                await _inventoryService.AddItemInstanceBackToInventory(characterAction.CharacterId, current.EquipmentInstance, cancellationToken);
            }
        }
        // If all items in the queue are processed, mark the action as deleted (finished / completed)
        if (actionDetails.CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
            characterAction.ActionDetails = null;
        }
        await UpdateCharacterProfessionsAsync(characterAction.CharacterId, temperingSession, cancellationToken).ConfigureAwait(false);
    }

    private static void AllocateExpBasedOnCraftingProfession(TemperingSession temperingSession, TemperingResult newResult, CraftType craftType)
    {
        switch (craftType)
        {   
            case CraftType.ArmorForging:
                temperingSession.ArmorForgingExperience += newResult.ExperienceGained;
                break;
            case CraftType.JewelryCrafting:
                temperingSession.JewelryCraftingExperience += newResult.ExperienceGained;
                break;
            case CraftType.WeaponSmithing:
                temperingSession.WeaponSmithingExperience += newResult.ExperienceGained;
                break;
            default:
                break;
        }
    }

    private async Task UpdateCharacterProfessionsAsync(Guid characterId, TemperingSession temperingSession, CancellationToken cancellationToken)
    {
        if (temperingSession.TotalExperience == 0) return;
        var professions = await _professionService.GetProfessionsAsync(characterId, cancellationToken);
        foreach (var profession in professions)
        {
            switch (profession.ProfessionType)
            {
                case ProfessionType.ArmorForging:
                    profession.Experience += temperingSession.ArmorForgingExperience;
                    break;
                case ProfessionType.JewelryCrafting:
                    profession.Experience += temperingSession.JewelryCraftingExperience;
                    break;
                case ProfessionType.WeaponSmithing:
                    profession.Experience += temperingSession.WeaponSmithingExperience;
                    break;
                default:
                    continue; // Skip if the profession type is not recognized
            }
            await _levelingService.UpdateProfessionLevel(profession);
        }

        await _professionService.UpdateProfessionLevelAsync(professions, cancellationToken);
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