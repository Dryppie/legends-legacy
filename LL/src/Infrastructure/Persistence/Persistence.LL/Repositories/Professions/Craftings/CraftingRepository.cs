using Application.Common.Interfaces;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions.Craftings;
public class CraftingRepository : ICraftingRepository
{
    private readonly IDbContext _dbContext;
    public CraftingRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken)
    {
        var characterAction = await _dbContext.CharacterActions
            .Include(ca => ca.ActionDetails)
                .ThenInclude(ad => (ad as CraftingActionDetails).CraftingQueueItems)
                    .ThenInclude(cq => cq.EquipmentInstance)
            .FirstOrDefaultAsync(ca => ca.CharacterId == characterId && ca.ActionDetails is CraftingActionDetails, cancellationToken);
        if (characterAction == null || characterAction.ActionDetails == null) return null;

        var queueItem = (characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems
            .FirstOrDefault(cq => cq.Id == queueItemId);
        if (queueItem == null) return null;

        (characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems.Remove(queueItem);
        if ((characterAction.ActionDetails as CraftingActionDetails).CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
            characterAction.ActionDetails = null;
        }
        return queueItem?.EquipmentInstance;
    }

    public async Task<IReadOnlySet<string>> GetUnlockedRecipeIdsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var unlocks = await _dbContext.CharacterRecipeUnlocks
            .Where(x => x.CharacterId == characterId)
            .Select(x => x.RecipeId)
            .ToListAsync(cancellationToken);

        return unlocks.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetUnlockedBlueprintIdsByRecipeIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var unlocks = await _dbContext.CharacterRecipeUnlocks
            .Where(x => x.CharacterId == characterId)
            .Select(x => new { x.RecipeId, x.BlueprintId })
            .ToListAsync(cancellationToken);

        return unlocks
            .GroupBy(x => x.RecipeId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlySet<string>)group
                    .Select(x => x.BlueprintId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeMasteries
            .Where(x => x.CharacterId == characterId)
            .ToDictionaryAsync(x => x.RecipeId, x => x.Level, StringComparer.OrdinalIgnoreCase, cancellationToken);

    public async Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeMasteries
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.RecipeId)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasRecipeUnlockAsync(Guid characterId, string recipeId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeUnlocks
            .AnyAsync(x => x.CharacterId == characterId && x.RecipeId == recipeId, cancellationToken);

    public async Task<bool> HasBlueprintUnlockAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeUnlocks
            .AnyAsync(x =>
                x.CharacterId == characterId &&
                x.RecipeId == recipeId &&
                x.BlueprintId == blueprintId,
                cancellationToken);

    public void AddRecipeUnlock(CharacterRecipeUnlock unlock) =>
        _dbContext.CharacterRecipeUnlocks.Add(unlock);

    public async Task<CharacterRecipeMastery?> GetRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken) =>
        await _dbContext.CharacterRecipeMasteries
            .FirstOrDefaultAsync(x => x.CharacterId == characterId && x.RecipeId == recipeId, cancellationToken);

    public void AddRecipeMastery(CharacterRecipeMastery mastery) =>
        _dbContext.CharacterRecipeMasteries.Add(mastery);
}
