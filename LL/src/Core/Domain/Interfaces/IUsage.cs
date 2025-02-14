namespace Domain.Interfaces;
public interface IUsage
{
    bool CanUse();
    void ConsumeUse();
    void Recharge();
    IUsage Clone();
    void Reset();
}