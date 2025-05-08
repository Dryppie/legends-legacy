using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.Duration;
using Domain.Models.Abilities.Effects.Intervals;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.Effects.Usages;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;
using Domain.Models.Entities.Creatures;

namespace Domain.Models.Abilities.Effects.Actions;
public class SummonAction : IEffectAction
{
    private readonly string _summonEntityType; // The type or identifier of the entity to summon
    private readonly int _duration;
    private ICombatContext? _combatContext;
    public int Magnitude => _duration;

    public SummonAction(string summonEntityType, int duration)
    {
        _summonEntityType = summonEntityType;
        _duration = duration;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        // Create the summoned entity based on the provided type
        CombatEntity summonedCreature = SummonCreatureFactory.CreateCreature(_summonEntityType);

        // If the summoned entity has a limited duration, add a self-destruct effect
        if (Magnitude > 0)
        {
            var selfDestructAction = new SelfDestructAction(_combatContext!);
            var duration = new TimedDuration(Magnitude);
            var condition = new NoCondition();
            var interval = new NoInterval();
            var usage = new UnlimitedUsage();
            var selfDestructEffectDefinition = new EffectDefinition(
                action: selfDestructAction,
                duration: duration,
                condition: condition,
                interval: interval,
                usage: usage,
                effectModifications: [],
                trigger: TriggerEvent.OnTickInterval,
                effectTags: [EffectTag.SummonExpiration]
            );
            var selfDestructEffect = new Effect()
            {
                Definition = selfDestructEffectDefinition,
                Caster = context.Actor!,
                Owner = summonedCreature
            };
            combatContext.EffectManager.AddEffect(context.Actor!, summonedCreature, selfDestructEffect);
        }

        // Add the summoned entity to the caster's team
        combatContext.EntityManager.AddEntityToOwnTeam(context.Actor!, summonedCreature);

        var simpleCombatEntity = new SimpleCombatEntity()
        {
            Id = summonedCreature.Id,
            Name = summonedCreature.Name,
            MaxHealth = summonedCreature.GetAttributeValue(AttributeType.MaxHealth),
            Health = summonedCreature.GetAttributeValue(AttributeType.Health),
            MaxMana = summonedCreature.GetAttributeValue(AttributeType.MaxMana),
            Mana = summonedCreature.GetAttributeValue(AttributeType.Mana),
            Barrier = summonedCreature.GetAttributeValue(AttributeType.Barrier),
            ImagePath = summonedCreature.ImagePath
        };

        context.Target = summonedCreature;
        context.EventType = EventType.Summon;
        context.Details = context.Details
            .Replace("{Actor}", context.Actor!.Name)
            .Replace("{Target}", summonedCreature.Name);

        combatContext.LogEffectExecution(context, simpleCombatEntity);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}