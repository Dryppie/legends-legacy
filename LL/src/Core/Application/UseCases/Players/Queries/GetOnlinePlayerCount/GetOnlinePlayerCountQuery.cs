using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using MediatR;

namespace Application.UseCases.Players.Queries.GetOnlinePlayerCount;
public record GetOnlinePlayerCountQuery() : IQuery<int>;
public class GetOnlinePlayerCountQueryHandler : IRequestHandler<GetOnlinePlayerCountQuery, int>
{
    private readonly IPlayerService _playerService;
    public GetOnlinePlayerCountQueryHandler(IPlayerService playerService)
    {
        _playerService = playerService;
    }
    public async Task<int> Handle(GetOnlinePlayerCountQuery request, CancellationToken cancellationToken)
    {
        return await _playerService.GetOnlinePlayerCountAsync(cancellationToken);
    }
}
