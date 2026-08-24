using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Application.WebSockets.Contracts;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.InviteCharacterByName;
public record InviteCharacterByNameCommand(Guid CurrentCharacterId, InviteToGuildDto Invite) : ICommand<Response<bool>>;
public class InviteCharacterByNameCommandHandler : IRequestHandler<InviteCharacterByNameCommand, Response<bool>>
{
    private readonly IGuildService _guildService;
    private readonly ICharacterService _characterService;
    private readonly IGameRealtimeBroadcaster _eventPublisher;
    private readonly IGuildSystemChatPublisher _guildChat;

    public InviteCharacterByNameCommandHandler(
        IGuildService guildService,
        ICharacterService characterService,
        IGameRealtimeBroadcaster eventPublisher,
        IGuildSystemChatPublisher guildChat)
    {
        _guildService = guildService;
        _characterService = characterService;
        _eventPublisher = eventPublisher;
        _guildChat = guildChat;
    }

    public async Task<Response<bool>> Handle(InviteCharacterByNameCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Invite.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        var invitedCharacterId = await _characterService.GetCharacterIdByNameAsync(
            request.Invite.CharacterNameOrId,
            cancellationToken);
        if (invitedCharacterId == null)
            return Response<bool>.Fail("Failed to invite character.");

        var invited = await _guildService.InviteCharacterByNameAsync(
            request.CurrentCharacterId,
            guildId,
            request.Invite.CharacterNameOrId,
            cancellationToken);
        if (!invited)
            return Response<bool>.Fail("Failed to invite character.");

        await _guildChat.PublishAsync(
            guildId,
            invitedCharacterId.Value,
            GuildSystemChatEvent.Invited,
            cancellationToken);

        await _eventPublisher.PublishAsync(
            new Audience.Character(invitedCharacterId.Value),
            new GuildInviteReceived(guildId, invitedCharacterId.Value),
            nameof(InviteCharacterByNameCommandHandler),
            cancellationToken);
        return Response<bool>.Success(true);
    }
}
