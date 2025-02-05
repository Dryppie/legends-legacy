using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Duration;
public class NoDuration : IEffectDuration
{
    public void DecrementDuration() { }
    public bool IsActive() => false; 
    public IEffectDuration Clone() => new NoDuration();
}
