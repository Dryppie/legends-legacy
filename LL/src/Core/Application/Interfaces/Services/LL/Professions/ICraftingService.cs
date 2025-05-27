using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Professions;
public interface ICraftingService
{
    Task<InventoryItem?> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken);
    Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken);
    Task<bool> RemoveCraftingQueueItemAsync(Guid characterId, Guid queueItemId, CancellationToken cancellationToken);
}