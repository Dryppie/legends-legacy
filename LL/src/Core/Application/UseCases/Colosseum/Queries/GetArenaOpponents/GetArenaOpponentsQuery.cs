using Application.Interfaces.Services.LL.Colosseum;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Colosseum.Queries.GetArenaOpponents;
public record GetArenaOpponentsQuery(Guid CharacterId) : IRequest<List<CharacterDto>>;
public class GetArenaOpponentsQueryHandler : IRequestHandler<GetArenaOpponentsQuery, List<CharacterDto>>
{

    private readonly IColosseumService _colosseumService;
    private readonly IMapper _mapper;

    public GetArenaOpponentsQueryHandler(IColosseumService colosseumService, IMapper mapper)
    {
        _colosseumService = colosseumService;
        _mapper = mapper;
    }

    public async Task<List<CharacterDto>> Handle(GetArenaOpponentsQuery request, CancellationToken cancellationToken)
    {
        var arenaOpponents = await _colosseumService.GetArenaOpponents(request.CharacterId, cancellationToken);

        return _mapper.Map<List<CharacterDto>>(arenaOpponents);
    }
}