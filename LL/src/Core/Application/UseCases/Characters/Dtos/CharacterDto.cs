using Application.Common.Mappings;
using Application.UseCases.Achievements.Dtos;
using AutoMapper;
using Domain.Models.Achievements;
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
    public int ArenaRating { get; set; }
    public EquippedTitleDto? EquippedTitle { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterDto>()
            .ForMember(
                dest => dest.EquippedTitle,
                opt => opt.MapFrom(src => src.EquippedTitleDefinition == null
                    ? null
                    : new EquippedTitleDto
                    {
                        Key = src.EquippedTitleDefinition.Key,
                        Name = src.EquippedTitleDefinition.Name,
                        DisplayPosition = src.EquippedTitleDefinition.DisplayPosition,
                        DisplayName = src.EquippedTitleDefinition.DisplayPosition == TitleDisplayPosition.Prefix
                            ? $"{src.EquippedTitleDefinition.Name} {src.Name}"
                            : $"{src.Name}, {src.EquippedTitleDefinition.Name}"
                    }));
    }
}
