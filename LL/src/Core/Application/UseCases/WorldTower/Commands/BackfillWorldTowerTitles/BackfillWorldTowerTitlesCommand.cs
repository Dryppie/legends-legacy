using Application.Interfaces.Services.LL.WorldTower;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.WorldTower.Commands.BackfillWorldTowerTitles;

public sealed record BackfillWorldTowerTitlesCommand(int FloorNumber) : ICommand<Response<int>>;

public sealed class BackfillWorldTowerTitlesCommandHandler(IWorldTowerTitleService titles)
    : IRequestHandler<BackfillWorldTowerTitlesCommand, Response<int>>
{
    public async Task<Response<int>> Handle(BackfillWorldTowerTitlesCommand request, CancellationToken cancellationToken) =>
        Response<int>.Success(await titles.BackfillAsync(request.FloorNumber, cancellationToken));
}
