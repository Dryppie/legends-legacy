using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces;
public interface IEffectModifier
{
    void ApplyModifier(EffectContext context);
}
