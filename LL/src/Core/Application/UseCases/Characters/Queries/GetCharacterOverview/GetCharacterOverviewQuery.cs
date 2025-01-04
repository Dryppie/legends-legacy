using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterOverview;
public record GetCharacterOverviewQuery(Guid CharacterId) : IRequest<CharacterOverviewDto>;

public class GetCharacterOverviewQueryHandler : IRequestHandler<GetCharacterOverviewQuery, CharacterOverviewDto>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;


    public GetCharacterOverviewQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<CharacterOverviewDto> Handle(GetCharacterOverviewQuery request, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetMyCharacterOverviewAsync(request.CharacterId);

        return _mapper.Map<CharacterOverviewDto>(character);
    }
}