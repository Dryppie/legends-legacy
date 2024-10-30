namespace Domain.Interfaces;
public interface IEffectInterval
{
    bool ShouldTrigger();
    void Update();
    IEffectInterval Clone();
}