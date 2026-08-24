using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.LeaveGuild;
public record LeaveGuildCommand(Guid CharacterId) : ICommand<Response<bool>>;
public class LeaveGuildCommandHandler : IRequestHandler<LeaveGuildCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGameEventOutbox _outbox;
    private readonly IGuildSystemChatPublisher _guildChat;

    public LeaveGuildCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher,
        IGameEventOutbox outbox,
        IGuildSystemChatPublisher guildChat)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
        _outbox = outbox;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(LeaveGuildCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildService.GetGuildForMemberAsync(request.CharacterId, cancellationToken);
        if (guild == null)
            return Response<bool>.Fail("Failed to leave guild");

        var left = await _guildService.LeaveGuildAsync(request.CharacterId, cancellationToken);
        if (!left)
            return Response<bool>.Fail("Failed to leave guild");

        await _outbox.EnqueueAsync(
            GameEventTypes.EquipmentChanged,
            new EquipmentChangedPayload(request.CharacterId),
            request.CharacterId,
            null,
            cancellationToken);

        await _guildChat.PublishAsync(
            guild.Id,
            request.CharacterId,
            GuildSystemChatEvent.Left,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChanged("membership", request.CharacterId),
            nameof(LeaveGuildCommandHandler),
            cancellationToken);

        return Response<bool>.Success(true);
    }
}
