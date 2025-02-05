namespace Domain.Interfaces;
public interface IEffectDuration
{
    void DecrementDuration();
    bool IsActive();
    void RenewDuration();
    IEffectDuration Clone();
}