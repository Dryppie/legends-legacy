using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Abilities;
using Domain.Models.Abilities.ResourceCosts;
using Domain.Models.Damages;

namespace Application.UseCases.Essences.Dtos;
public class AbilityDescriptionDto : IMapFrom<AbilityDefinition>
{
    public string Id { get; set; } = string.Empty; // Unique identifier
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cooldown { get; set; }
    public ResourceCost? Cost { get; set; }
    public IReadOnlyCollection<AttackType> AttackTypes { get; init; } = [];
    public IReadOnlyCollection<DamageType> DamageTypes { get; init; } = [];
    public IReadOnlyCollection<EffectTag> EffectTags { get; init; } = [];


    public void Mapping(Profile profile)
    {
        profile.CreateMap<AbilityDefinition, AbilityDescriptionDto>();
    }
}
