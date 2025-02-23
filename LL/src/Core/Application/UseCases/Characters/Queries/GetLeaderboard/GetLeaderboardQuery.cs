using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetLeaderboard;
public record GetLeaderboardQuery() : IRequest<List<CharacterLeaderboardDto>>;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, List<CharacterLeaderboardDto>>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;

    public GetLeaderboardQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<List<CharacterLeaderboardDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var characters = await _characterService.GetLeaderboardCharactersAsync(cancellationToken);

        return _mapper.Map<List<CharacterLeaderboardDto>>(characters);
    }
}

