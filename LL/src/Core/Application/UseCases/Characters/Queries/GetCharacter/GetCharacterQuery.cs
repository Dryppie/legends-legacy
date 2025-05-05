using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacter;
public record GetCharacterQuery(Guid UserId) : IRequest<Response<CharacterDto>>;
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