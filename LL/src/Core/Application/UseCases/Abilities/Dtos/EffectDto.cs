using Application.Common.Mappings;
using AutoMapper;
using Domain.Interfaces;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;

namespace Application.UseCases.Abilities.Dtos;
public class EffectDto : IMapFrom<EffectDefinition>
{
    public IEffectAction Action { get; }
    public IEffectDuration Duration { get; }
    public IEffectInterval Interval { get; }
    public ICondition Condition { get; }
    public IUsage Usage { get; }
    public Targeting Targeting { get; }
    public TriggerEvent Trigger { get; }
    public int Chance { get; }
    public EffectType EffectType { get; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EffectDefinition, EffectDto>();
    }
}