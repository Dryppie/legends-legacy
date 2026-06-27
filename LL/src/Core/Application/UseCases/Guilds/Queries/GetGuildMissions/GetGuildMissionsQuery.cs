using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
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

    public async Task<GuildMissionOverviewDto?> Handle(GetGuildMissionsQuery request, CancellationToken cancellationToken) =>
        await _guildMissionService.GetOverviewAsync(request.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
}
