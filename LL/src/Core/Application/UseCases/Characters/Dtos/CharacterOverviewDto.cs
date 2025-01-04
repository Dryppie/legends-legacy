using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;

namespace Application.UseCases.Characters.Dtos;
public class CharacterOverviewDto : IMapFrom<Character>
{
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<Essence> EquippedEssences { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character,  CharacterOverviewDto>();
    }

}