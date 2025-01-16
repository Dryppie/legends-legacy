namespace Domain.Interfaces;
public interface IEffectUsage
{
    bool CanUse();
    void ConsumeUse();
    void Recharge();
    IEffectUsage Clone();
}