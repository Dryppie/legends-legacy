using Application.Interfaces.Services.LL;
using Application.UseCases.Colosseum.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetRankings;
public record GetRankingsQuery(Guid CharacterId) : IRequest<List<ColosseumArenaRankDto>>;
public class GetRankingsQueryHandler : IRequestHandler<GetRankingsQuery, List<ColosseumArenaRankDto>>
{

    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetRankingsQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<List<ColosseumArenaRankDto>> Handle(GetRankingsQuery request, CancellationToken cancellationToken)
    {
        var rankings = await _colosseumService.GetRankings(request.CharacterId, cancellationToken);

        return _mapper.Map<List<ColosseumArenaRankDto>>(rankings);
    }
}