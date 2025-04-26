using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Entities.Characters;

namespace Application.UseCases.Characters.Dtos;
public class CharacterDto : IMapFrom<Character>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
    public float ExperienceUntilNextLevel { get; set; }
    public int Gold { get; set; }
    public int ArenaRating { get; set; }
    //public List<AttributeDto> RawAttributes { get; set; } = [];
    //public List<AttributeDto> Attributes { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterDto>();
    }
}