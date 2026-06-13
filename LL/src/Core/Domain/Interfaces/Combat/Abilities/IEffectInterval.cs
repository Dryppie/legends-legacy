namespace Domain.Interfaces.Combat.Abilities;
public interface IEffectInterval
{
    bool ShouldTrigger();
    void Update();
    IEffectInterval Clone();
}