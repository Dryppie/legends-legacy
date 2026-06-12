namespace Domain.Interfaces.Combat.Abilities;
public interface IEffectDuration
{
    void DecrementDuration();
    bool IsActive();
    void RenewDuration();
    IEffectDuration Clone();
}