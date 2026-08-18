using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.CharacterActions;
public class CharacterActionRepository : ICharacterActionRepository
{
    private readonly IDbContext _context;

    public CharacterActionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)  // Ensure ActionDetails is loaded
            .FirstOrDefaultAsync(a => a.CharacterId == characterAction.CharacterId, cancellationToken);

        if (existingAction == null)
        {
            characterAction.IsDeleted = false; // Ensure it's not marked as deleted on creation
            await _context.CharacterActions.AddAsync(characterAction, cancellationToken);
            return characterAction;
        }

        if (existingAction.BlockedUntilUtc > now)
        {
            return null;
        }

        existingAction.NextResolutionAtUtc = characterAction.NextResolutionAtUtc;
        existingAction.UpdatedAt = now;
        existingAction.BlockedUntilUtc = null;
        existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
        existingAction.IsDeleted = false;
        existingAction.RowVersion++;

        if (existingAction.ActionDetails != null)
        {
            _context.ActionDetails.Remove(existingAction.ActionDetails);
        }

        existingAction.ActionDetails = characterAction.ActionDetails!;
        existingAction.ActionDetails.CharacterActionId = existingAction.CharacterId;
        _context.ActionDetails.Add(existingAction.ActionDetails);
        _context.CharacterActions.Update(existingAction);
        return existingAction;
    }

    public async Task<bool> DeleteCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var wasCombat = characterAction.ActionDetails is CombatActionDetails;
        if (characterAction.ActionDetails != null)
            _context.ActionDetails.Remove(characterAction.ActionDetails);  // Explicitly remove the related entity

        characterAction.IsDeleted = true;
        characterAction.ActionDetails = null;
        characterAction.BlockedUntilUtc = wasCombat && characterAction.NextResolutionAtUtc > now
            ? characterAction.NextResolutionAtUtc
            : null;
        characterAction.NextResolutionAtUtc = null;
        characterAction.UpdatedAt = now;
        characterAction.RowVersion++;
        _context.CharacterActions.Update(characterAction);
        return true;
    }

    public Task<CharacterAction?> GetActionScheduleAsync(Guid characterId, CancellationToken cancellationToken) =>
        _context.CharacterActions
            .Include(ca => ca.ActionDetails)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId, cancellationToken);

    public async Task<CharacterAction?> GetCombatActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var characterAction = await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CombatActionDetails).Area)
                    .ThenInclude(a => a.Creatures)
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CombatActionDetails).Area)
                    .ThenInclude(a => a.GatheringNodes)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
        return characterAction;
    }

    public async Task<CharacterAction?> GetCraftingActionForResolutionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
                        .ThenInclude(ei => ei.InstanceModifiers)
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
                        .ThenInclude(ei => ei.ToolAffixes)
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
                        .ThenInclude(ei => ei.ItemBase)
                            .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
                        .ThenInclude(ei => ei.ItemBase)
                            .ThenInclude(ib => (ib as EquipmentBase).ToolBonuses)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId, cancellationToken);
    }

    public void UpdateCharacterAction(CharacterAction characterAction)
    {
        var entry = _context.GetEntry(characterAction);

        // A newly started combat action is resolved immediately, before the unit of
        // work is saved. Calling Update here would change the new parent from Added
        // to Modified while leaving its ActionDetails Added. Relational providers
        // would then insert the details before the missing parent and violate the FK.
        if (entry.State == EntityState.Added)
            return;

        characterAction.RowVersion++;

        if (entry.State == EntityState.Detached)
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

    // Adding to an active queue preserves its next due boundary; starting/restarting
    // a queue establishes a new generation and first-attempt boundary. Replacing
    // combat waits for its already-earned action boundary before beginning the
    // tempering cadence, matching the cooldown enforced by an explicit combat stop.
    public async Task<bool> UpdateCraftingActionAsync(Guid characterId, CraftingQueueItem craftingQueueItem, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)  // Ensure ActionDetails is loaded
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(ci => ci.EquipmentInstance)
            .FirstOrDefaultAsync(a => a.CharacterId == characterId, cancellationToken);

        if (existingAction?.BlockedUntilUtc > now)
            return false;

        var inventoryItem = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
                .ThenInclude(inventoryItem => inventoryItem.ItemBase)
            .FirstOrDefaultAsync(ii => ii.ItemInstanceId == craftingQueueItem.EquipmentInstanceId && ii.InventoryId == characterId, cancellationToken);

        if (inventoryItem?.ItemInstance is not EquipmentInstance equipmentInstance)
            return false; // Item doesn't belong to the character or doesn't exist

        if (equipmentInstance.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return false;

        craftingQueueItem.CraftType = equipmentInstance.EquipmentBase.EquipmentType switch
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
                NextResolutionAtUtc = now.AddSeconds(TemperingConstants.ActionDurationSeconds),
                ScheduleGeneration = 1,
                IsDeleted = false, // Ensure it's not marked as deleted on creation
                ActionDetails = new CraftingActionDetails
                {
                    CraftingQueueItems = [craftingQueueItem]
                },
            };
            await _context.CharacterActions.AddAsync(action, cancellationToken);
            return true;
        }

        existingAction.IsDeleted = false;
        existingAction.RowVersion++;

        if (existingAction.ActionDetails is not CraftingActionDetails craftingDetails)
        {
            var temperingStartsAt = existingAction.ActionDetails is CombatActionDetails &&
                existingAction.NextResolutionAtUtc is { } combatBoundary &&
                combatBoundary > now
                    ? combatBoundary
                    : now;

            if (existingAction.ActionDetails != null)
            {
                _context.ActionDetails.Remove(existingAction.ActionDetails);
            }

            // Make the queued replacement visible immediately, but do not grant its
            // first tempering result until one full tempering interval after combat
            // becomes eligible to be replaced.
            existingAction.UpdatedAt = now;
            existingAction.NextResolutionAtUtc = temperingStartsAt.AddSeconds(TemperingConstants.ActionDurationSeconds);
            existingAction.BlockedUntilUtc = null;
            existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
            existingAction.ActionDetails = new CraftingActionDetails
            {
                CharacterActionId = characterId,
                CraftingQueueItems = [craftingQueueItem]
            };
            _context.ActionDetails.Add(existingAction.ActionDetails);
        }
        else
        {
            if (craftingDetails.CraftingQueueItems.Count == 0)
            {
                existingAction.NextResolutionAtUtc = now.AddSeconds(TemperingConstants.ActionDurationSeconds);
                existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
            }
            existingAction.UpdatedAt = now;
            existingAction.BlockedUntilUtc = null;

            craftingQueueItem.Position = craftingDetails.CraftingQueueItems.Count == 0
                ? 0
                : craftingDetails.CraftingQueueItems.Max(item => item.Position) + 1;
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
