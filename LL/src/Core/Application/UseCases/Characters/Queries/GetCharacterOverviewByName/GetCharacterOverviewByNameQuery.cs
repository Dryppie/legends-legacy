using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterOverviewByName;

public record GetCharacterOverviewByNameQuery(string CharacterName) : IQuery<Response<CharacterOverviewDto>>;

public sealed class GetCharacterOverviewByNameQueryHandler : IRequestHandler<GetCharacterOverviewByNameQuery, Response<CharacterOverviewDto>>
{
    private readonly ICharacterService _characters;
    private readonly IMapper _mapper;
    private readonly IPowerRatingService _powerRatings;
    private readonly IGuildRepository _guilds;

    public GetCharacterOverviewByNameQueryHandler(
        ICharacterService characters,
        IMapper mapper,
        IPowerRatingService powerRatings,
        IGuildRepository guilds)
    {
        _characters = characters;
        _mapper = mapper;
        _powerRatings = powerRatings;
        _guilds = guilds;
    }

    public async Task<Response<CharacterOverviewDto>> Handle(
        GetCharacterOverviewByNameQuery request,
        CancellationToken cancellationToken)
    {
        var character = await _characters.GetCharacterOverviewByNameAsync(request.CharacterName, cancellationToken);
        if (character is null)
            return Response<CharacterOverviewDto>.Fail("Failed to get character overview.");

        var dto = _mapper.Map<CharacterOverviewDto>(character);
        dto.Power = await _powerRatings.GetCharacterOverallRatingAsync(character.Id, cancellationToken);
        dto.Guild = CharacterGuildDto.From(
            await _guilds.GetGuildMember(character.Id, cancellationToken));
        return Response<CharacterOverviewDto>.Success(dto);
    }
}
