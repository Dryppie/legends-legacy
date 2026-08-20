using Application.Interfaces.Services.LL.WorldTower;
using Application.MediatR.Markers;
using Application.UseCases.WorldTower.Dtos;
using Common.Primitives;
using Domain.Models.WorldTower;
using MediatR;

namespace Application.UseCases.WorldTower.Commands.UpdateTowerRallyParties;

public sealed record UpdateTowerRallyPartiesCommand(
    Guid CharacterId,
    Guid RallyId,
    IReadOnlyList<TowerPartyAssignment> Assignments)
    : ICommand<Response<TowerRallyDto>>;

public sealed class UpdateTowerRallyPartiesCommandHandler(IWorldTowerService tower)
    : IRequestHandler<UpdateTowerRallyPartiesCommand, Response<TowerRallyDto>>
{
    public async Task<Response<TowerRallyDto>> Handle(
        UpdateTowerRallyPartiesCommand request,
        CancellationToken cancellationToken)
    {
        var result = await tower.UpdateRallyPartiesAsync(
            request.CharacterId,
            request.RallyId,
            request.Assignments,
            cancellationToken);
        return result.Succeeded
            ? Response<TowerRallyDto>.Success(result.Value!)
            : Response<TowerRallyDto>.Fail(result.Error!);
    }
}
