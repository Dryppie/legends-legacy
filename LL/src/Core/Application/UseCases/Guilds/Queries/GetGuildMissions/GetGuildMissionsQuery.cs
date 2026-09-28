using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using Application.MediatR.Synchronization;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetGuildMissions;

public record GetGuildMissionsQuery(Guid CharacterId) : IQuery<GuildMissionOverviewDto?>;

public class GetGuildMissionsQueryHandler : IRequestHandler<GetGuildMissionsQuery, GuildMissionOverviewDto?>
{
    private readonly IGuildMissionService _guildMissionService;

    public GetGuildMissionsQueryHandler(IGuildMissionService guildMissionService)
    {
        _guildMissionService = guildMissionService;
    }

    public async Task<GuildMissionOverviewDto?> Handle(GetGuildMissionsQuery request, CancellationToken cancellationToken)
    {
        // The overview initializes mission state in its own transaction. Join the
        // command queue before opening it so same-process contention does not
        // consume the PostgreSQL advisory-lock command timeout.
        using var commandLock = await CharacterCommandLockRegistry.Instance.AcquireAsync(
            request.CharacterId,
            cancellationToken);

        return await _guildMissionService.GetOverviewAsync(request.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
    }
}
