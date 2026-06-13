using Domain.Interfaces.Combat.Abilities;

namespace Domain.Models.Combat.Abilities.Effects.Usages;
public class UnlimitedUsage : IUsage
{
    public bool CanUse() => true;
    public void ConsumeUse() { }
    public void Recharge() { }
    public IUsage Clone() => new UnlimitedUsage();

    public void Reset() { }
}