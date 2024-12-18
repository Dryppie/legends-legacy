using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.Timed;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;

namespace Domain.Models.Abilities.Effects.Actions;
public class SummonAction : IEffectAction
{
    private Entity? _caster;
    private readonly string _summonEntityType; // The type or identifier of the entity to summon
    private readonly int _duration;
    private ICombatContext? _combatContext;
    public int Magnitude => _duration;

    public SummonAction(string summonEntityType, int duration)
    {
        _summonEntityType = summonEntityType;
        _duration = duration;
    }

    public void SetContext(Entity caster, ICombatContext combatContext)
    {
        _caster = caster;
        _combatContext = combatContext;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        // Create the summoned entity based on the provided type
        Entity summonedCreature = SummonCreatureFactory.CreateCreature(_summonEntityType);

        // If the summoned entity has a limited duration, add a self-destruct effect
        if (Magnitude > 0)
        {
            var selfDestructAction = new SelfDestructAction(_combatContext!);
            var duration = new TimedDuration(Magnitude);
            var condition = new NoCondition();
            var selfDestructEffect = new Effect(
                action: selfDestructAction,
                duration: duration,
                condition: condition,
                caster: context.Owner,
                trigger: TriggerEvent.OnTickInterval
            );
            combatContext.EffectManager.AddEffect(summonedCreature, selfDestructEffect);
        }

        // Add the summoned entity to the caster's team
        combatContext.EntityManager.AddEntityToOwnTeam(context.Owner!, summonedCreature);

        context.Target = summonedCreature;
        context.EventType = EventType.Summon;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", summonedCreature.Name);

        combatContext.LogEffectExecution(context);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}