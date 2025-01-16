using Domain.Interfaces;

namespace Domain.Models.Abilities.Effects.Usages;
public class RechargeUsage : IEffectUsage
{
    private int _remainingUses;
    private readonly int _rechargeInterval;
    private int _ticksUntilNextRecharge;

    public RechargeUsage(int maxUses, int rechargeInterval)
    {
        _remainingUses = maxUses;
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
    public IEffectUsage Clone() => new RechargeUsage(_remainingUses, _rechargeInterval);
}