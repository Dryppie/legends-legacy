using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Leaderboards.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;
using Domain.Models.Leaderboards;

namespace Application.UseCases.Leaderboards.Queries.GetLeaderboard;
public record GetLeaderboardQuery(
    Guid CharacterId,
    string BoardKey,
    int Limit = 50,
    string? Cursor = null,
    string? Search = null)
    : IQuery<Response<LeaderboardBoardDto>>;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, Response<LeaderboardBoardDto>>
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IMapper _mapper;

    public GetLeaderboardQueryHandler(ILeaderboardService leaderboardService, IMapper mapper)
    {
        _leaderboardService = leaderboardService;
        _mapper = mapper;
    }

    public async Task<Response<LeaderboardBoardDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        if (!LeaderboardBoardKey.All.Contains(request.BoardKey))
        {
            return Response<LeaderboardBoardDto>.Fail("Unknown leaderboard board.");
        }

        if (!string.IsNullOrWhiteSpace(request.Cursor) &&
            !LeaderboardCursor.TryDecode(request.BoardKey, request.Cursor, out _))
        {
            return Response<LeaderboardBoardDto>.Fail("Invalid leaderboard cursor.");
        }

        if (request.Search?.Length > 80)
        {
            return Response<LeaderboardBoardDto>.Fail(
                "Leaderboard participant searches cannot exceed 80 characters.");
        }

        var leaderboard = await _leaderboardService.GetLeaderboardAsync(
            request.CharacterId,
            request.BoardKey,
            request.Limit,
            request.Cursor,
            request.Search,
            cancellationToken);

        return Response<LeaderboardBoardDto>.Success(_mapper.Map<LeaderboardBoardDto>(leaderboard));
    }
}
