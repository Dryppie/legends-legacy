using Application.Interfaces.Services.LL;
using MediatR;

namespace Application.UseCases.Guilds.Commands.ApplyToGuild;
public record ApplyToGuildCommand(Guid CharacterId, string GuildId) : IRequest;
public class ApplyToGuildCommandHandler : IRequestHandler<ApplyToGuildCommand>
{
    private readonly IGuildService _guildService;

    public ApplyToGuildCommandHandler(IGuildService guildService)
    {
        _guildService = guildService;
    }

    public async Task Handle(ApplyToGuildCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.GuildId, out var guildId))
            throw new ArgumentException("Invalid GuildId");

        await _guildService.ApplyToGuildAsync(request.CharacterId, guildId, cancellationToken);
    }
}