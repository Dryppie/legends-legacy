using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Domain.Models.Abilities.Effects.Actions;
public class DamageAction : IEffectAction
{
    private readonly int _damageAmount;
    private readonly float _lifeStealPercentage;
    public int Magnitude => _damageAmount;
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingMultiplier { get; set; }

    public DamageAction(int damageAmount, AttributeType? scalingAttribute, float scalingMultiplier, float lifeStealPercentage = 0f)
    {
        _damageAmount = damageAmount;
        ScalingAttribute = scalingAttribute;
        ScalingMultiplier = scalingMultiplier;
        _lifeStealPercentage = lifeStealPercentage;
    }

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        var attackOutcome = AttackOutcome.Miss;
        var damageAmount = Magnitude;
        var eventType = effect.AttackType == AttackType.DamageOverTime
            ? EventType.DamageOverTime
            : EventType.Damage;
        if (eventType != EventType.DamageOverTime)
        {
            attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForDamage(effect.Source, effect.Target, effect.EffectModifications);

            if (attackOutcome == AttackOutcome.Miss)
            {
                effect.EventType = EventType.Miss;
                effect.Details = $"{effect.Source.Name} missed the target.";
                //// Log
                combatContext.LogEffectExecution(effect);

                combatContext.EventBus.Publish(new CombatEvent
                {
                    // When someone dodges, they're the source of the event, as they're the ones dodging the attack
                    // The target is the whoever attacked. This will result in being able to trigger effects on the attacker
                    Type = TriggerEvent.OnDodge,
                    Source = effect.Target,
                    Target = effect.Source,
                    CurrentTime = combatContext.CurrentTime
                });
                return;
            }
            // Potential damage to deal before calculating opponent's defenses
            damageAmount = combatContext.InteractionManager.CalculateDamageToDeal(effect.Source, effect.Target, Magnitude, attackOutcome, ScalingAttribute, ScalingMultiplier);
        }

        //// Damage opponent will receive
        var damageResult = combatContext.InteractionManager.CalculateDamageBreakdown(effect.Target, damageAmount, attackOutcome, effect.DamageType);

        effect.AttackOutcome = attackOutcome;
        effect.Magnitude = damageResult.HealthDamage; // Only set HealthDamage, as that's what we'll use to deduct from Health. TotalDamage is only for the Log
        effect.EventType = eventType;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name);

        if (damageResult.IsCrit)
            effect.Details = effect.Details.Replace("{Amount}", $"{damageResult.TotalDamage} critical");
        else if (attackOutcome == AttackOutcome.Parry)
            effect.Details = effect.Details.Replace("{Amount}", $"{damageResult.TotalDamage} parried");
        else if (attackOutcome == AttackOutcome.Block)
            effect.Details = effect.Details.Replace("{Amount}", $"{damageResult.TotalDamage} blocked");
        else
            effect.Details = effect.Details.Replace("{Amount}", damageResult.TotalDamage.ToString());

        var simpleCombatEntity = CreateSimpleCombatEntity(effect.Target);
        simpleCombatEntity.Health = Math.Max(0, simpleCombatEntity.Health - damageResult.HealthDamage);
        //var context = new EffectContext(source, target, eventType, damageResult.HealthDamage, details);
        combatContext.LogEffectExecution(effect, simpleCombatEntity);

        combatContext.InteractionManager.ApplyDamage(effect.Source, effect.Target, damageResult.HealthDamage, effect.AttackType);

        ApplyLifeStealIfAny(effect, combatContext, damageResult.HealthDamage);
    }

    private void ApplyLifeStealIfAny(EffectContext effect, ICombatContext combatContext, int finalDamageDealt)
    {
        // If there's no life-steal or no damage dealt, skip
        if (_lifeStealPercentage <= 0f || finalDamageDealt <= 0)
            return;

        // Calculate how much life to steal
        int lifeStolen = (int)(finalDamageDealt * (_lifeStealPercentage / 100));

        if (lifeStolen <= 0)
            return;

        effect.Magnitude += lifeStolen;
        effect.Target = effect.Source; // The source is the one who gets healed
        effect.EventType = EventType.Heal;
        effect.Details = $"{effect.Source.Name} restored {lifeStolen} health through lifesteal.";

        // Apply the healing
        combatContext.InteractionManager.ApplyHealing(effect);

        // Log the healing event
        var simpleCombatEntity = CreateSimpleCombatEntity(effect.Source);
        simpleCombatEntity.Health += effect.Magnitude;
        simpleCombatEntity.Health = Math.Min(simpleCombatEntity.MaxHealth, simpleCombatEntity.Health + effect.Magnitude);

        // Trigger OnLifesteal
        combatContext.LogEffectExecution(effect, simpleCombatEntity);

        combatContext.EventBus.Publish(new CombatEvent
        {
            Type = TriggerEvent.OnLifestealHeal,
            Source = effect.Source,
        });
    }

    private SimpleCombatEntity CreateSimpleCombatEntity(CombatEntity target)
    {
        return new SimpleCombatEntity()
        {
            Id = target.Id,
            MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
            Health = target.GetAttributeValue(AttributeType.Health),
            MaxMana = target.GetAttributeValue(AttributeType.MaxMana),
            Mana = target.GetAttributeValue(AttributeType.Mana),
            Barrier = target.GetAttributeValue(AttributeType.Barrier)
        };
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
        // Do nothing
    }
}