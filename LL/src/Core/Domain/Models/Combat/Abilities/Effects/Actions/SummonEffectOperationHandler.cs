using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Intervals;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Damages;
using Domain.Models.Entities.Creatures;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class SummonEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.Summon;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        if (string.IsNullOrWhiteSpace(action.SummonId))
            throw new InvalidOperationException("Summon requires a summon id.");

        var summonedCreature = SummonCreatureFactory.CreateCreature(action.SummonId);

        if (action.SummonDuration > 0)
        {
            var selfDestructEffectDefinition = new EffectDefinition(
                action: new CombatEffectAction { Operation = CombatEffectOperation.SelfDestruct },
                duration: new TimedDuration(action.SummonDuration + 1),
                condition: new NoCondition(),
                interval: new NoInterval(),
                usage: new UnlimitedUsage(),
                effectModifications: [],
                effectTags: [EffectTag.SummonExpiration]);
            combatContext.EffectManager.AddEffect(new(selfDestructEffectDefinition, summonedCreature, summonedCreature));
        }

        combatContext.EntityManager.AddEntityToOwnTeam(effect.Source, summonedCreature);
        effect.Target = summonedCreature;
        effect.EventType = EventType.Summon;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", summonedCreature.Name);

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(summonedCreature));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }
}
