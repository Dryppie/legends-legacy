using Domain.Models.Items.Equipments;

using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Domain.Models.Professions.Crafting;
public interface ICraftingRepository
{
    void RemoveCompletedCraftingQueueItem(
        CraftingActionDetails actionDetails,
        CraftingQueueItem queueItem);
    Task<CraftingQueueRemovalResult?> RemoveCraftingQueueItemsAndReturnItemsAsync(
        Guid characterId,
        IReadOnlyCollection<Guid>? queueItemIds,
        CancellationToken cancellationToken);
    Task<bool> MoveCraftingQueueItemAsync(
        Guid characterId,
        Guid queueItemId,
        CraftingQueueMoveDirection direction,
        CancellationToken cancellationToken);
    Task<bool> SetRemoveAfterNextRarityUpgradeAsync(
        Guid characterId,
        Guid queueItemId,
        bool enabled,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeUnlock>> GetBlueprintUnlocksAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasBlueprintUnlockAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken);
    void AddRecipeUnlock(CharacterRecipeUnlock unlock);
    Task<CharacterRecipeMastery?> GetRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
    void AddRecipeMastery(CharacterRecipeMastery mastery);
}
