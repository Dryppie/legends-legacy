using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
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
    private readonly IGameEventPublisher _eventPublisher;

    public InviteCharacterByNameCommandHandler(
        IGuildService guildService,
        ICharacterService characterService,
        IGameEventPublisher eventPublisher)
    {
        _guildService = guildService;
        _characterService = characterService;
        _eventPublisher = eventPublisher;
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

        await _eventPublisher.PublishAsync(
            new Audience.Character(invitedCharacterId.Value),
            new GuildInviteReceivedMsg(guildId, invitedCharacterId.Value));
        await _eventPublisher.PublishAsync(
            new Audience.Guild(guildId),
            new GuildStateChangedMsg(guildId));

        return Response<bool>.Success(true);
    }
}
