using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingProgressionService
{
    Task<IReadOnlySet<string>> GetUnlockedRecipeIdsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetUnlockedBlueprintIdsByRecipeIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasRecipeUnlockAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
    Task<bool> HasBlueprintUnlockAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken);
    Task<bool> TryUnlockRecipeAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken);
    Task<bool> TryUnlockBlueprintForRecipeAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken);
    Task<CharacterRecipeMastery> GetOrCreateRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
}
