using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces.Abilities;
public interface ICondition
{
    bool IsSatisfied(EffectContext context);
    void PerformCondition(EffectContext context);
    ICondition Clone();
}