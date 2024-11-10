using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Interval;
public class Interval : IEffectInterval
{
    private int interval;
    private int ticksUntilNextTrigger;

    public Interval(int interval)
    {
        this.interval = interval;
        ticksUntilNextTrigger = interval;
    }

    public bool ShouldTrigger()
    {
        return ticksUntilNextTrigger <= 0;
    }

    public void Update()
    {
        ticksUntilNextTrigger--;

        if (ticksUntilNextTrigger < 0)
            ticksUntilNextTrigger = interval;
    }
    public IEffectInterval Clone() => new Interval(interval);
}