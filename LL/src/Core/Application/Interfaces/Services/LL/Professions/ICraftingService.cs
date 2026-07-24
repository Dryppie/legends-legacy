using Application.UseCases.Crafting.Dtos;
using Common.Primitives;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingService
{
    Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken);
    Task<bool> RemoveCraftingQueueItemsAsync(Guid characterId, List<Guid> queueItemIds, CancellationToken cancellationToken);
    Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken);
    Task<Response<LearnBlueprintResult>> LearnBlueprintAsync(Guid characterId, Guid blueprintItemInstanceId, string recipeId, CancellationToken cancellationToken);
    Task<Response<CraftItemsResult>> CraftItemsAsync(Guid characterId, string recipeId, string? blueprintId, int targetTier, int quantity, CancellationToken cancellationToken);
}
