using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Conditions;
// Effects default to this if they have no condition specified
public class NoCondition : IEffectCondition
{
    public bool IsSatisfied(EffectContext context) => true;
}
