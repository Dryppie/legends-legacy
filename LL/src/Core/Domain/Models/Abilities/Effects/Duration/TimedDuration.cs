using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Duration;
public class TimedDuration : IEffectDuration
{
    private int initialDuration;
    private int durationRemaining;

    public TimedDuration(int duration)
    {
        initialDuration = duration;
        durationRemaining = initialDuration;
    }

    public void DecrementDuration() => durationRemaining--;
    public bool IsActive() => durationRemaining > 0;
    public void RenewDuration() => durationRemaining = initialDuration;
    public IEffectDuration Clone() => new TimedDuration(durationRemaining);

}