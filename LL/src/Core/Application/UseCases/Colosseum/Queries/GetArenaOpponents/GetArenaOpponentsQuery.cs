using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Application.UseCases.Colosseum.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetArenaOpponents;
public record GetArenaOpponentsQuery(Guid CharacterId) : IQuery<List<ArenaOpponentPreviewDto>>;
public class GetArenaOpponentsQueryHandler : IRequestHandler<GetArenaOpponentsQuery, List<ArenaOpponentPreviewDto>>
{
    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetArenaOpponentsQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<List<ArenaOpponentPreviewDto>> Handle(GetArenaOpponentsQuery request, CancellationToken cancellationToken)
    {
        var arenaOpponents = await _colosseumService.GetArenaOpponents(request.CharacterId, cancellationToken);

        return _mapper.Map<List<ArenaOpponentPreviewDto>>(arenaOpponents);
    }
}