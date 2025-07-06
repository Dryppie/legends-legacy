using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Colosseum.Dtos;
using Application.UseCases.Leaderboards.Dtos;
using AutoMapper;
using Domain.Models.Leaderboards;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetRankings;
public record GetRankingsQuery(Guid CharacterId) : IRequest<List<LeaderboardEntryDto>>;
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