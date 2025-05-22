namespace Domain.Models.Professions.Crafting;
public interface ICraftingRepository
{
    Task<bool> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken);
}