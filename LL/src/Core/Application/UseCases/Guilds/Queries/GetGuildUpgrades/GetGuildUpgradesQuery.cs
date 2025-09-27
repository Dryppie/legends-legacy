using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Guilds.Buildings;
using MediatR;

namespace Application.UseCases.Guilds.Queries.GetGuildUpgrades;
public record GetGuildUpgradesQuery(Guid CharacterId) : IQuery<Response<List<BuildingUpgradeView>>>;
public class GetGuildUpgradesQueryHandler : IRequestHandler<GetGuildUpgradesQuery, Response<List<BuildingUpgradeView>>>
{
    private readonly IGuildBuildingUpgradeService _upgradeService;
    public GetGuildUpgradesQueryHandler(IGuildBuildingUpgradeService upgradeService)
    {
        _upgradeService = upgradeService;
    }
    public async Task<Response<List<BuildingUpgradeView>>> Handle(GetGuildUpgradesQuery request, CancellationToken cancellationToken)
    {
        return Response<List<BuildingUpgradeView>>.Success(await _upgradeService.GetForGuildAsync(request.CharacterId, cancellationToken));
    }
}