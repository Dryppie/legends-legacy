using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.WithdrawGuildVaultItem;

public record WithdrawGuildVaultItemCommand(Guid CharacterId, Guid VaultItemId) : ICommand<Response<bool>>;

public class WithdrawGuildVaultItemCommandHandler : IRequestHandler<WithdrawGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGameRealtimeBroadcaster _events;
    private readonly IGameEventOutbox _outbox;
    private readonly IMapper _mapper;

    public WithdrawGuildVaultItemCommandHandler(
        IGuildVaultService vault,
        IGameRealtimeBroadcaster events,
        IGameEventOutbox outbox,
        IMapper mapper)
    {
        _vault = vault;
        _events = events;
        _outbox = outbox;
        _mapper = mapper;
    }

    public async Task<Response<bool>> Handle(WithdrawGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _vault.WithdrawAsync(request.CharacterId, request.VaultItemId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to withdraw equipment.");

        var mutation = result.Value!;
        var equipment = _mapper.Map<EquipmentInstanceDto>(mutation.Equipment);
        var messageId = Guid.NewGuid();
        var sentAt = DateTimeOffset.UtcNow;
        await _outbox.EnqueueAsync(
            GameEventTypes.GuildVaultChatMessage,
            new GuildVaultChatMessagePayload(
                mutation.GuildId,
                mutation.CharacterId,
                mutation.CharacterName,
                "withdrew",
                equipment,
                messageId,
                sentAt),
            request.CharacterId,
            null,
            cancellationToken);

        await _events.PublishAsync(
            new Audience.Guild(mutation.GuildId),
            new GuildVaultChatMessage(
                mutation.GuildId,
                messageId,
                mutation.CharacterId,
                mutation.CharacterName,
                "withdrew",
                equipment,
                sentAt),
            nameof(WithdrawGuildVaultItemCommandHandler),
            cancellationToken);

        await _events.PublishAsync(
            new Audience.Guild(mutation.GuildId),
            new GuildStateChanged(mutation.GuildId, request.CharacterId, true),
            nameof(WithdrawGuildVaultItemCommandHandler),
            cancellationToken);
        return Response<bool>.Success(true);
    }
}
