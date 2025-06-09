using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Soulstones.Events;
using Domain.Helpers.Constants;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.Professions.Craftings;
public class CraftingService : ICraftingService
{
    private readonly ICraftingRepository _craftingRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IProfessionService _professionService;
    private readonly IRecipeService _recipeService;
    private readonly ITemperingService _temperingService;
    private readonly ILevelingService _levelingService;
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;
    private readonly ILootService _lootService;
    private readonly IPublisher _publisher;

    public CraftingService(ICraftingRepository cr, IInventoryService invS, IProfessionService ps, IRecipeService rs, ITemperingService ts, ILevelingService lvlS, ISoulstoneUpgradeService sus, ILootService ls, IPublisher p)
    {
        _craftingRepository = cr;
        _inventoryService = invS;
        _professionService = ps;
        _recipeService = rs;
        _temperingService = ts;
        _levelingService = lvlS;
        _soulstoneUpgradeService = sus;
        _lootService = ls;
        _publisher = p;
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
        // Is the profession level sufficient for this recipe?
        if (professionLevel < recipe.LevelRequirement) return null;

        // Check inventory for required materials
        var removedMaterials = await _inventoryService.TryRemoveCraftingMaterialsAsync(characterId, [.. recipe.Materials], cancellationToken);
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

    public async Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        string[] wantedBonuses = [SoulstoneUpgradeContants.SoulstoneDropRate,
                                  SoulstoneUpgradeContants.SoulstoneDoubleDropChance,
                                  SoulstoneUpgradeContants.CraftingDoubleItemExpChance,
                                  SoulstoneUpgradeContants.CraftingNegativeOutcome];

        var soulstoneBonuses = await _soulstoneUpgradeService.GetSoulstoneBonusesByCharacterIdAsync(characterAction.CharacterId, wantedBonuses, cancellationToken);

        soulstoneBonuses.TryGetValue(SoulstoneUpgradeContants.SoulstoneDropRate, out var soulstoneDropRate);
        soulstoneBonuses.TryGetValue(SoulstoneUpgradeContants.SoulstoneDoubleDropChance, out var soulstoneDoubleDropChance);
        soulstoneBonuses.TryGetValue(SoulstoneUpgradeContants.CraftingDoubleItemExpChance, out var craftingDoubleItemExpChance);
        soulstoneBonuses.TryGetValue(SoulstoneUpgradeContants.CraftingNegativeOutcome, out var craftingNegativeOutcome);
        
        var temperingBonuses = new Dictionary<TemperingOutcome, double>()
        {
            { TemperingOutcome.Negative, craftingNegativeOutcome },
            { TemperingOutcome.Positive, craftingDoubleItemExpChance }
        };

        var actionDetails = (characterAction.ActionDetails as CraftingActionDetails)!;
        var produced = new List<InventoryItem>();
        var sessionStartedAt = characterAction.UpdatedAt;
        var now = DateTimeOffset.UtcNow;

        var temperingSummary = new TemperingSummary();
        var rng = Random.Shared;

        while (actionsToPerform > 0 && actionDetails.CraftingQueueItems.Count > 0)
        {
            var current = actionDetails.CraftingQueueItems.First();
            var spend = Math.Min(actionsToPerform, current.EquipmentInstance.Potential ?? 0);
            characterAction.UpdatedAt += TimeSpan.FromSeconds(6 * spend);

            current.EquipmentInstance.Potential -= spend;
            actionsToPerform -= spend;
            temperingSummary.TotalActions += spend;

            for (int i = 0; i < spend; i++)
            {
                _temperingService.HandleTempering(current, temperingSummary, rng, temperingBonuses);
            }

            if (current.EquipmentInstance.Potential == 0)
            {
                temperingSummary.TotalItemsCrafted++;
                actionDetails.CraftingQueueItems.Remove(current); // next item slides up
                await _inventoryService.AddItemInstanceBackToInventory(characterAction.CharacterId, current.EquipmentInstance, cancellationToken);
            }
        }
        // If all items in the queue are processed, mark the action as deleted (finished / completed)
        if (actionDetails.CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
            //characterAction.ActionDetails = null;
        }

        await ProcessSoulstoneDrops(characterAction.CharacterId, temperingSummary.TotalActions, soulstoneDropRate, soulstoneDoubleDropChance, cancellationToken);
        await UpdateCharacterProfessionsAsync(characterAction.CharacterId, temperingSummary, cancellationToken);

        // TODO: Publish event to handle earning soulstones
        // TODO: Perhaps publish event with nothing but a durationInSeconds, and a CharacterGuid. The event can then handle checking whether SS drops
        var temperingSession = new TemperingSession()
        {
            From = sessionStartedAt,
            To = now,
            TemperingSummary = temperingSummary
        };

        return temperingSession;
    }

    private async Task ProcessSoulstoneDrops(Guid characterId, int actionsPerformed, double dropRate, double doubleDropChance, CancellationToken cancellationToken)
    {
        var durationInSeconds = 6 * actionsPerformed;
        var soulstonesEarned = _lootService.GenerateSoulstoneLoot(durationInSeconds, dropRate, doubleDropChance);
        if (soulstonesEarned < 1) return;

        await _publisher.Publish(new SoulstoneDropEvent(characterId, soulstonesEarned), cancellationToken);
    }

    private async Task UpdateCharacterProfessionsAsync(Guid characterId, TemperingSummary temperingSummary, CancellationToken cancellationToken)
    {
        if (temperingSummary.TotalExperience == 0) return;
        var professions = await _professionService.GetProfessionsAsync(characterId, cancellationToken);
        var professionsToUpdate = new List<Profession>();
        foreach (var profession in professions)
        {
            switch (profession.ProfessionType)
            {
                case ProfessionType.ArmorForging:
                    profession.Experience += temperingSummary.ArmorForgingExperience;
                    professionsToUpdate.Add(profession);
                    break;
                case ProfessionType.JewelryCrafting:
                    profession.Experience += temperingSummary.JewelryCraftingExperience;
                    professionsToUpdate.Add(profession);
                    break;
                case ProfessionType.WeaponSmithing:
                    profession.Experience += temperingSummary.WeaponSmithingExperience;
                    professionsToUpdate.Add(profession);
                    break;
                default:
                    continue; // Skip if the profession type is not recognized
            }
            await _levelingService.UpdateProfessionLevel(profession, cancellationToken);
        }


        await _professionService.UpdateProfessionLevelAsync(professionsToUpdate, cancellationToken);
    }

    public async Task<bool> RemoveCraftingQueueItemsAsync(Guid characterId, List<Guid> queueItemIds, CancellationToken cancellationToken)
{
    var anyItemAdded = false;

    foreach (var queueItemId in queueItemIds)
    {
        var equipmentInstance = await _craftingRepository.RemoveCraftingQueueItemAndReturnItemAsync(characterId, queueItemId, cancellationToken);
        if (equipmentInstance == null) continue;

        var itemAdded = await _inventoryService.AddItemInstanceBackToInventory(characterId, equipmentInstance, cancellationToken);
        if (itemAdded) anyItemAdded = true;
    }

    if (anyItemAdded)
        await _craftingRepository.SaveChangesAsync(cancellationToken);

    return anyItemAdded;
}

}