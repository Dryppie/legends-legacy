using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Intervals;
public class NoInterval : IEffectInterval
{
    public void Update() { }
    public bool ShouldTrigger() => false;
    public IEffectInterval Clone() => new NoInterval();
}