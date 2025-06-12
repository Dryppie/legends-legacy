using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Duration;
public class TimedDuration : IEffectDuration
{
    private int initialDuration;
    private int durationRemaining;
    /// <summary>
    /// When timedDuration is loaded from the EssenceJsonReader, we add +1 to the duration so it counteracts the tick that happens while this is applied to a target
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