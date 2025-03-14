using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Conditions;
// Effects default to this if they have no condition specified
public class NoCondition : ICondition
{
    public bool IsSatisfied(EffectContext context) => true;
    public ICondition Clone() => new NoCondition();
    public void PerformCondition(EffectContext context) {}
}
