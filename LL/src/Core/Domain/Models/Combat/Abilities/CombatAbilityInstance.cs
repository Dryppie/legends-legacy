namespace Domain.Models.Combat.Abilities;
public class CombatAbilityInstance
{
    public CombatAbilityDefinition Definition { get; } = null!;
    public int RemainingTimeUntilUse { get; set; }
    public CombatAbilityInstance(CombatAbilityDefinition definition)
    {
        Definition = definition.Clone();
        SetCooldown();
    }

    public void SetCooldown()
    {
        RemainingTimeUntilUse = Definition.Cooldown;
    }
}
