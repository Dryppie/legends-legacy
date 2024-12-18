using Domain.Models.Abilities.Effects;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Interfaces.Combat;
public interface ICombatInteractionManager
{
    int CalculateBasicAttackDamage(Entity attacker, float baseDamage);
    int CalculateDamageToDeal(Entity attacker, Entity defender, float magnitude);
    int CalculateDamageReceived(Entity defender, float magnitude, AttackOutcome attackOutcome);
    int CalculateHealingToDo(Entity healer, Entity target, float baseHealing);
    int CalculateHealingReceived(Entity healer, Entity target, float baseHealing);
    void ApplyDamage(EffectContext context);
    void ApplyHealing(Entity healer, Entity target, float healing);
}