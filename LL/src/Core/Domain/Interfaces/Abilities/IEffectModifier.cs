using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces.Abilities;
public interface IEffectModifier
{
    void ApplyModifier(EffectContext context);
}
