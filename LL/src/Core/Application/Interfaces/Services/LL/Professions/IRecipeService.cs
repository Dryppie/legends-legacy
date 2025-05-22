using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.LL.Professions;
public interface IRecipeService
{
    Task<Recipe?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken);
}