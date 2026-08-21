using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.AcceptInvite;
public record AcceptInviteCommand(Guid CharacterId, string GuildId) : ICommand<Response<bool>>;
public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGuildSystemChatPublisher _guildChat;

    public AcceptInviteCommandHandler(
        IGuildService guildService,
        IGameRealtimeBroadcaster eventPublisher,
        IGuildSystemChatPublisher guildChat)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        var accepted = await _guildService.AcceptInviteAsync(request.CharacterId, guildId, cancellationToken);
        if (!accepted)
            return Response<bool>.Fail("Failed to accept invite.");

        await _guildChat.PublishAsync(
            guildId,
            request.CharacterId,
            GuildSystemChatEvent.Joined,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.Character(request.CharacterId),
            new GuildMembershipChanged(guildId, request.CharacterId),
            nameof(AcceptInviteCommandHandler),
            cancellationToken);
        await _eventPublisher.PublishAsync(
            new Audience.Guild(guildId),
            new GuildStateChanged(guildId),
            nameof(AcceptInviteCommandHandler),
            cancellationToken);
        await _eventPublisher.PublishAsync(
            new Audience.World(),
            new GuildDirectoryChanged("membership"),
            nameof(AcceptInviteCommandHandler),
            cancellationToken);

        return Response<bool>.Success(true);
    }
}
