using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Damages;

namespace Application.UseCases.Abilities.Dtos;
public class AbilityDto : IMapFrom<AbilityDefinition>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AttackType? AttackType { get; set; }
    public DamageType? DamageType { get; set; }

    public List<EffectTag>? DamageTags { get; set; } = [];
    public AbilityType Type { get; set; } // Active or Passive
    public int Cooldown { get; set; }
    public int RemainingTimeUntilUse { get; set; }
    public int Cost { get; set; } // e.g., mana cost
    public List<EffectType> EffectTypes { get; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<AbilityDefinition, AbilityDto>()
            .ForMember(dto => dto.EffectTypes, opt => opt.MapFrom(src => src.Effects.Select(e => e.EffectType).ToList()));

            // TODO: THIS SHOULD BE POSSIBLE TO DELETE
            //.ForMember(dto => dto.AttackType, opt => opt.MapFrom(src => src.Effects
            //   .Where(e => e.EffectType == EffectType.Damage)
            //   .Select(e => (e.Action as DamageAction).AttackType)
            //   .FirstOrDefault()
            //))

            //// Similarly, map DamageType from the first damage effect
            //.ForMember(dto => dto.DamageType, opt => opt.MapFrom(src => src.Effects
            //    .Where(e => e.EffectType == EffectType.Damage)
            //    .Select(e => (e.Action as DamageAction).DamageType)
            //    .FirstOrDefault()
            //))

            //// For DamageTags, you might want to combine tags from all damage effects:
            //.ForMember(dto => dto.DamageTags, opt => opt.MapFrom(src => src.Effects
            //    .Where(e => e.EffectType == EffectType.Damage)
            //    .SelectMany(e => (e.Action as DamageAction).DamageTags ?? new List<EffectTag>())
            //    .Distinct()
            //    .ToList()
            //));
    }
}