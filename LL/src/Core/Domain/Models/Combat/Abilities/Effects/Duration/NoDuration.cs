using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Effects.Duration;
public class NoDuration : IEffectDuration
{
    public void DecrementDuration() { }
    public bool IsActive() => false;
    public void RenewDuration() { }
    public IEffectDuration Clone() => new NoDuration();
}
