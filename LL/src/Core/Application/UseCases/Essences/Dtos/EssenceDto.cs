using Application.Common.Mappings;
using Application.UseCases.Abilities.Dtos;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;
public class EssenceDto : IMapFrom<Essence>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AbilityDto Passive { get; set; } = null!;
    public AbilityDto Active { get; set; } = null!;
    public List<AbilityAttributeModifier> AttributeModifiers { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Essence, EssenceDto>();
    }
}