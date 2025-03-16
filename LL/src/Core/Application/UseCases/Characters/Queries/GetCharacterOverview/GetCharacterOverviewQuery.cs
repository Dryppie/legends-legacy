using Application.Common.Responses;
using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterOverview;
public record GetCharacterOverviewQuery(Guid CharacterId) : IRequest<Response<CharacterOverviewDto>>;

public class GetCharacterOverviewQueryHandler : IRequestHandler<GetCharacterOverviewQuery, Response<CharacterOverviewDto>>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;


    public GetCharacterOverviewQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<Response<CharacterOverviewDto>> Handle(GetCharacterOverviewQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var character = await _characterService.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);

            var characterOverviewDto = _mapper.Map<CharacterOverviewDto>(character);

            return Response<CharacterOverviewDto>.Success(characterOverviewDto);
        }
        catch (Exception)
        {
            return Response<CharacterOverviewDto>.Fail("Error getting character overview related to: " +  request.CharacterId);
        }
    }
}