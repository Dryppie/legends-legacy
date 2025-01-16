using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Usages;
public class LimitedUsage : IEffectUsage
{
    private int _remainingUses;
    public LimitedUsage(int maxUses) => _remainingUses = maxUses;

    public bool CanUse() => _remainingUses > 0;
    public void ConsumeUse() => _remainingUses--;
    public void Recharge() { }
    public IEffectUsage Clone() => new LimitedUsage(_remainingUses);
}