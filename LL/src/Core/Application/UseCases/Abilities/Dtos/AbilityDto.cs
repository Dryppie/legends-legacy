using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;

namespace Application.UseCases.Abilities.Dtos;
public class AbilityDto : IMapFrom<Ability>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AbilityType Type { get; set; } // Active or Passive
    public int Cooldown { get; set; }
    public int RemainingTimeUntilUse { get; set; }
    public int Cost { get; set; } // e.g., mana cost
    public List<Effect> Effects { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Ability, AbilityDto>();
    }
}