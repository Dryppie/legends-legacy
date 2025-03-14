using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Usages;
public class LimitedUsage : IUsage
{
    private readonly int _initialUses;
    private int _remainingUses;
    public LimitedUsage(int initialUses)
    {
        _initialUses = initialUses;
        _remainingUses = initialUses;
    }

    public bool CanUse() => _remainingUses > 0;
    public void ConsumeUse() => _remainingUses--;
    public void Recharge() { }
    public IUsage Clone() => new LimitedUsage(_remainingUses);

    public void Reset() => _remainingUses = _initialUses;
}