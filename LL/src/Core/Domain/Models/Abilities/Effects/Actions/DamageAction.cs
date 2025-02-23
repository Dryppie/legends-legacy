using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using System.Net.Mime;

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

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var isFlatAmount = context.Effect.Definition.IsFlatAmount;
        var attackOutcome = AttackOutcome.Hit;
        var damageAmount = Magnitude;

        if (!isFlatAmount) // If it isn't a flat amount, perform the necessary calculations for attack outcome and damage
        {
            attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForDamage(context.Actor, context.Target, context.Effect.Definition.EffectModifications);

            if (attackOutcome == AttackOutcome.Miss)
            {
                context.EventType = EventType.Miss;
                context.Details = $"{context.Actor?.Name!} missed the target.";
                // Log
                combatContext.LogEffectExecution(context);
                return;
            }

            // Potential damage to deal before calculating opponent's defenses
            damageAmount = combatContext.InteractionManager.CalculateDamageToDeal(context.Actor, context.Target, Magnitude, attackOutcome, ScalingAttribute!.Value, ScalingMultiplier);
        }

        // Damage opponent will receive
        var damageReceived = combatContext.InteractionManager.CalculateDamageReceived(context.Target, damageAmount, attackOutcome);

        context.AttackOutcome = attackOutcome;
        context.Magnitude = damageReceived;
        context.EventType = EventType.Damage;
        context.Details = context.Details
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name);

        if (attackOutcome.Equals(AttackOutcome.Crit))
            context.Details = context.Details.Replace("{Amount}", $"{context.Magnitude} critical");
        else
            context.Details = context.Details.Replace("{Amount}", context.Magnitude.ToString());

        var simpleCombatEntity = CreateSimpleCombatEntity(context.Target);
        combatContext.LogEffectExecution(context, simpleCombatEntity);
        
        combatContext.InteractionManager.ApplyDamage(context);

        ApplyLifeStealIfAny(context, combatContext, damageReceived);
    }

    private void ApplyLifeStealIfAny(EffectContext context, ICombatContext combatContext, int finalDamageDealt)
    {
        // If there's no life-steal or no damage dealt, skip
        if (_lifeStealPercentage <= 0f || finalDamageDealt <= 0)
            return;

        // Calculate how much life to steal
        int lifeStolen = (int)(finalDamageDealt * (_lifeStealPercentage / 100));

        if (lifeStolen <= 0)
            return;

        context.Actor = context.Actor;
        context.Target = context.Actor;
        context.EventType = EventType.Heal;
        context.Details = $"{context.Actor.Name} restored {lifeStolen} health through lifesteal.";

        // Apply the healing
        combatContext.InteractionManager.ApplyHealing(context);

        // Log the healing event
        var simpleCombatEntity = CreateSimpleCombatEntity(context.Target);
        combatContext.LogEffectExecution(context, simpleCombatEntity);

        // Trigger OnLifesteal
        combatContext.EffectManager.TriggerEffects(TriggerEvent.OnLifestealHeal, context.Actor, context.Actor, lifeStolen);
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

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}