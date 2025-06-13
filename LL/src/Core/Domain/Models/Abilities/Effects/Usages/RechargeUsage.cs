using Domain.Interfaces.Abilities;

namespace Domain.Models.Abilities.Effects.Usages;
public class RechargeUsage : IUsage
{
    private readonly int _initialUses;
    private int _remainingUses;
    private readonly int _rechargeInterval;
    private int _ticksUntilNextRecharge;

    public RechargeUsage(int initialUses, int rechargeInterval)
    {
        _initialUses = initialUses;
        _remainingUses = initialUses;
        _rechargeInterval = rechargeInterval;
        _ticksUntilNextRecharge = 0;
    }

    public bool CanUse() => _remainingUses > 0;
    public void ConsumeUse() => _remainingUses--;
    public void Recharge()
    {
        _ticksUntilNextRecharge++;
        if (_ticksUntilNextRecharge >= _rechargeInterval)
        {
            _remainingUses++;
            _ticksUntilNextRecharge = 0;
        }
    }
    // Clone with remaining uses to keep track during a single fight
    public IUsage Clone() => new RechargeUsage(_remainingUses, _rechargeInterval);
    // Reset makes sure to reset everything after a fight is over
    public void Reset() => _remainingUses = _initialUses;
}