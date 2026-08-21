using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Inventories;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Inventories;
using MediatR;

namespace Application.UseCases.Inventories.Commands.TransferInventoryItem;

public sealed record TransferInventoryItemCommand(
    Guid CharacterId,
    Guid ItemInstanceId,
    string RecipientName,
    int Quantity) : ICommand<Response<TransferInventoryItemResponseDto>>;

public sealed class TransferInventoryItemCommandHandler
    : IRequestHandler<TransferInventoryItemCommand, Response<TransferInventoryItemResponseDto>>
{
    private readonly ICharacterService _characters;
    private readonly IInventoryService _inventory;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IGameEventOutbox _outbox;
    private readonly ILootHistoryService _lootHistory;
    private readonly IMapper _mapper;

    public TransferInventoryItemCommandHandler(
        ICharacterService characters,
        IInventoryService inventory,
        IGameRealtimeBroadcaster gameRealtime,
        IGameEventOutbox outbox,
        ILootHistoryService lootHistory,
        IMapper mapper)
    {
        _characters = characters;
        _inventory = inventory;
        _gameRealtime = gameRealtime;
        _outbox = outbox;
        _lootHistory = lootHistory;
        _mapper = mapper;
    }

    public async Task<Response<TransferInventoryItemResponseDto>> Handle(
        TransferInventoryItemCommand request,
        CancellationToken cancellationToken)
    {
        var recipientName = request.RecipientName.Trim();
        if (recipientName.Length == 0)
            return Response<TransferInventoryItemResponseDto>.Fail("Enter the receiving player's name.");
        if (request.Quantity <= 0)
            return Response<TransferInventoryItemResponseDto>.Fail("Transfer quantity must be at least 1.");

        var recipientId = await _characters.GetCharacterIdByNameAsync(recipientName, cancellationToken);
        if (!recipientId.HasValue)
            return Response<TransferInventoryItemResponseDto>.Fail($"Player '{recipientName}' was not found.");
        if (recipientId.Value == request.CharacterId)
            return Response<TransferInventoryItemResponseDto>.Fail("You cannot transfer an item to yourself.");

        var transfer = await _inventory.TransferItemAsync(
            request.CharacterId,
            recipientId.Value,
            request.ItemInstanceId,
            request.Quantity,
            cancellationToken);

        if (!transfer.IsSuccess ||
            transfer.TransferredItem is null ||
            transfer.TransferRecord is null)
            return Response<TransferInventoryItemResponseDto>.Fail(GetFailureMessage(transfer.Failure));

        var itemSummary = $"{request.Quantity:N0} {transfer.TransferRecord.AssetName}";
        var sentMessage = $"You sent {itemSummary} to {transfer.TransferRecord.RecipientCharacterName}.";
        var receivedMessage = $"You received {itemSummary} from {transfer.TransferRecord.SenderCharacterName}.";
        await QueueChatMessageAsync(
            transfer.TransferRecord.Id,
            transfer.TransferRecord.SenderCharacterId,
            transfer.TransferRecord.SenderAccountId,
            sentMessage,
            transfer.TransferRecord.OccurredAt,
            cancellationToken);
        await QueueChatMessageAsync(
            transfer.TransferRecord.Id,
            transfer.TransferRecord.RecipientCharacterId,
            transfer.TransferRecord.RecipientAccountId,
            receivedMessage,
            transfer.TransferRecord.OccurredAt,
            cancellationToken);

        var transferredItem = _mapper.Map<InventoryItemDto>(transfer.TransferredItem);
        var transferredItems = new[] { transferredItem };
        const string source = "player-transfer";
        var tradeCounterparty = transfer.TransferRecord.SenderCharacterName;

        await _lootHistory.RecordAsync(
            recipientId.Value,
            transferredItems,
            source,
            tradeCounterparty,
            cancellationToken);

        await _gameRealtime.PublishAsync(
            new Audience.Character(recipientId.Value),
            new LootReceived(recipientId.Value, transferredItems, source, tradeCounterparty),
            nameof(TransferInventoryItemCommandHandler),
            cancellationToken);

        var senderInventory = await _inventory.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);
        if (senderInventory is null)
            return Response<TransferInventoryItemResponseDto>.Fail(
                "Your inventory could not be loaded.");

        return Response<TransferInventoryItemResponseDto>.Success(new TransferInventoryItemResponseDto
        {
            ItemInstanceId = request.ItemInstanceId,
            RecipientName = transfer.TransferRecord.RecipientCharacterName,
            Quantity = request.Quantity,
            InventoryItems = _mapper.Map<List<InventoryItemDto>>(senderInventory.InventoryItems)
        });
    }

    private async Task QueueChatMessageAsync(
        Guid transferId,
        Guid characterId,
        Guid accountId,
        string message,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid();
        await _outbox.EnqueueAsync(
            GameEventTypes.PlayerTransferChatMessage,
            new PlayerTransferChatMessagePayload(
                transferId,
                messageId,
                characterId,
                message,
                occurredAt),
            characterId,
            accountId,
            cancellationToken);

        await _gameRealtime.PublishAsync(
            new Audience.Character(characterId),
            new PlayerTransfer(transferId, messageId, characterId, message),
            nameof(TransferInventoryItemCommandHandler),
            cancellationToken);
    }

    private static string GetFailureMessage(InventoryTransferFailure failure) => failure switch
    {
        InventoryTransferFailure.SameRecipient => "You cannot transfer an item to yourself.",
        InventoryTransferFailure.InvalidQuantity => "Transfer quantity must be at least 1.",
        InventoryTransferFailure.ItemNotFound => "The item is no longer in your inventory.",
        InventoryTransferFailure.ItemIsBound => "Bound items cannot be transferred.",
        InventoryTransferFailure.NonStackableQuantity => "This item can only be transferred one at a time.",
        InventoryTransferFailure.InsufficientQuantity => "You do not have enough of this item.",
        InventoryTransferFailure.BorrowedGuildItem => "Borrowed guild-vault items cannot be transferred.",
        InventoryTransferFailure.SenderNotFound => "Your character could not be found.",
        InventoryTransferFailure.RecipientNotFound => "The receiving player could not be found.",
        InventoryTransferFailure.RecipientInventoryNotFound => "The receiving player's inventory could not be found.",
        InventoryTransferFailure.AccountRestricted => "One of the accounts is restricted from player transfers.",
        InventoryTransferFailure.GuestAccount => "Guest accounts cannot send or receive items.",
        _ => "The item could not be transferred."
    };
}
