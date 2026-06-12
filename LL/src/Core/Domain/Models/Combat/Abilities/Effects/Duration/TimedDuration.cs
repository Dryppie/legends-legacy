using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Effects.Duration;
public class TimedDuration : IEffectDuration
{
    private int initialDuration;
    private int durationRemaining;
    /// <summary>
    /// Adds one tick so a duration applied during a combat tick lasts for the intended visible duration.
    /// </summary>
    /// <param name="duration"></param>
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