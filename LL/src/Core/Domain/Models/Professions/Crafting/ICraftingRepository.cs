using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting;
public interface ICraftingRepository
{
    Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetUnlockedRecipeIdsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetUnlockedBlueprintIdsByRecipeIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasRecipeUnlockAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
    Task<bool> HasBlueprintUnlockAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken);
    void AddRecipeUnlock(CharacterRecipeUnlock unlock);
    Task<CharacterRecipeMastery?> GetRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
    void AddRecipeMastery(CharacterRecipeMastery mastery);
}
