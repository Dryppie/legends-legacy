using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces;
public interface IEffectAction
{
    int Magnitude { get; }
    void Execute(EffectContext context, Action<EffectContext> action);
    void OnExpireExecute(EffectContext context, Action<EffectContext> action);
}