using Application.Interfaces.Services.LL;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Entities.Characters;
using MediatR;

namespace Application.UseCases.Characters.Commands.WireCinders;

public sealed record WireCindersCommand(
    Guid CharacterId,
    string RecipientName,
    long Amount,
    string Currency) : ICommand<Response<WireCindersResponseDto>>;

public sealed class WireCindersCommandHandler(
    ICurrencyTransferService currencyTransfers,
    IGameEventOutbox outbox,
    IGameEventPublisher legacyEvents,
    IGameRealtimeBroadcaster gameRealtime,
    IMapper mapper)
    : IRequestHandler<WireCindersCommand, Response<WireCindersResponseDto>>
{
    public async Task<Response<WireCindersResponseDto>> Handle(
        WireCindersCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Currency.Trim(), "Cinders", StringComparison.OrdinalIgnoreCase))
            return Response<WireCindersResponseDto>.Fail("Only Cinders can currently be wired.");
        if (request.Amount <= 0)
            return Response<WireCindersResponseDto>.Fail("The wire amount must be at least 1 Cinder.");

        var result = await currencyTransfers.TransferCindersAsync(
            request.CharacterId,
            request.RecipientName,
            request.Amount,
            cancellationToken);

        if (!result.IsSuccess ||
            result.Sender is null ||
            result.Recipient is null ||
            result.TransferRecord is null)
            return Response<WireCindersResponseDto>.Fail(GetFailureMessage(result.Failure));

        var sentMessage = $"You sent {request.Amount:N0} Cinders to {result.Recipient.Name}.";
        var receivedMessage = $"You received {request.Amount:N0} Cinders from {result.Sender.Name}.";
        await QueueChatMessageAsync(
            result.TransferRecord.Id,
            result.Sender.Id,
            result.Sender.UserId,
            sentMessage,
            result.TransferRecord.OccurredAt,
            cancellationToken);
        await QueueChatMessageAsync(
            result.TransferRecord.Id,
            result.Recipient.Id,
            result.Recipient.UserId,
            receivedMessage,
            result.TransferRecord.OccurredAt,
            cancellationToken);

        var senderDto = mapper.Map<CharacterDto>(result.Sender);
        var recipientDto = mapper.Map<CharacterDto>(result.Recipient);

        await gameRealtime.PublishAsync(
            new Audience.Character(result.Sender.Id),
            new CharacterSnapshot(result.Sender.Id, senderDto, "cinders-wire-sent"),
            nameof(WireCindersCommandHandler),
            cancellationToken);

        await gameRealtime.PublishAsync(
            new Audience.Character(result.Recipient.Id),
            new CharacterSnapshot(result.Recipient.Id, recipientDto, "cinders-wire-received"),
            nameof(WireCindersCommandHandler),
            cancellationToken);

        return Response<WireCindersResponseDto>.Success(new WireCindersResponseDto
        {
            RecipientName = result.Recipient.Name,
            Amount = request.Amount,
            RemainingCinders = result.Sender.Cinders
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
        await outbox.EnqueueAsync(
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

        await legacyEvents.PublishAsync(
            new Audience.Character(characterId),
            new PlayerTransferMsg(transferId, messageId, characterId, message));
    }

    private static string GetFailureMessage(CinderTransferFailure failure) => failure switch
    {
        CinderTransferFailure.InvalidAmount => "The wire amount must be at least 1 Cinder.",
        CinderTransferFailure.RecipientNotFound => "The receiving player could not be found.",
        CinderTransferFailure.SameRecipient => "You cannot wire Cinders to yourself.",
        CinderTransferFailure.SenderNotFound => "Your character could not be found.",
        CinderTransferFailure.InsufficientCinders => "You do not have enough Cinders for this wire.",
        CinderTransferFailure.RecipientBalanceOverflow => "The receiving player cannot hold that many Cinders.",
        CinderTransferFailure.AccountRestricted => "One of the accounts is restricted from player transfers.",
        CinderTransferFailure.GuestAccount => "Guest accounts cannot send or receive Cinders.",
        _ => "The Cinders could not be wired."
    };
}
