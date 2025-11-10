using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CharacterActions;
public class CharacterActionRepository : ICharacterActionRepository
{
    private readonly IDbContext _context;

    public CharacterActionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<bool> StartCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)  // Ensure ActionDetails is loaded
            .FirstOrDefaultAsync(a => a.CharacterId == characterAction.CharacterId, cancellationToken);

        if (existingAction == null)
        {
            characterAction.IsDeleted = false; // Ensure it's not marked as deleted on creation
            await _context.CharacterActions.AddAsync(characterAction, cancellationToken);
            return true;
        }

        // If combat or any other action ends in the future, it is not possible to start a new action until that time has passed
        if (existingAction.UpdatedAt > DateTimeOffset.UtcNow)
        {
            return false;
        }

        existingAction.UpdatedAt = characterAction.UpdatedAt;
        existingAction.IsDeleted = false;

        if (existingAction.ActionDetails == null || existingAction.ActionDetails is CraftingActionDetails)
        {
            // If existing action had no details, add new details
            existingAction.ActionDetails = characterAction.ActionDetails!;
            _context.ActionDetails.Add(existingAction.ActionDetails);
        }
        else
        {
            // If existing action has details, update them
            _context.GetEntry(existingAction.ActionDetails).CurrentValues.SetValues(characterAction.ActionDetails!);
        }

        _context.CharacterActions.Update(existingAction);
        return true;
    }

    public async Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, CancellationToken cancellationToken)
    {
        if (characterAction.ActionDetails != null)
            _context.ActionDetails.Remove(characterAction.ActionDetails);  // Explicitly remove the related entity

        characterAction.IsDeleted = true;
        characterAction.ActionDetails = null;
        _context.CharacterActions.Update(characterAction);
        return true;
    }

    public async Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CombatActionDetails).Area)
                    .ThenInclude(a => a.Creatures)
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
                        .ThenInclude(ei => ei.ItemBase)
                            .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
        return characterAction;
    }

    public void UpdateCharacterAction(CharacterAction characterAction)
    {
        _context.CharacterActions.Update(characterAction);
    }

    public async Task<CharacterAction?> GetCraftingActionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var craftingAction = await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);

        return craftingAction;
    }

    // This differs from the StartCharacterActionAsync method in that it doesn't update UpdatedAt
    public async Task<bool> UpdateCraftingActionAsync(Guid characterId, CraftingQueueItem craftingQueueItem, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)  // Ensure ActionDetails is loaded
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
            .FirstOrDefaultAsync(a => a.CharacterId == characterId, cancellationToken);

        var inventoryItem = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
                .ThenInclude(inventoryItem => inventoryItem.ItemBase)
            .FirstOrDefaultAsync(ii => ii.ItemInstanceId == craftingQueueItem.EquipmentInstanceId && ii.InventoryId == characterId, cancellationToken);

        if (inventoryItem == null)
            return false; // Item doesn't belong to the character or doesn't exist
        craftingQueueItem.CraftType = (inventoryItem.ItemInstance.ItemBase as EquipmentBase)!.EquipmentType switch
        {
            EquipmentType.TwoHanded or EquipmentType.OneHanded or EquipmentType.OffHand => CraftType.WeaponSmithing,
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs => CraftType.ArmorForging,
            EquipmentType.Necklace or EquipmentType.Relic or EquipmentType.Ring => CraftType.JewelryCrafting,
            _ => throw new NotImplementedException($"Craft type for {inventoryItem.ItemInstance.ItemBase.Id} is not implemented")
        };
        // Remove it from inventory
        _context.InventoryItems.Remove(inventoryItem);

        if (existingAction == null)
        {

            var action = new CharacterAction
            {
                CharacterId = characterId,
                UpdatedAt = now,
                IsDeleted = false, // Ensure it's not marked as deleted on creation
                ActionDetails = new CraftingActionDetails
                {
                    CraftingQueueItems = [craftingQueueItem]
                },
            };
            await _context.CharacterActions.AddAsync(action, cancellationToken);
            return true;
        }

        // If combat or any other action ends in the future, it is not possible to start a new action until that time has passed
        if (existingAction.UpdatedAt > now)
            return false;

        existingAction.IsDeleted = false;

        if (existingAction.ActionDetails is not CraftingActionDetails craftingDetails)
        {
            // If existing action had no details, add new details
            existingAction.UpdatedAt = now;
            existingAction.ActionDetails = new CraftingActionDetails
            {
                CraftingQueueItems = [craftingQueueItem]
            };
            _context.ActionDetails.Add(existingAction.ActionDetails);
        }
        else
        {
            if (craftingDetails.CraftingQueueItems.Count == 0) existingAction.UpdatedAt = now;

            craftingQueueItem.CraftingActionDetailsId = craftingDetails.Id;
            _context.CraftingQueueItems.Add(craftingQueueItem);
        }

        return true;
    }

    public async Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
    }
}