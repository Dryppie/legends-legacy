using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.RejectInvite;
public record RejectInviteCommand(Guid CharacterId, string GuildId) : IRequest;
public class RejectInviteCommandHandler : IRequestHandler<RejectInviteCommand>
{
    private readonly IGuildService _guildService;

    public RejectInviteCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(RejectInviteCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
            throw new ArgumentException("Invalid GuildId");

        await _guildService.RejectInviteAsync(request.CharacterId, guildId, cancellationToken);
    }
}