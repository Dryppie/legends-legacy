using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Interval;
public class NoInterval : IEffectInterval
{
    public void Update() { }
    public bool ShouldTrigger() => false;
    public IEffectInterval Clone() => new NoInterval();
}