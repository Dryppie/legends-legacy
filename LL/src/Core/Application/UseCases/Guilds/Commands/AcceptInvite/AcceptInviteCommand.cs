using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.AcceptInvite;
public record AcceptInviteCommand(Guid CharacterId, string GuildId) : IRequest;
public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand>
{
    private readonly IGuildService _guildService;

    public AcceptInviteCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
            throw new ArgumentException("Invalid GuildId");

        // Assuming your IGuildService has a method like:
        // Task InviteCharacterAsync(Guid inviterId, Guid guildId, Guid invitedCharacterId);
        await _guildService.AcceptInviteAsync(request.CharacterId, guildId, cancellationToken);
    }
}