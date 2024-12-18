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
    void ApplyDamage(Entity attacker, Entity target, float damage);
    void ApplyHealing(Entity healer, Entity target, float healing);
}