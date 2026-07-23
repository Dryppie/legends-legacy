using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingProgressionService
{
    Task<IReadOnlySet<string>> GetUnlockedBlueprintIdsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> HasBlueprintUnlockAsync(Guid characterId, string blueprintId, CancellationToken cancellationToken);
    Task<bool> TryUnlockBlueprintAsync(Guid characterId, string blueprintId, CancellationToken cancellationToken);
    Task<CharacterRecipeMastery> GetOrCreateRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken);
}
