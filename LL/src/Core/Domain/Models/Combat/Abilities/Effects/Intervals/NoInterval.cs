using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Effects.Intervals;
public class NoInterval : IEffectInterval
{
    public void Update() { }
    public bool ShouldTrigger() => false;
    public IEffectInterval Clone() => new NoInterval();
}