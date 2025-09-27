using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.InviteCharacterByName;
public record InviteCharacterByNameCommand(Guid CurrentCharacterId, InviteToGuildDto Invite) : ICommand<Response<bool>>;
public class InviteCharacterByNameCommandHandler : IRequestHandler<InviteCharacterByNameCommand, Response<bool>>
{
    private readonly IGuildService _guildService;

    public InviteCharacterByNameCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task<Response<bool>> Handle(InviteCharacterByNameCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Invite.GuildId, out var guildId)) return Response<bool>.Fail("Invalid guild.");

        return await _guildService.InviteCharacterByNameAsync(request.CurrentCharacterId, guildId, request.Invite.CharacterNameOrId, cancellationToken)
            ? Response<bool>.Success(true)
            : Response<bool>.Fail("Failed to invite character.");
    }
}