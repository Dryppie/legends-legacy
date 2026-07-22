using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterOverview;

public record GetCharacterOverviewQuery(Guid CharacterId) : IQuery<Response<CharacterOverviewDto>>;

public sealed class GetCharacterOverviewQueryHandler : IRequestHandler<GetCharacterOverviewQuery, Response<CharacterOverviewDto>>
{
    private readonly ICharacterService _characters;
    private readonly IMapper _mapper;
    private readonly IPowerRatingService _powerRatings;

    public GetCharacterOverviewQueryHandler(
        ICharacterService characters,
        IMapper mapper,
        IPowerRatingService powerRatings)
    {
        _characters = characters;
        _mapper = mapper;
        _powerRatings = powerRatings;
    }

    public async Task<Response<CharacterOverviewDto>> Handle(
        GetCharacterOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var character = await _characters.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);
        if (character is null)
            return Response<CharacterOverviewDto>.Fail("Failed to get character overview.");

        var dto = _mapper.Map<CharacterOverviewDto>(character);
        dto.Power = await _powerRatings.GetCharacterRatingAsync(request.CharacterId, cancellationToken);
        return Response<CharacterOverviewDto>.Success(dto);
    }
}
