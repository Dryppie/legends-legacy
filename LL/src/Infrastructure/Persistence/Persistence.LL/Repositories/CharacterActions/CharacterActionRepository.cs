using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Inventories;
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
            .Include(a => a.ActionDetails)
                .ThenInclude(details => (details as CraftingActionDetails).CraftingQueueItems)
            .FirstOrDefaultAsync(a => a.CharacterId == characterAction.CharacterId, cancellationToken);

        if (existingAction == null)
        {
            characterAction.IsDeleted = false; // Ensure it's not marked as deleted on creation
            characterAction.BlockedUntilUtc = GetSwitchLock(characterAction.ActionDetails, now);
            RememberCombatArea(characterAction, characterAction.ActionDetails);
            await _context.CharacterActions.AddAsync(characterAction, cancellationToken);
            return characterAction;
        }

        if (!existingAction.IsDeleted &&
            existingAction.ActionDetails is CombatActionDetails existingCombatDetails &&
            characterAction.ActionDetails is CombatActionDetails requestedCombatDetails)
        {
            existingCombatDetails.AreaId = requestedCombatDetails.AreaId;
            existingCombatDetails.Area = requestedCombatDetails.Area;
            existingAction.UpdatedAt = now;
            existingAction.RowVersion++;
            existingAction.ReturnToCombatAreaId = requestedCombatDetails.AreaId;
            _context.CharacterActions.Update(existingAction);
            return existingAction;
        }

        if (existingAction.BlockedUntilUtc > now)
        {
            return null;
        }

        existingAction.NextResolutionAtUtc = characterAction.NextResolutionAtUtc;
        existingAction.UpdatedAt = now;
        existingAction.BlockedUntilUtc = GetSwitchLock(characterAction.ActionDetails, now);
        existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
        existingAction.IsDeleted = false;
        existingAction.RowVersion++;
        RememberCombatArea(existingAction, characterAction.ActionDetails);

        if (characterAction.ActionDetails is CombatActionDetails &&
            existingAction.ActionDetails is CraftingActionDetails craftingDetails)
        {
            foreach (var queueItem in craftingDetails.CraftingQueueItems)
            {
                queueItem.CraftingActionDetailsId = null;
                queueItem.PausedForCharacterId = characterAction.CharacterId;
            }

            existingAction.PausedTemperingQueueItems =
                [.. craftingDetails.CraftingQueueItems.OrderBy(item => item.Position)];
        }

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
        var stoppedCombat = characterAction.ActionDetails as CombatActionDetails;
        if (stoppedCombat != null)
        {
            characterAction.ReturnToCombatAreaId = stoppedCombat.AreaId;
        }

        if (characterAction.ActionDetails is CraftingActionDetails craftingDetails)
        {
            foreach (var queueItem in craftingDetails.CraftingQueueItems)
            {
                queueItem.CraftingActionDetailsId = null;
                queueItem.PausedForCharacterId = characterAction.CharacterId;
            }

            characterAction.PausedTemperingQueueItems =
                [.. craftingDetails.CraftingQueueItems.OrderBy(item => item.Position)];
        }

        if (characterAction.ActionDetails != null)
            _context.ActionDetails.Remove(characterAction.ActionDetails);  // Explicitly remove the related entity

        characterAction.IsDeleted = true;
        characterAction.ActionDetails = null;
        characterAction.BlockedUntilUtc = stoppedCombat != null && characterAction.BlockedUntilUtc > now
            ? characterAction.BlockedUntilUtc
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
    // combat may be replaced or followed by queued tempering immediately, while the
    // tempering cadence still begins at the fixed switch lock. It does not wait for
    // a rolling combat resolution boundary.
    public async Task<bool> UpdateCraftingActionAsync(
        Guid characterId,
        CraftingQueueItem craftingQueueItem,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await _context.InventoryItems
            .Include(item => item.ItemInstance)
                .ThenInclude(itemInstance => itemInstance.ItemBase)
            .FirstOrDefaultAsync(
                item => item.InventoryId == characterId &&
                    item.ItemInstanceId == craftingQueueItem.EquipmentInstanceId,
                cancellationToken);
        return inventoryItem is not null &&
            await UpdateCraftingActionAsync(
                characterId,
                craftingQueueItem,
                inventoryItem,
                now,
                cancellationToken) is not null;
    }

    public async Task<CharacterAction?> UpdateCraftingActionAsync(
        Guid characterId,
        CraftingQueueItem craftingQueueItem,
        InventoryItem inventoryItem,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingAction = await _context.CharacterActions
            .Include(a => a.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
            .FirstOrDefaultAsync(a => a.CharacterId == characterId, cancellationToken);

        var pausedQueue = existingAction?.ActionDetails is CraftingActionDetails
            ? []
            : await GetPausedTemperingQueueForMutationAsync(characterId, cancellationToken);

        var pendingSwitchLock = existingAction?.BlockedUntilUtc is { } blockedUntil && blockedUntil > now
            ? blockedUntil
            : (DateTimeOffset?)null;

        if (inventoryItem.InventoryId != characterId ||
            inventoryItem.ItemInstanceId != craftingQueueItem.EquipmentInstanceId ||
            inventoryItem.ItemInstance is not EquipmentInstance equipmentInstance)
            return null;

        if (equipmentInstance.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return null;

        craftingQueueItem.EquipmentInstance = equipmentInstance;

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
                ActionDetails = new CraftingActionDetails { Id = Guid.NewGuid() },
            };
            AttachQueue(action.ActionDetails as CraftingActionDetails, pausedQueue, craftingQueueItem);
            await _context.CharacterActions.AddAsync(action, cancellationToken);
            return action;
        }

        existingAction.IsDeleted = false;
        existingAction.RowVersion++;

        if (existingAction.ActionDetails is not CraftingActionDetails craftingDetails)
        {
            var temperingStartsAt = pendingSwitchLock ?? now;

            if (existingAction.ActionDetails is CombatActionDetails interruptedCombat)
            {
                existingAction.ReturnToCombatAreaId = interruptedCombat.AreaId;
            }

            if (existingAction.ActionDetails != null)
            {
                _context.ActionDetails.Remove(existingAction.ActionDetails);
            }

            // Make the queued replacement visible immediately, but do not grant its
            // first tempering result until one full tempering interval after combat
            // becomes eligible to be replaced.
            existingAction.UpdatedAt = now;
            existingAction.NextResolutionAtUtc = temperingStartsAt.AddSeconds(TemperingConstants.ActionDurationSeconds);
            existingAction.BlockedUntilUtc = pendingSwitchLock;
            existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
            var resumedDetails = new CraftingActionDetails
            {
                Id = Guid.NewGuid(),
                CharacterActionId = characterId
            };
            _context.ActionDetails.Add(resumedDetails);
            AttachQueue(resumedDetails, pausedQueue, craftingQueueItem);
            existingAction.ActionDetails = resumedDetails;
        }
        else
        {
            if (craftingDetails.CraftingQueueItems.Count == 0)
            {
                existingAction.NextResolutionAtUtc = now.AddSeconds(TemperingConstants.ActionDurationSeconds);
                existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
            }
            existingAction.UpdatedAt = now;
            existingAction.BlockedUntilUtc = existingAction.BlockedUntilUtc > now
                ? existingAction.BlockedUntilUtc
                : null;

            craftingQueueItem.Position = craftingDetails.CraftingQueueItems.Count == 0
                ? 0
                : craftingDetails.CraftingQueueItems.Max(item => item.Position) + 1;
            craftingQueueItem.CraftingActionDetailsId = craftingDetails.Id;
            _context.CraftingQueueItems.Add(craftingQueueItem);
        }

        return existingAction;
    }

    private async Task<List<CraftingQueueItem>> GetPausedTemperingQueueForMutationAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await _context.CraftingQueueItems
            .Where(item => item.PausedForCharacterId == characterId)
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<CharacterAction?> ResumeTemperingAsync(
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingAction = await _context.CharacterActions
            .Include(action => action.ActionDetails)
            .FirstOrDefaultAsync(action => action.CharacterId == characterId, cancellationToken);
        if (existingAction?.ActionDetails is CraftingActionDetails)
            return await GetCraftingActionForResolutionAsync(characterId, cancellationToken);

        var pausedQueue = (await GetPausedTemperingQueueAsync(characterId, cancellationToken)).ToList();
        if (pausedQueue.Count == 0)
            return null;

        var pendingSwitchLock = existingAction?.BlockedUntilUtc is { } blockedUntil && blockedUntil > now
            ? blockedUntil
            : (DateTimeOffset?)null;
        var temperingStartsAt = pendingSwitchLock ?? now;

        if (existingAction?.ActionDetails is CombatActionDetails interruptedCombat)
        {
            existingAction.ReturnToCombatAreaId = interruptedCombat.AreaId;
        }

        if (existingAction == null)
        {
            existingAction = new CharacterAction
            {
                CharacterId = characterId,
                ScheduleGeneration = 1
            };
            await _context.CharacterActions.AddAsync(existingAction, cancellationToken);
        }
        else
        {
            if (existingAction.ActionDetails != null)
                _context.ActionDetails.Remove(existingAction.ActionDetails);
            existingAction.ScheduleGeneration = checked(existingAction.ScheduleGeneration + 1);
            existingAction.RowVersion++;
        }

        var craftingDetails = new CraftingActionDetails
        {
            Id = Guid.NewGuid(),
            CharacterActionId = characterId
        };
        _context.ActionDetails.Add(craftingDetails);
        AttachQueue(craftingDetails, pausedQueue);

        existingAction.ActionDetails = craftingDetails;
        existingAction.IsDeleted = false;
        existingAction.UpdatedAt = now;
        existingAction.BlockedUntilUtc = pendingSwitchLock;
        existingAction.NextResolutionAtUtc = temperingStartsAt.AddSeconds(TemperingConstants.ActionDurationSeconds);
        existingAction.PausedTemperingQueueItems = [];
        return existingAction;
    }

    public bool ResumeCombatAfterTempering(
        CharacterAction characterAction,
        CombatActionDetails combatActionDetails,
        DateTimeOffset combatStartsAt,
        DateTimeOffset now)
    {
        if (characterAction.ActionDetails is not CraftingActionDetails craftingDetails ||
            craftingDetails.CraftingQueueItems.Count != 0 ||
            string.IsNullOrWhiteSpace(characterAction.ReturnToCombatAreaId) ||
            !string.Equals(
                characterAction.ReturnToCombatAreaId,
                combatActionDetails.AreaId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _context.ActionDetails.Remove(craftingDetails);
        combatActionDetails.CharacterActionId = characterAction.CharacterId;
        _context.ActionDetails.Add(combatActionDetails);

        characterAction.ActionDetails = combatActionDetails;
        characterAction.IsDeleted = false;
        characterAction.NextResolutionAtUtc = combatStartsAt;
        characterAction.BlockedUntilUtc = combatStartsAt.AddSeconds(
            CharacterActionTimingConstants.CombatSwitchLockSeconds);
        characterAction.UpdatedAt = now;
        characterAction.ScheduleGeneration = checked(
            characterAction.ScheduleGeneration + 1);
        characterAction.ReturnToCombatAreaId = combatActionDetails.AreaId;
        characterAction.AutoResumedFromTempering = true;
        return true;
    }

    private static void RememberCombatArea(
        CharacterAction characterAction,
        ActionDetails? actionDetails)
    {
        if (actionDetails is CombatActionDetails combatDetails)
        {
            characterAction.ReturnToCombatAreaId = combatDetails.AreaId;
        }
    }

    public async Task<IReadOnlyList<CraftingQueueItem>> GetPausedTemperingQueueAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await _context.CraftingQueueItems
            .Where(item => item.PausedForCharacterId == characterId)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.InstanceModifiers)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.ToolAffixes)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.ItemBase)
                    .ThenInclude(itemBase => (itemBase as EquipmentBase).AttributeModifiers)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.ItemBase)
                    .ThenInclude(itemBase => (itemBase as EquipmentBase).ToolBonuses)
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    private void AttachQueue(
        CraftingActionDetails? craftingDetails,
        IReadOnlyList<CraftingQueueItem> pausedQueue,
        CraftingQueueItem? appendedItem = null)
    {
        if (craftingDetails == null)
            throw new InvalidOperationException("Crafting details are required to attach a tempering queue.");

        var queue = appendedItem == null
            ? pausedQueue.ToList()
            : [.. pausedQueue, appendedItem];

        for (var position = 0; position < queue.Count; position++)
        {
            var queueItem = queue[position];
            queueItem.Position = position;
            queueItem.CraftingActionDetailsId = craftingDetails.Id;
            queueItem.PausedForCharacterId = null;
            craftingDetails.CraftingQueueItems.Add(queueItem);
        }
    }

    private static DateTimeOffset? GetSwitchLock(
        ActionDetails? actionDetails,
        DateTimeOffset now) =>
        actionDetails is CombatActionDetails
            ? now.AddSeconds(CharacterActionTimingConstants.CombatSwitchLockSeconds)
            : null;

    public async Task<CharacterAction?> GetCharacterActionForDeletionAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
            .FirstOrDefaultAsync(ca => ca.CharacterId.Equals(characterId), cancellationToken);
    }
}
