using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Colosseum.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetColosseumMatchResults;
public record GetColosseumMatchResultsQuery(Guid CharacterId) : IRequest<List<ColosseumMatchResultDto>>;
public class GetColosseumMatchResultsQueryHandler : IRequestHandler<GetColosseumMatchResultsQuery, List<ColosseumMatchResultDto>>
{

    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetColosseumMatchResultsQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<List<ColosseumMatchResultDto>> Handle(GetColosseumMatchResultsQuery request, CancellationToken cancellationToken)
    {
        var matchResults = await _colosseumService.GetColosseumMatchResults(request.CharacterId, cancellationToken);

        return _mapper.Map<List<ColosseumMatchResultDto>>(matchResults);
    }
}