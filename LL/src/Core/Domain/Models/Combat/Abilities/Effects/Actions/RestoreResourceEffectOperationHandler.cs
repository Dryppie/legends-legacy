using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.ResourceCosts;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class RestoreResourceEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.RestoreResource;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        if (action.Resource == ResourceType.Barrier)
        {
            var barrier = effect.Target.CombatAttributes.GetValueOrDefault(AttributeType.BlockEffectiveness);
            effect.Target.CombatAttributes[AttributeType.BlockEffectiveness] = barrier + action.Magnitude;
            effect.AttackOutcome = AttackOutcome.Hit;
            effect.Magnitude = action.Magnitude;
            effect.EventType = EventType.RestoreBarrier;
        }
        else
        {
            var attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForHealing(effect.Source, effect.Target, []);
            var healingAmount = combatContext.InteractionManager.CalculateHealingToDeal(
                effect.Source,
                effect.Target,
                action.Magnitude,
                attackOutcome,
                action.ScalingAttribute,
                action.ScalingMultiplier);
            effect.AttackOutcome = attackOutcome;
            effect.Magnitude = combatContext.InteractionManager.CalculateHealingReceived(effect.Target, healingAmount, attackOutcome);
            effect.EventType = EventType.Heal;
        }

        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Amount}", effect.Magnitude.ToString());

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target));

        if (action.Resource == ResourceType.Health)
            combatContext.InteractionManager.ApplyHealing(effect);
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }
}
