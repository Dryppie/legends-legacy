using Application.Interfaces.Services.LL;
using Application.UseCases.Guilds.Dtos.Requests;
using MediatR;

namespace Application.UseCases.Guilds.Commands.InviteCharacterByName;
public record InviteCharacterByNameCommand(Guid CurrentCharacterId, InviteToGuildDto Invite) : IRequest;
public class InviteCharacterByNameCommandHandler : IRequestHandler<InviteCharacterByNameCommand>
{
    private readonly IGuildService _guildService;

    public InviteCharacterByNameCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(InviteCharacterByNameCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Invite.GuildId, out var guildId))
            throw new ArgumentException("Invalid GuildId");

        // Assuming your IGuildService has a method like:
        // Task InviteCharacterAsync(Guid inviterId, Guid guildId, Guid invitedCharacterId);
        await _guildService.InviteCharacterByNameAsync(request.CurrentCharacterId, guildId, request.Invite.CharacterNameOrId, cancellationToken);
    }
}