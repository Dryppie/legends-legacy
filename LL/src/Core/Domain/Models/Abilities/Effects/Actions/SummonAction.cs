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

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        // Create the summoned entity based on the provided type
        CombatEntity summonedCreature = SummonCreatureFactory.CreateCreature(_summonEntityType);

        // If the summoned entity has a limited duration, add a self-destruct effect
        if (Magnitude > 0)
        {
            var selfDestructAction = new SelfDestructAction(_combatContext!);
            var duration = new TimedDuration(Magnitude + 1);
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
            var selfDestructEffect = new EffectInstance(selfDestructEffectDefinition, summonedCreature, summonedCreature);

            combatContext.EffectManager.AddEffect(selfDestructEffect);
        }

        // Add the summoned entity to the caster's team
        combatContext.EntityManager.AddEntityToOwnTeam(effect.Source, summonedCreature);

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

        effect.Target = summonedCreature;
        effect.EventType = EventType.Summon;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", summonedCreature.Name);

        combatContext.LogEffectExecution(effect, simpleCombatEntity);
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
        // Do nothing
    }
}