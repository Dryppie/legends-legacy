using Application.Common.Interfaces;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions.Craftings;
public class CraftingRepository : ICraftingRepository
{
    private readonly IDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CraftingRepository(IDbContext dbContext, TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void RemoveCompletedCraftingQueueItem(
        CraftingActionDetails actionDetails,
        CraftingQueueItem queueItem)
    {
        _dbContext.CraftingQueueItems.Remove(queueItem);
        actionDetails.CraftingQueueItems.Remove(queueItem);
    }

    public async Task<CraftingQueueRemovalResult?> RemoveCraftingQueueItemsAndReturnItemsAsync(
        Guid characterId,
        IReadOnlyCollection<Guid>? queueItemIds,
        CancellationToken cancellationToken)
    {
        var characterAction = await _dbContext.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId, cancellationToken);
        var craftingDetails = characterAction?.ActionDetails as CraftingActionDetails;
        var craftingDetailsId = craftingDetails?.Id;
        var requestedIds = queueItemIds?.Distinct().ToArray();
        var queueQuery = _dbContext.CraftingQueueItems
            .Where(item =>
                (craftingDetailsId.HasValue &&
                    item.CraftingActionDetailsId == craftingDetailsId) ||
                item.PausedForCharacterId == characterId);
        if (requestedIds is not null)
        {
            if (requestedIds.Length == 0)
                return null;
            queueQuery = queueQuery.Where(item => requestedIds.Contains(item.Id));
        }

        var queueItems = await queueQuery
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.ItemBase)
                    .ThenInclude(itemBase => (itemBase as EquipmentBase).AttributeModifiers)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.ItemBase)
                    .ThenInclude(itemBase => (itemBase as EquipmentBase).ToolBonuses)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.InstanceModifiers)
            .Include(item => item.EquipmentInstance)
                .ThenInclude(equipment => equipment.ToolAffixes)
            .AsSingleQuery()
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        if (requestedIds is null && queueItems.Count == 0)
            return new CraftingQueueRemovalResult(characterAction, [], []);
        if (queueItems.Count == 0 ||
            (requestedIds is not null && queueItems.Count != requestedIds.Length))
            return null;

        _dbContext.CraftingQueueItems.RemoveRange(queueItems);
        if (craftingDetails is not null)
        {
            foreach (var queueItem in queueItems)
                craftingDetails.CraftingQueueItems.Remove(queueItem);
        }

        if (craftingDetails?.CraftingQueueItems.Count == 0 && characterAction != null)
        {
            var now = _timeProvider.GetUtcNow();
            _dbContext.ActionDetails.Remove(craftingDetails);
            characterAction.IsDeleted = true;
            characterAction.ActionDetails = null;
            characterAction.NextResolutionAtUtc = null;
            characterAction.ReturnToCombatAreaId = null;
            characterAction.BlockedUntilUtc = characterAction.BlockedUntilUtc > now
                ? characterAction.BlockedUntilUtc
                : null;
            characterAction.UpdatedAt = now;
            characterAction.RowVersion++;
        }
        else if (characterAction != null)
        {
            characterAction.UpdatedAt = _timeProvider.GetUtcNow();
            characterAction.RowVersion++;
        }
        return new CraftingQueueRemovalResult(
            characterAction,
            queueItems.Select(item => item.EquipmentInstance).ToList(),
            queueItems.Select(item => item.Id).ToList());
    }

    public async Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(
        Guid characterId,
        Guid queueItemId,
        CancellationToken cancellationToken) =>
        (await RemoveCraftingQueueItemsAndReturnItemsAsync(
            characterId,
            [queueItemId],
            cancellationToken))?.EquipmentInstances.SingleOrDefault();

    public async Task<bool> MoveCraftingQueueItemAsync(
        Guid characterId,
        Guid queueItemId,
        CraftingQueueMoveDirection direction,
        CancellationToken cancellationToken)
    {
        var characterAction = await _dbContext.CharacterActions
            .Include(action => action.ActionDetails)
                .ThenInclude(details => (details as CraftingActionDetails).CraftingQueueItems)
            .FirstOrDefaultAsync(
                action => action.CharacterId == characterId,
                cancellationToken);

        var queue = characterAction?.ActionDetails is CraftingActionDetails craftingDetails
            ? craftingDetails.CraftingQueueItems
            : await _dbContext.CraftingQueueItems
                .Where(item => item.PausedForCharacterId == characterId)
                .ToListAsync(cancellationToken);
        var orderedQueue = queue
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .ThenBy(item => item.Id)
            .ToList();
        var currentIndex = orderedQueue.FindIndex(item => item.Id == queueItemId);
        if (currentIndex < 0)
        {
            return false;
        }

        var targetIndex = direction switch
        {
            CraftingQueueMoveDirection.Up => currentIndex - 1,
            CraftingQueueMoveDirection.Down => currentIndex + 1,
            CraftingQueueMoveDirection.Top => 0,
            _ => -1
        };

        if (targetIndex < 0 ||
            targetIndex >= orderedQueue.Count ||
            targetIndex == currentIndex)
        {
            return false;
        }

        if (direction == CraftingQueueMoveDirection.Top)
        {
            var queueItem = orderedQueue[currentIndex];
            orderedQueue.RemoveAt(currentIndex);
            orderedQueue.Insert(0, queueItem);
        }
        else
        {
            (orderedQueue[currentIndex], orderedQueue[targetIndex]) =
                (orderedQueue[targetIndex], orderedQueue[currentIndex]);
        }

        for (var index = 0; index < orderedQueue.Count; index++)
        {
            orderedQueue[index].Position = index;
        }

        if (characterAction != null)
        {
            characterAction.UpdatedAt = _timeProvider.GetUtcNow();
            characterAction.RowVersion++;
        }
        return true;
    }

    public async Task<IReadOnlyList<CharacterRecipeUnlock>> GetBlueprintUnlocksAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeUnlocks
            .Where(x => x.CharacterId == characterId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeMasteries
            .Where(x => x.CharacterId == characterId)
            .ToDictionaryAsync(x => x.RecipeId, x => x.Level, StringComparer.OrdinalIgnoreCase, cancellationToken);

    public async Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeMasteries
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.RecipeId)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasBlueprintUnlockAsync(
        Guid characterId,
        string recipeId,
        string blueprintId,
        CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeUnlocks
            .AnyAsync(
                x => x.CharacterId == characterId &&
                     x.BlueprintId == blueprintId &&
                     (x.RecipeId == recipeId || x.RecipeId == null),
                cancellationToken);

    public void AddRecipeUnlock(CharacterRecipeUnlock unlock) =>
        _dbContext.CharacterRecipeUnlocks.Add(unlock);

    public async Task<CharacterRecipeMastery?> GetRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeMasteries
            .FirstOrDefaultAsync(x => x.CharacterId == characterId && x.RecipeId == recipeId, cancellationToken);

    public void AddRecipeMastery(CharacterRecipeMastery mastery) =>
        _dbContext.CharacterRecipeMasteries.Add(mastery);
}
