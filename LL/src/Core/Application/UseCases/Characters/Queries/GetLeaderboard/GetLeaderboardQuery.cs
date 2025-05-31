using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetLeaderboard;
public record GetLeaderboardQuery() : IRequest<Response<List<CharacterLeaderboardDto>>>;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, Response<List<CharacterLeaderboardDto>>>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;

    public GetLeaderboardQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<Response<List<CharacterLeaderboardDto>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var characters = await _characterService.GetLeaderboardCharactersAsync(cancellationToken);

        return Response<List<CharacterLeaderboardDto>>.Success(_mapper.Map<List<CharacterLeaderboardDto>>(characters));
    }
}

