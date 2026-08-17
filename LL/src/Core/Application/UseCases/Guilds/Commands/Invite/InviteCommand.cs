using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.Invite;
public record InviteCommand(Guid CurrentCharacterId, InviteToGuildDto Invite) : ICommand<Response<bool>>;
public class InviteCommandHandler : IRequestHandler<InviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly IGameEventPublisher _eventPublisher;
    private readonly IGuildSystemChatPublisher _guildChat;

    public InviteCommandHandler(
        IGuildService guildService,
        IGameEventPublisher eventPublisher,
        IGuildSystemChatPublisher guildChat)
    {
        _guildService = guildService;
        _eventPublisher = eventPublisher;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(InviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Invite.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        if (!Guid.TryParse(request.Invite.CharacterNameOrId, out var invitedCharacterId)) return Response<bool>.Fail("Invalid character.");

        var invited = await _guildService.InviteAsync(request.CurrentCharacterId, guildId, invitedCharacterId, cancellationToken);
        if (!invited)
            return Response<bool>.Fail("Failed to invite character.");

        await _guildChat.PublishAsync(
            guildId,
            invitedCharacterId,
            GuildSystemChatEvent.Invited,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.Character(invitedCharacterId),
            new GuildInviteReceivedMsg(guildId, invitedCharacterId));
        await _eventPublisher.PublishAsync(
            new Audience.Guild(guildId),
            new GuildStateChangedMsg(guildId));

        return Response<bool>.Success(true);
    }
}
