using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Timed;
public class TimedDuration : IEffectDuration
{
    private int durationRemaining;

    public TimedDuration(int duration)
    {
        durationRemaining = duration;
    }

    public void DecrementDuration() => durationRemaining--;
    public bool IsActive() => durationRemaining > 0;
    public IEffectDuration Clone() => new TimedDuration(durationRemaining);

}