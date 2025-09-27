using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterOverview;
public record GetCharacterOverviewQuery(Guid CharacterId) : IQuery<Response<CharacterOverviewDto>>;

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
        var character = await _characterService.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);

        return character != null
            ? Response<CharacterOverviewDto>.Success(_mapper.Map<CharacterOverviewDto>(character))
            : Response<CharacterOverviewDto>.Fail("Failed to get character overview.");
    }
}