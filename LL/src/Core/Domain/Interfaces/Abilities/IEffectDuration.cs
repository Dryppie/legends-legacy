namespace Domain.Interfaces.Abilities;
public interface IEffectDuration
{
    void DecrementDuration();
    bool IsActive();
    void RenewDuration();
    IEffectDuration Clone();
}