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
    public long Cinders { get; set; } = 0;
    public long Soulstones { get; set; } = 0;
    public long FateEcho { get; set; } = 0;
    public long SigilFragments { get; set; } = 0;
    public long AscensionStoneFragments { get; set; } = 0;
    public int ArenaRating { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterDto>()
            .ForMember(dest => dest.ArenaRating, opt => opt.MapFrom(src => src.ArenaProfile != null ? src.ArenaProfile.Rating : 1000));
    }
}
