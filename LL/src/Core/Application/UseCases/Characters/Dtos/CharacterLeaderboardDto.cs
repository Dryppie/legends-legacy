using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Entities.Characters;

namespace Application.UseCases.Characters.Dtos;
public class CharacterLeaderboardDto : IMapFrom<CharacterLeaderboardItem>
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CharacterLeaderboardItem, CharacterLeaderboardDto>();
    }
}
