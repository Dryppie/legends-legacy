using Domain.Models.Abilities.Effects;
using Domain.Models.Combat;

namespace Domain.Interfaces.Combat;
public interface ICombatInteractionManager
{
    AttackOutcome CalculateAttackOutcomeForDamage(CombatEntity actor, CombatEntity target);
    AttackOutcome CalculateAttackOutcomeForHealing(CombatEntity actor, CombatEntity target);
    int CalculateBasicAttackDamage(CombatEntity actor, CombatEntity target, float baseDamage);
    int CalculateDamageToDeal(CombatEntity actor, CombatEntity target, float magnitude);
    int CalculateHealingToDeal(CombatEntity actor, CombatEntity target, float magnitude);
    int CalculateDamageReceived(CombatEntity target, float magnitude, AttackOutcome attackOutcome = AttackOutcome.Hit);
    int CalculateHealingReceived(CombatEntity target, float magnitude, AttackOutcome attackOutcome = AttackOutcome.Hit);
    void ApplyDamage(EffectContext context);
    void ApplyHealing(EffectContext context);
}