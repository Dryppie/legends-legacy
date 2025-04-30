using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.Invite;
public record InviteCommand(Guid CurrentCharacterId, string GuildId, string InvitedCharacterId) : IRequest;
public class InviteCommandHandler : IRequestHandler<InviteCommand>
{
    private readonly IGuildService _guildService;

    public InviteCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(InviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
            throw new ArgumentException("Invalid GuildId");

        if (!Guid.TryParse(request.InvitedCharacterId, out var invitedCharacterId))
            throw new ArgumentException("Invalid InvitedCharacterId");

        // Assuming your IGuildService has a method like:
        // Task InviteCharacterAsync(Guid inviterId, Guid guildId, Guid invitedCharacterId);
        await _guildService.InviteAsync(request.CurrentCharacterId, guildId, invitedCharacterId, cancellationToken);
    }
}