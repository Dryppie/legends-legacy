using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Domain.Interfaces.Combat;
public interface ICombatInteractionManager
{
    AttackOutcome CalculateAttackOutcomeForDamage(CombatEntity actor, CombatEntity target, List<EffectModification> effectModifications);
    AttackOutcome CalculateAttackOutcomeForHealing(CombatEntity actor, CombatEntity target, List<EffectModification> effectModifications);
    int CalculateBasicAttackDamage(CombatEntity actor, CombatEntity target, float baseDamage);
    int CalculateDamageToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttackOutcome attackOutcome, AttributeType scalingAttribute, float scalingMultiplier);
    int CalculateHealingToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttackOutcome attackOutcome, AttributeType? scalingAttribute, float scalingMultiplier);
    DamageResult CalculateDamageBreakdown(CombatEntity target, float baseDamage, AttackOutcome outcome, DamageType damageType);
    int CalculateHealingReceived(CombatEntity target, float magnitude, AttackOutcome attackOutcome = AttackOutcome.Hit);
    void ApplyDamage(EffectContext context);
    void ApplyHealing(EffectContext context);
}