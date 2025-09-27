using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacter;
public record GetCharacterQuery(Guid UserId) : IQuery<Response<CharacterDto>>;
public class GetCharacterQueryHandler : IRequestHandler<GetCharacterQuery, Response<CharacterDto>>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;

    public GetCharacterQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<Response<CharacterDto>> Handle(GetCharacterQuery request, CancellationToken cancellationToken)
    {
        var character =  await _characterService.GetMyCharacterAsync(request.UserId, cancellationToken);

        var dto = _mapper.Map<CharacterDto>(character);

        return Response<CharacterDto>.Success(dto);
    }
}