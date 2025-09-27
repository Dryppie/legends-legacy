using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Leaderboards.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Leaderboards.Queries.GetLeaderboard;
public record GetLeaderboardQuery(Guid CharacterId) : IQuery<Response<LeaderboardDto>>;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, Response<LeaderboardDto>>
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IMapper _mapper;

    public GetLeaderboardQueryHandler(ILeaderboardService leaderboardService, IMapper mapper)
    {
        _leaderboardService = leaderboardService;
        _mapper = mapper;
    }

    public async Task<Response<LeaderboardDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var leaderboard = await _leaderboardService.GetLeaderboardAsync(request.CharacterId, cancellationToken);

        return Response<LeaderboardDto>.Success(_mapper.Map<LeaderboardDto>(leaderboard));
    }
}