using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences.EssenceSlots;

namespace Application.UseCases.Characters.Dtos;
public class CharacterOverviewDto : IMapFrom<Character>
{
    public int Level { get; set; }
    public List<EntityAttribute> BaseAttributes { get; set; } = [];
    public List<EntityAttribute> BaseCombatAttributes { get; set; } = [];
    public List<EssenceSlot> EssenceSlots { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Character, CharacterOverviewDto>()
            .ForMember(dest => dest.BaseCombatAttributes, opt => opt.MapFrom(src =>
                src.BaseCombatAttributes.Select(kvp => new EntityAttribute
                {
                    EntityId = src.Id, // Assuming your Character has an Id property
                    AttributeType = kvp.Key,
                    Value = kvp.Value
                }).ToList()
            ));
    }

}