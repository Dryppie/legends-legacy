using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Interfaces.Combat.Abilities;
public interface IEffectModifier
{
    void ApplyModifier(EffectContext context);
}
