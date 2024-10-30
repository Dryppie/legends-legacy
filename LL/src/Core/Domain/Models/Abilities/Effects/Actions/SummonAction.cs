using Domain.Interfaces;
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

    public void Execute(EffectContext context, Action<EffectContext> action)
    {
        // Create the summoned entity based on the provided type
        Entity summonedCreature = SummonCreatureFactory.CreateCreature(_summonEntityType);

        // If the summoned entity has a limited duration, add a self-destruct effect
        if (Magnitude > 0)
        {
            var selfDestructAction = new SelfDestructAction(_combatContext);
            var duration = new TimedDuration(Magnitude);
            var selfDestructEffect = new Effect(
                action: selfDestructAction,
                duration: duration,
                caster: context.Owner,
                trigger: TriggerEvent.OnTickInterval
            );
            summonedCreature.AddEffect(selfDestructEffect);
        }

        // Add the summoned entity to the caster's team
        _combatContext.AddEntityToTeam(_caster, summonedCreature);

        context.Target = summonedCreature;
        context.EffectType = EventType.Summon;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", summonedCreature.Name);

        action.Invoke(context);
    }

    public void OnExpireExecute(EffectContext context, Action<EffectContext> action)
    {
        // Do nothing
    }
}