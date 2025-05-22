namespace Domain.Models.Professions.Crafting;
public interface IRecipeRepository
{
    Task<Recipe?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken);
}