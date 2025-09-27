using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.Invite;
public record InviteCommand(Guid CurrentCharacterId, InviteToGuildDto Invite) : ICommand<Response<bool>>;
public class InviteCommandHandler : IRequestHandler<InviteCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public InviteCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(InviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Invite.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        if (!Guid.TryParse(request.Invite.CharacterNameOrId, out var invitedCharacterId)) return Response<bool>.Fail("Invalid character.");

        return await _guildService.InviteAsync(request.CurrentCharacterId, guildId, invitedCharacterId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to invite character.");
    }
}