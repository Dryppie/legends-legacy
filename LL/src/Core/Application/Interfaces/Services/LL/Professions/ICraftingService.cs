using Application.UseCases.Crafting.Dtos;
using Common.Primitives;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingService
{
    Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, DateTimeOffset now, CancellationToken cancellationToken);
    Task<TemperingQueueRemovalResult?> RemoveCraftingQueueItemsAsync(
        Guid characterId,
        IReadOnlyCollection<Guid> queueItemIds,
        CancellationToken cancellationToken);
    Task<TemperingQueueRemovalResult> CancelTemperingQueueAsync(
        Guid characterId,
        CancellationToken cancellationToken);
    Task<bool> MoveCraftingQueueItemAsync(
        Guid characterId,
        Guid queueItemId,
        CraftingQueueMoveDirection direction,
        CancellationToken cancellationToken);
    Task<bool> SetRemoveAfterNextRarityUpgradeAsync(
        Guid characterId,
        Guid queueItemId,
        bool enabled,
        CancellationToken cancellationToken);
    Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken);
    Task<Response<LearnBlueprintResult>> LearnBlueprintAsync(Guid characterId, Guid blueprintItemInstanceId, string recipeId, CancellationToken cancellationToken);
    Task<Response<CraftItemsResult>> CraftItemsAsync(Guid characterId, string recipeId, string? blueprintId, int targetTier, int quantity, CancellationToken cancellationToken);
}

public sealed record TemperingQueueRemovalResult(
    CharacterAction? Action,
    IReadOnlyList<Domain.Models.Inventories.InventoryItem> ReturnedInventoryItems,
    IReadOnlyList<Guid> RemovedQueueItemIds);
