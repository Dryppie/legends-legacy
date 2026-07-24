using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting;
public interface ICraftingRepository
{
    Task<EquipmentInstance?> RemoveCraftingQueueItemAndReturnItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeUnlock>> GetBlueprintUnlocksAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasBlueprintUnlockAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken);
    void AddRecipeUnlock(CharacterRecipeUnlock unlock);
    Task<CharacterRecipeMastery?> GetRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
    void AddRecipeMastery(CharacterRecipeMastery mastery);
}
