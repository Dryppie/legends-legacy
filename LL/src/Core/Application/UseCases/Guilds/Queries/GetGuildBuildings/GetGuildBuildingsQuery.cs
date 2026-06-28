using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Markers;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetGuildBuildings;

public record GetGuildBuildingsQuery(Guid CharacterId) : IQuery<GuildBuildingOverviewDto?>;

public class GetGuildBuildingsQueryHandler : IRequestHandler<GetGuildBuildingsQuery, GuildBuildingOverviewDto?>
{
    private readonly IGuildBuildingService _guildBuildingService;

    public GetGuildBuildingsQueryHandler(IGuildBuildingService guildBuildingService)
    {
        _guildBuildingService = guildBuildingService;
    }

    public async Task<GuildBuildingOverviewDto?> Handle(GetGuildBuildingsQuery request, CancellationToken cancellationToken) =>
        await _guildBuildingService.GetOverviewAsync(request.CharacterId, DateTimeOffset.UtcNow, cancellationToken);
}
