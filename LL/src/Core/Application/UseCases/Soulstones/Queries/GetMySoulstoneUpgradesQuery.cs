using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Models.Soulstones.UpgradeDefinition;
using MediatR;

namespace Application.UseCases.Soulstones.Queries;
public record GetMySoulstoneUpgradesQuery(Guid CharacterId) : IQuery<Response<List<SoulstoneUpgradeView>>>;
public class GetMySoulstoneUpgradesQueryHandler : IRequestHandler<GetMySoulstoneUpgradesQuery, Response<List<SoulstoneUpgradeView>>>
{
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;

    public GetMySoulstoneUpgradesQueryHandler(ISoulstoneUpgradeService soulstoneUpgradeService)
    {
        _soulstoneUpgradeService = soulstoneUpgradeService;
    }

    public async Task<Response<List<SoulstoneUpgradeView>>> Handle(GetMySoulstoneUpgradesQuery request, CancellationToken cancellationToken)
    {
        return Response<List<SoulstoneUpgradeView>>.Success(await _soulstoneUpgradeService.GetForCharacterAsync(request.CharacterId, cancellationToken));
    }
}
