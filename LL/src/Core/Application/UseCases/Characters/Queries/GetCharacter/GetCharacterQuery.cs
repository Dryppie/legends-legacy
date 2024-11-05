using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Domain.Models.Entities.Characters;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacter;
public record GetCharacterQuery(Guid CharacterId) : IRequest<CharacterDto>;

public class GetCharacterQueryHandler : IRequestHandler<GetCharacterQuery, CharacterDto>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;


    public GetCharacterQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<CharacterDto> Handle(GetCharacterQuery request, CancellationToken cancellationToken)
    {
        var character =  await _characterService.GetMyCharacterAsync(request.CharacterId);

        return _mapper.Map<CharacterDto>(character);
    }
}