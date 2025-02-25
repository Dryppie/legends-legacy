using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Abilities;
using Domain.Models.Abilities.ResourceCosts;

namespace Application.UseCases.Essences.Dtos;
public class AbilityDescriptionDto : IMapFrom<AbilityDefinition>
{
    public string Id { get; set; } = string.Empty; // Unique identifier
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cooldown { get; set; }
    public int Cost { get; set; }
    public ResourceType CostType { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<AbilityDefinition, AbilityDescriptionDto>();
    }
}
