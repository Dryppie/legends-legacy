using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ReturnGuildVaultItem;

public record ReturnGuildVaultItemCommand(Guid CharacterId, Guid VaultItemId) : ICommand<Response<bool>>;

public class ReturnGuildVaultItemCommandHandler : IRequestHandler<ReturnGuildVaultItemCommand, Response<bool>>
{
    private readonly IGuildVaultService _vault;
    private readonly IGuildService _guild;
    private readonly IGameRealtimeBroadcaster _events;
    private readonly IGameEventOutbox _outbox;
    public ReturnGuildVaultItemCommandHandler(IGuildVaultService vault, IGuildService guild, IGameRealtimeBroadcaster events, IGameEventOutbox outbox)
    {
        _vault = vault;
        _guild = guild;
        _events = events;
        _outbox = outbox;
    }

    public async Task<Response<bool>> Handle(ReturnGuildVaultItemCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guild.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        var result = await _vault.ReturnAsync(request.CharacterId, request.VaultItemId, cancellationToken);
        if (!result.Succeeded) return Response<bool>.Fail(result.Error ?? "Failed to return equipment.");
        await _outbox.EnqueueAsync(GameEventTypes.EquipmentChanged, new EquipmentChangedPayload(request.CharacterId), request.CharacterId, null, cancellationToken);
        if (guild is not null)
            await _events.PublishAsync(new Audience.Guild(guild.Id), new GuildStateChanged(guild.Id, request.CharacterId, true), nameof(ReturnGuildVaultItemCommandHandler), cancellationToken);
        return Response<bool>.Success(true);
    }
}
