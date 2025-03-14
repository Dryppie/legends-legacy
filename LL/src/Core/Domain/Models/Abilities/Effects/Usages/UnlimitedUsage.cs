using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Usages;
public class UnlimitedUsage : IUsage
{
    public bool CanUse() => true;
    public void ConsumeUse() { }
    public void Recharge() { }
    public IUsage Clone() => new UnlimitedUsage();

    public void Reset() { }
}