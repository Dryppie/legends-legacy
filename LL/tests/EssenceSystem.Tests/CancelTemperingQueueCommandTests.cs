using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Crafting.Dtos;
using Application.UseCases.Professions.Commands.CancelTemperingQueue;
using AutoMapper;
using Common.Primitives;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class CancelTemperingQueueCommandTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<MappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Cancel_is_idempotent_when_the_server_queue_is_already_empty()
    {
        var characterId = Guid.NewGuid();
        var action = new CharacterAction
        {
            CharacterId = characterId,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = true
        };
        var handler = new CancelTemperingQueueCommandHandler(
            new StubCraftingService(action),
            _mapper);

        var response = await handler.Handle(
            new CancelTemperingQueueCommand(characterId),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data.ReturnedInventoryItems);
        Assert.Empty(response.Data.RemovedQueueItemIds);
        Assert.True(response.Data.Action!.IsDeleted);
    }

    private sealed class StubCraftingService(CharacterAction action) : ICraftingService
    {
        public Task<TemperingQueueRemovalResult> CancelTemperingQueueAsync(
            Guid characterId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new TemperingQueueRemovalResult(action, [], []));

        public Task<TemperingQueueRemovalResult?> RemoveCraftingQueueItemsAsync(
            Guid characterId,
            IReadOnlyCollection<Guid> queueItemIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> MoveCraftingQueueItemAsync(Guid characterId, Guid queueItemId, CraftingQueueMoveDirection direction, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Response<LearnBlueprintResult>> LearnBlueprintAsync(Guid characterId, Guid blueprintItemInstanceId, string recipeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Response<CraftItemsResult>> CraftItemsAsync(Guid characterId, string recipeId, string? blueprintId, int targetTier, int quantity, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
