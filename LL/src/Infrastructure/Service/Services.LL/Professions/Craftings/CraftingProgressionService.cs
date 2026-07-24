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

    public Task<IReadOnlyList<CharacterRecipeUnlock>> GetBlueprintUnlocksAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        _craftingRepository.GetBlueprintUnlocksAsync(characterId, cancellationToken);

    public async Task<IReadOnlyDictionary<string, int>> GetRecipeMasteryLevelsAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var masteries = await _craftingRepository.GetRecipeMasteriesAsync(characterId, cancellationToken);
        return masteries.ToDictionary(
            x => x.RecipeId,
            x => CraftingMasteryProgression.GetLevelForExperience(x.Experience),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<CharacterRecipeMastery>> GetRecipeMasteriesAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var masteries = await _craftingRepository.GetRecipeMasteriesAsync(characterId, cancellationToken);
        foreach (var mastery in masteries)
            mastery.Level = CraftingMasteryProgression.GetLevelForExperience(mastery.Experience);
        return masteries;
    }

    public Task<bool> HasBlueprintUnlockAsync(
        Guid characterId,
        string recipeId,
        string blueprintId,
        CancellationToken cancellationToken) =>
        _craftingRepository.HasBlueprintUnlockAsync(characterId, recipeId, blueprintId, cancellationToken);

    public async Task<bool> TryUnlockBlueprintAsync(
        Guid characterId,
        string recipeId,
        string blueprintId,
        CancellationToken cancellationToken)
    {
        if (await HasBlueprintUnlockAsync(characterId, recipeId, blueprintId, cancellationToken))
            return false;

        _craftingRepository.AddRecipeUnlock(new CharacterRecipeUnlock
        {
            CharacterId = characterId,
            RecipeId = recipeId,
            BlueprintId = blueprintId
        });
        return true;
    }

    public async Task<CharacterRecipeMastery> GetOrCreateRecipeMasteryAsync(
        Guid characterId,
        string recipeId,
        CancellationToken cancellationToken)
    {
        var mastery = await _craftingRepository.GetRecipeMasteryAsync(characterId, recipeId, cancellationToken);
        if (mastery != null)
            return mastery;

        mastery = new CharacterRecipeMastery
        {
            CharacterId = characterId,
            RecipeId = recipeId
        };
        _craftingRepository.AddRecipeMastery(mastery);
        return mastery;
    }
}
