using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.EssenceSlots;

namespace Application.UseCases.Characters.Dtos;
public class CharacterOverviewDto : IMapFrom<Character>
{
    public int Level { get; set; }
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<EssenceSlot> EssenceSlots { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character,  CharacterOverviewDto>();
    }

}