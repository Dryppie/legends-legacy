using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces;
public interface IEffectCondition
{
    bool IsSatisfied(EffectContext context);
}