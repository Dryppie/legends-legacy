using Application.Common.Mappings;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.UseCases.Professions.Commands.CancelTemperingQueue;
using AutoMapper;
using Domain.Models.CharacterActions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.MarketPlaces;
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
            null!,
            new StubInventoryService(new Inventory { CharacterId = characterId }),
            new StubCharacterActionService(action),
            _mapper);

        var response = await handler.Handle(
            new CancelTemperingQueueCommand(characterId),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data.InventoryItems);
        Assert.Empty(response.Data.CurrentAction!.TemperingQueueItems);
    }

    private sealed class StubCharacterActionService(CharacterAction action)
        : ICharacterActionService
    {
        public Task<CharacterAction?> PeekCharacterActionAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<CharacterAction?>(action);

        public Task<CharacterAction?> StartCharacterActionAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CharacterAction?> GetCharacterActionAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteCharacterActionAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateCraftingCharacterActionAsync(Guid characterId, CraftingQueueItem characterAction, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CharacterAction?> ResumeTemperingAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubInventoryService(Inventory inventory) : IInventoryService
    {
        public Task<Inventory?> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult<Inventory?>(inventory);

        public Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, string acquisitionSource, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, string acquisitionSource, Guid correlationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, List<Material> materials, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryConsumeInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> SetItemFavoriteAsync(Guid characterId, Guid itemInstanceId, bool isFavorite, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing marketplaceListing, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem inventoryItem, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InventoryTransferResult> TransferItemAsync(Guid senderCharacterId, Guid recipientCharacterId, Guid itemInstanceId, int quantity, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
