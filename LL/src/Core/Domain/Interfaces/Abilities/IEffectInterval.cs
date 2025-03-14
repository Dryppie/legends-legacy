namespace Domain.Interfaces.Abilities;
public interface IEffectInterval
{
    bool ShouldTrigger();
    void Update();
    IEffectInterval Clone();
}