using Domain.Models.CharacterActions;
using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Professions;
public interface ICraftingService
{
    Task<InventoryItem?> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken);
    Task PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken);
}