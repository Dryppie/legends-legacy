using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Usages;
public class UnlimitedUsage : IEffectUsage
{
    public bool CanUse() => true;
    public void ConsumeUse() { }
    public void Recharge() { }
    public IEffectUsage Clone() => new UnlimitedUsage();
}