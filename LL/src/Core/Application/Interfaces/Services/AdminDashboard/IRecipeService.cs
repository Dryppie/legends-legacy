using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.AdminDashboard;
public interface IRecipeService
{
    Task<List<Recipe>> GetRecipesAsync(CancellationToken cancellationToken);
    Task UpdateRecipeAsync(Recipe recipeToUpdate, CancellationToken cancellationToken);
}