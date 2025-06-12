using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.ResourceCosts;
using Domain.Models.Attributes;
using Domain.Models.Combat;

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

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        //Attack outcome


        effect = _resourceType switch
        {
            ResourceType.Mana => HandleMana(effect, combatContext),
            ResourceType.Health => HandleHealth(effect, combatContext),
            ResourceType.Barrier => HandleBarrier(effect, combatContext),
            _ => effect
        };

        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Amount}", effect.Magnitude.ToString());

        var simpleCombatEntity = CreateSimpleCombatEntity(effect.Target);
        combatContext.LogEffectExecution(effect, simpleCombatEntity);

        if (_resourceType == ResourceType.Health)
        {
            combatContext.InteractionManager.ApplyHealing(effect);
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
        var attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForHealing(context.Source, context.Target, [/*context.Effect.Definition.EffectModifications*/]);

        // Potential healing
        var healingAmount = combatContext.InteractionManager.CalculateHealingToDeal(context.Source, context.Target, Magnitude, attackOutcome, ScalingAttribute, ScalingMultiplier);

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

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
        // Do nothing
    }
}