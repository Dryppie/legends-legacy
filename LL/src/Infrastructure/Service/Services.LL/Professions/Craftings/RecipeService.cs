using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Professions.Crafting;

namespace Services.LL.Professions.Craftings;
public class RecipeService : IRecipeService
{
    private readonly IRecipeRepository _recipeRepository;

    public RecipeService(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;
    }

    public async Task<Recipe?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        return await _recipeRepository.GetRecipeByIdAsync(recipeId, cancellationToken);
    }
}