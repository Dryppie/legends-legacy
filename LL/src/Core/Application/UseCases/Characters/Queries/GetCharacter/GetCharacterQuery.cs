using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Domain.Models.Entities.Characters;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacter;
public record GetCharacterQuery(Guid CharacterId) : IRequest<Response<CharacterDto>>;

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
        try
        {
            var character =  await _characterService.GetMyCharacterAsync(request.CharacterId);

            var dto = _mapper.Map<CharacterDto>(character);

            return Response<CharacterDto>.Success(dto);
        }
        catch (Exception)
        {
            return Response<CharacterDto>.Fail("Error getting character by: " +  request.CharacterId);
        }
    }
}