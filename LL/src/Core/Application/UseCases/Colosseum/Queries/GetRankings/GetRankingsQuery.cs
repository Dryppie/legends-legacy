using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Leaderboards.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetRankings;
public record GetRankingsQuery(Guid CharacterId) : IQuery<List<LeaderboardEntryDto>>;
public class GetRankingsQueryHandler : IRequestHandler<GetRankingsQuery, List<LeaderboardEntryDto>>
{

    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetRankingsQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<List<LeaderboardEntryDto>> Handle(GetRankingsQuery request, CancellationToken cancellationToken)
    {
        var rankings = await _colosseumService.GetRankings(request.CharacterId, cancellationToken);

        return _mapper.Map<List<LeaderboardEntryDto>>(rankings);
    }
}