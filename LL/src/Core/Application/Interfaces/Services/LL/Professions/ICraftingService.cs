using Domain.Models.CharacterActions;

namespace Application.Interfaces.Services.LL.Professions;
public interface ICraftingService
{
    Task<bool> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken);
    Task PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken);
}