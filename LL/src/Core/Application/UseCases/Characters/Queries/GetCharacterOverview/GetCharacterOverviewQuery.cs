using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Guilds;
using MediatR;

namespace Application.UseCases.Characters.Queries.GetCharacterOverview;

public record GetCharacterOverviewQuery(Guid CharacterId) : IQuery<Response<CharacterOverviewDto>>;

public sealed class GetCharacterOverviewQueryHandler : IRequestHandler<GetCharacterOverviewQuery, Response<CharacterOverviewDto>>
{
    private readonly ICharacterService _characters;
    private readonly IMapper _mapper;
    private readonly IPowerRatingService _powerRatings;
    private readonly IGuildRepository _guilds;

    public GetCharacterOverviewQueryHandler(
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
        GetCharacterOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var character = await _characters.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);
        if (character is null)
            return Response<CharacterOverviewDto>.Fail("Failed to get character overview.");

        var dto = _mapper.Map<CharacterOverviewDto>(character);
        dto.Power = await _powerRatings.GetCharacterOverallRatingAsync(character, cancellationToken);
        dto.Guild = CharacterGuildDto.From(
            await _guilds.GetGuildMember(character.Id, cancellationToken));
        return Response<CharacterOverviewDto>.Success(dto);
    }
}
