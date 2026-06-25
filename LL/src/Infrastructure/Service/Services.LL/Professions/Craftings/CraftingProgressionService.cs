using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Professions.Crafting;

namespace Services.LL.Professions.Craftings;

public sealed class CraftingProgressionService : ICraftingProgressionService
{
    private readonly ICraftingRepository _craftingRepository;

    public CraftingProgressionService(ICraftingRepository craftingRepository)
    {
        _craftingRepository = craftingRepository;
    }

    public async Task<IReadOnlySet<string>> GetUnlockedRecipeIdsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _craftingRepository.GetUnlockedRecipeIdsAsync(characterId, cancellationToken);

    public async Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> GetUnlockedBlueprintIdsByRecipeIdAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _craftingRepository.GetUnlockedBlueprintIdsByRecipeIdAsync(characterId, cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var masteries = await _craftingRepository.GetRecipeMasteriesAsync(characterId, cancellationToken);
        return masteries.ToDictionary(
            x => x.RecipeId,
            x => CraftingMasteryProgression.GetLevelForExperience(x.Experience),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var masteries = await _craftingRepository.GetRecipeMasteriesAsync(characterId, cancellationToken);
        foreach (var mastery in masteries)
        {
            mastery.Level = CraftingMasteryProgression.GetLevelForExperience(mastery.Experience);
        }

        return masteries;
    }

    public async Task<bool> HasRecipeUnlockAsync(Guid characterId, string recipeId, CancellationToken cancellationToken) =>
        await _craftingRepository.HasRecipeUnlockAsync(characterId, recipeId, cancellationToken);

    public async Task<bool> HasBlueprintUnlockAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken) =>
        await _craftingRepository.HasBlueprintUnlockAsync(characterId, recipeId, blueprintId, cancellationToken);

    public async Task<bool> TryUnlockRecipeAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken)
    {
        var alreadyUnlocked = await HasRecipeUnlockAsync(characterId, recipeId, cancellationToken);
        if (alreadyUnlocked) return false;

        _craftingRepository.AddRecipeUnlock(new CharacterRecipeUnlock
        {
            CharacterId = characterId,
            RecipeId = recipeId,
            BlueprintId = blueprintId
        });

        return true;
    }

    public async Task<bool> TryUnlockBlueprintForRecipeAsync(Guid characterId, string recipeId, string blueprintId, CancellationToken cancellationToken)
    {
        var alreadyUnlocked = await HasBlueprintUnlockAsync(characterId, recipeId, blueprintId, cancellationToken);
        if (alreadyUnlocked) return false;

        _craftingRepository.AddRecipeUnlock(new CharacterRecipeUnlock
        {
            CharacterId = characterId,
            RecipeId = recipeId,
            BlueprintId = blueprintId
        });

        return true;
    }

    public async Task<CharacterRecipeMastery> GetOrCreateRecipeMasteryAsync(Guid characterId, string recipeId, CancellationToken cancellationToken)
    {
        var mastery = await _craftingRepository.GetRecipeMasteryAsync(characterId, recipeId, cancellationToken);
        if (mastery != null) return mastery;

        mastery = new CharacterRecipeMastery
        {
            CharacterId = characterId,
            RecipeId = recipeId
        };
        _craftingRepository.AddRecipeMastery(mastery);
        return mastery;
    }
}
