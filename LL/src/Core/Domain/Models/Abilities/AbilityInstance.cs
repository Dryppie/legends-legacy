using Domain.Models.Abilities.Triggers;

namespace Domain.Models.Abilities;
public class AbilityInstance
{
    public AbilityDefinition Definition { get; } = null!;
    public int RemainingTimeUntilUse { get; set; }
    public AbilityInstance(AbilityDefinition definition)
    {
        Definition = definition.Clone();
        SetCooldown();
    }

    public void SetCooldown()
    {
        RemainingTimeUntilUse = Definition.Cooldown;
    }

    public IEnumerable<Trigger> GetTriggers() => Definition.Triggers;
}
