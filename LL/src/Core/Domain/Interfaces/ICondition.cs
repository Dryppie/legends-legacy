using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces;
public interface ICondition
{
    bool IsSatisfied(EffectContext context);
    void PerformCondition(EffectContext context);
    ICondition Clone();
}