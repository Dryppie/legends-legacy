using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.ResourceCosts;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using System.Threading;

namespace Domain.Models.Abilities.Effects.Actions;
public class ResourceRestoreAction : IEffectAction
{
    private readonly ResourceType _resourceType;
    private readonly int _restoreAmount;
    public int Magnitude => _restoreAmount;
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingMultiplier { get; set; }

    public ResourceRestoreAction(int restoreAmount, ResourceType resourceType, AttributeType? scalingAttribute, float scalingMultiplier)
    {
        _restoreAmount = restoreAmount;
        _resourceType = resourceType;
        ScalingAttribute = scalingAttribute;
        ScalingMultiplier = scalingMultiplier;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        // Attack outcome
        
        context = _resourceType switch
        {
            ResourceType.Mana => HandleMana(context, combatContext),
            ResourceType.Health => HandleHealth(context, combatContext),
            ResourceType.Barrier => HandleBarrier(context, combatContext),
            _ => context
        };

        context.Details = context.Details   
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", context.Magnitude.ToString());

        var simpleCombatEntity = CreateSimpleCombatEntity(context.Target);
        combatContext.LogEffectExecution(context, simpleCombatEntity);

        if (_resourceType == ResourceType.Health)
        {
            combatContext.InteractionManager.ApplyHealing(context);
        }
    }

    private EffectContext HandleBarrier(EffectContext context, ICombatContext combatContext)
    {
        var restoreAmount = Magnitude;

        if (context.Target.CombatAttributes.TryGetValue(AttributeType.Barrier, out var barrier))
        {
            barrier += restoreAmount;
            context.Target.CombatAttributes[AttributeType.Barrier] = barrier;  // <-- Write it back
        }

        context.AttackOutcome = AttackOutcome.Hit;
        context.Magnitude = restoreAmount;
        context.EventType = EventType.RestoreBarrier;

        return context;
    }

    private EffectContext HandleHealth(EffectContext context, ICombatContext combatContext)
    {
        var attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForHealing(context.Actor, context.Target, context.Effect.Definition.EffectModifications);

        // Potential healing
        var isFlatAmount = context.Effect.Definition.IsFlatAmount;
        var healingAmount = isFlatAmount
                            ? Magnitude
                            : combatContext.InteractionManager.CalculateHealingToDeal(context.Actor, context.Target, Magnitude, attackOutcome, ScalingAttribute, ScalingMultiplier);

        // Healing target will receive
        var healingReceived = combatContext.InteractionManager.CalculateHealingReceived(context.Target, healingAmount, attackOutcome);

        context.AttackOutcome = attackOutcome;
        context.Magnitude = healingReceived;
        context.EventType = EventType.Heal;

        return context;
    }

    private EffectContext HandleMana(EffectContext context, ICombatContext combatContext)
    {
        var restoreAmount = Magnitude;

        if (context.Target.CombatAttributes.TryGetValue(AttributeType.Mana, out var mana))
        {
            mana += restoreAmount;
            if (context.Target.CombatAttributes.TryGetValue(AttributeType.MaxMana, out var maxMana) && mana > maxMana)
            {
                mana = maxMana;
            }
            context.Target.CombatAttributes[AttributeType.Mana] = mana;
        }

        context.AttackOutcome = AttackOutcome.Hit;
        context.Magnitude = restoreAmount;
        context.EventType = EventType.RestoreMana;

        return context;
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