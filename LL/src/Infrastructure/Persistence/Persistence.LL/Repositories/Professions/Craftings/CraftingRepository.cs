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

    public async Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken)
    {
        var characterAction = await _dbContext.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(cq => cq.EquipmentInstance)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId, cancellationToken);
        if (characterAction?.ActionDetails is not CraftingActionDetails craftingDetails) return null;

        var queueItem = craftingDetails.CraftingQueueItems
            .FirstOrDefault(cq => cq.Id == queueItemId);
        if (queueItem == null) return null;

        craftingDetails.CraftingQueueItems.Remove(queueItem);
        if (craftingDetails.CraftingQueueItems.Count == 0)
        {
            var now = _timeProvider.GetUtcNow();
            _dbContext.ActionDetails.Remove(craftingDetails);
            characterAction.IsDeleted = true;
            characterAction.ActionDetails = null;
            characterAction.NextResolutionAtUtc = null;
            characterAction.BlockedUntilUtc = characterAction.BlockedUntilUtc > now
                ? characterAction.BlockedUntilUtc
                : null;
            characterAction.UpdatedAt = now;
            characterAction.RowVersion++;
        }
        return queueItem.EquipmentInstance;
    }

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

        if (characterAction?.ActionDetails is not CraftingActionDetails craftingDetails)
        {
            return false;
        }

        var orderedQueue = craftingDetails.CraftingQueueItems
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .ThenBy(item => item.Id)
            .ToList();
        var currentIndex = orderedQueue.FindIndex(item => item.Id == queueItemId);
        var targetIndex = currentIndex + (int)direction;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= orderedQueue.Count)
        {
            return false;
        }

        (orderedQueue[currentIndex], orderedQueue[targetIndex]) =
            (orderedQueue[targetIndex], orderedQueue[currentIndex]);

        for (var index = 0; index < orderedQueue.Count; index++)
        {
            orderedQueue[index].Position = index;
        }

        characterAction.RowVersion++;
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
