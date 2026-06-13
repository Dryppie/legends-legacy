using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Effects.Intervals;
public class Interval : IEffectInterval
{
    private int interval;
    private int ticksUntilNextTrigger;

    public Interval(int interval)
    {
        this.interval = interval;
        ticksUntilNextTrigger = interval + 1; // +1 to counteract the first tick which is calculated right when this is applied
    }

    public bool ShouldTrigger()
    {
        return ticksUntilNextTrigger <= 0;
    }

    public void Update()
    {
        ticksUntilNextTrigger--;

        if (ticksUntilNextTrigger < 0)
            ticksUntilNextTrigger = interval - 1; // -1 to counteract Interval + 1, which increments each interval by 1 tick without this
    }
    public IEffectInterval Clone() => new Interval(interval);
}