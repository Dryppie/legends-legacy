using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Duration;
public class IndefiniteDuration : IEffectDuration
{
    public void DecrementDuration() { }
    public bool IsActive() => true;
    public void RenewDuration() { }
    public IEffectDuration Clone() => new IndefiniteDuration();
}