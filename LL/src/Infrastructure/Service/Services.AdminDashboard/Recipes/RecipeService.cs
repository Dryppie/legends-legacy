using Application.Interfaces.Services.AdminDashboard;
using Domain.Models.Professions.Crafting;
using Services.AdminDashboard.JsonReaders;

namespace Services.AdminDashboard.Recipes;
public class RecipeService : IRecipeService
{
    private readonly RecipeJsonReader _reader;

    public RecipeService()
    {
        _reader = new RecipeJsonReader();
    }

    public async Task<List<Recipe>> GetRecipesAsync(CancellationToken cancellationToken)
    {
        return _reader.GetRecipes();
    }

    public async Task UpdateRecipeAsync(Recipe recipeToUpdate, CancellationToken cancellationToken)
    {
        _reader.UpdateRecipe(recipeToUpdate);
    }
}