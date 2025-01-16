using Domain.Models.Abilities.Effects;
using Domain.Models.Combat;

namespace Domain.Interfaces.Combat;
public interface ICombatInteractionManager
{
    AttackOutcome CalculateAttackOutcomeForDamage(CombatEntity actor, CombatEntity target);
    AttackOutcome CalculateAttackOutcomeForHealing(CombatEntity actor, CombatEntity target);
    int CalculateBasicAttackDamage(CombatEntity attacker, float baseDamage);
    int CalculateDamageToDeal(CombatEntity attacker, CombatEntity defender, float magnitude);
    int CalculateHealingToDeal(CombatEntity healer, CombatEntity receiver, float magnitude);
    int CalculateDamageReceived(CombatEntity defender, float magnitude, AttackOutcome attackOutcome = AttackOutcome.Hit);
    int CalculateHealingReceived(CombatEntity receiver, float magnitude, AttackOutcome attackOutcome = AttackOutcome.Hit);
    void ApplyDamage(EffectContext context);
    void ApplyHealing(EffectContext context);
}