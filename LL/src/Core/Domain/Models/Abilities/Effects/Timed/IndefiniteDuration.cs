using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Timed;
public class IndefiniteDuration : IEffectDuration
{
    public void DecrementDuration() { }

    public bool IsActive() => true;

    public IEffectDuration Clone() => new IndefiniteDuration();
}