namespace Domain.Interfaces;
public interface IEffectDuration
{
    void DecrementDuration();
    bool IsActive();
    IEffectDuration Clone();
}