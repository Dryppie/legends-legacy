using Domain.Interfaces;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Services.LL.Combat;
public class CombatSimulation : ICombatContext
{
    private List<Entity> _originalPlayerTeam;
    private List<Entity> _originalEnemyTeam;
    private List<Entity> _playerTeam;
    private List<Entity> _enemyTeam;
    private List<CombatEvent> _eventLog;
    private const int MaxSimulationTime = 6000; // Max duration in milliseconds
    private const int TimeStep = 1; // Time step in milliseconds
    private int CurrentTime = 0;

    public CombatSimulation(List<Entity> playerTeam, List<Entity> enemyTeam)
    {
        _originalPlayerTeam = playerTeam;
        _originalEnemyTeam = enemyTeam;

        // Initialize teams
        _playerTeam = new List<Entity>(_originalPlayerTeam);
        _enemyTeam = new List<Entity>(_originalEnemyTeam);

        _eventLog = [];

        Effect.OnEffectExecuted += LogEffectExecution;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="simulated">Whether this is being simulated for testing purposes</param>
    /// <returns></returns>
    public async Task<CombatResult> RunSimulation(bool simulated = false)
    {
        ActivatePassiveAbilities([.. _playerTeam, .. _enemyTeam]);

        while (CurrentTime < MaxSimulationTime && _playerTeam.Any(c => c.IsAlive) && _enemyTeam.Any(c => c.IsAlive))
        {
            // Process actions for both teams
            ProcessTeamActions([.. _playerTeam], [.. _enemyTeam], CurrentTime);
            ProcessTeamActions([.. _enemyTeam], [.. _playerTeam], CurrentTime);

            // Advance time
            CurrentTime += TimeStep;
        }

        // Determine outcome
        var outcome = DetermineOutcome();
        if (simulated)
        {
            foreach (var log in _eventLog)
            {
                Console.WriteLine($"Time: {log.Timestamp} - " + log.Details);
            }
            Console.WriteLine(outcome);
        }

        Effect.OnEffectExecuted -= LogEffectExecution;

        return new CombatResult
        {
            EventLog = _eventLog,
            Outcome = outcome,
            Duration = CurrentTime // CurrentTime 10 equals 1 second
        };
    }

    private void ActivatePassiveAbilities(List<Entity> entities)
    {
        foreach (var entity in entities)
        {
            foreach (var ability in entity.Abilities)
            {
                ability.RemainingTimeUntilUse = ability.Cooldown;
                if (ability.Type.Equals(AbilityType.Passive))
                {
                    foreach (var effect in ability.Effects)
                    {
                        entity.ActiveEffects.Add(effect);
                    }
                }
            }
        }
    }

    private void ProcessTeamActions(List<Entity> actingTeam, List<Entity> opposingTeam, int currentTime)
    {
        foreach (var entity in actingTeam.Where(c => c.IsAlive))
        {
            if (!entity.CanAct())
            {
                //_eventLog.Add(new CombatEvent()
                //{
                //    ActorId = entity.Id,
                //    Action = EffectType.Stunned,
                //    Timestamp = currentTime,
                //    Details = $"{entity.Name} is stunned, and can not act."
                //});
                entity.IncrementStep();

                continue;
            }

            if (entity.NextBasicAttackIn <= 0)
            {
                // Perform basic
                //var weapon = entity.Equipment.FirstOrDefault(e => e.Type.Equals(ItemType.Weapon));
                //var target = SelectTarget(weapon.Targeting);
                var target = SelectTarget(opposingTeam); // Replace this with the two previous lines
                if (target == null) continue;
                if (false/*weapon.Heal*/)
                {
                    PerformHealing(entity, [target], currentTime);
                }
                else
                {
                    PerformDamage(entity, new List<Entity>() { target }, currentTime);
                }

                entity.NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the Entity class
            }

            entity.NextBasicAttackIn -= (int)entity.CombatAttributes[AttributeType.BasicAttackSpeed];

            foreach (var ability in entity.Abilities.Where(a => a.Type.Equals(AbilityType.Active)))
            {
                if (ability.RemainingTimeUntilUse <= 0)
                {  
                    UseAbility(entity, ability, opposingTeam, actingTeam, currentTime);
                }
            }

            entity.IncrementStep();
        }
    }

    private void PerformHealing(Entity entity, List<Entity> targets, int currentTime)
    {
        foreach (var target in targets)
        {
            var healing = entity.CalculateHealing(5);
            var healingReceived = target.PerformReceiveHealing(healing);
            CombatEvent(currentTime, entity, target, EventType.Heal, healingReceived);
        }
    }

    private void PerformDamage(Entity actor, List<Entity> targets, int currentTime)
    {
        foreach (var target in targets)
        {
            var damage = actor.CalculateDamage(5);
            damage = 5;
            var damageDealt = target.CalculateReceiveDamage(damage);
            CombatEvent(currentTime, actor, target, EventType.Damage, damageDealt);

            target.PerformReceiveDamage(damageDealt, actor);
        }
    }

    private void CombatEvent(int currentTime, Entity actor, Entity target, EventType actionType, int magnitude)
    {
        _eventLog.Add(new CombatEvent
        {
            Timestamp = currentTime,
            ActorId = actor.Id,
            TargetId = target.Id,
            EventType = actionType,
            Magnitude = magnitude,
            Details = $"{actor.Name} hit {target.Name} with a basic attack, dealing {magnitude} damage."
        });
    }

    private void PerformBasicAttack()
    {

    }

    private void UseAbility(Entity actor, Ability ability, List<Entity> opposingTeam, List<Entity> ownTeam, int currentTime)
    {
        if ((actor.CombatAttributes[AttributeType.Mana] - ability.Cost) < 0)
        {
            //_eventLog.Add(new CombatEvent()
            //{
            //    ActorId = actor.Id,
            //    Timestamp = currentTime,
            //    Details = $"{actor.Name} did not have enough mana to activate {ability.Name}"
            //});
            return;
        };
        // Deduct mana cost
        actor.CombatAttributes[AttributeType.Mana] -= ability.Cost;

        // Put ability on cooldown
        ability.RemainingTimeUntilUse = ability.Cooldown;

        var targetNames = new List<string>();
        var effectsToApply = new List<(Entity target, Effect effectInstance)>();
        var targetsPerTargeting = new Dictionary<Targeting, List<Entity>>(); // Cache for targets per Targeting type

        // Apply each effect of the ability
        foreach (var effectTemplate in ability.Effects)
        {
            // If multiple effects on the same ability has the same targeting,
            // the effects should be applied to the same targets
            if (!targetsPerTargeting.TryGetValue(effectTemplate.Targeting, out var targets))
            {
                targets = SelectTargets(effectTemplate.Targeting, actor, opposingTeam, ownTeam);
                targetsPerTargeting[effectTemplate.Targeting] = targets;
            }

            if (targets.Count == 0) return;

            foreach (var target in targets)
            {
                targetNames.Add(target.Name);

                var effectInstance = new Effect(
                action: effectTemplate.Action,
                duration: effectTemplate.Duration.Clone(),
                condition: effectTemplate.Condition.Clone(),
                targeting: effectTemplate.Targeting,
                trigger: effectTemplate.Trigger,
                interval: effectTemplate.Interval.Clone(),
                caster: actor,
                applyOnSelf: effectTemplate.ApplyOnSelf,
                isFlatAmount: effectTemplate.IsFlatAmount,
                chance: effectTemplate.Chance
                );

                if (effectTemplate.Action is SummonAction summonEffect)
                {
                    summonEffect.SetContext(actor, this);
                }

                effectInstance.Log = effectTemplate.Log;

                // Defer effect application until after logging
                effectsToApply.Add((target, effectInstance));
            }
        }
        var uniqueNames = targetNames.Distinct();
        var formattedNames = string.Join(", ", uniqueNames);

        // Actor has cast Ability log
        _eventLog.Add(new CombatEvent()
        {
            ActorId = actor.Id,
            Timestamp = currentTime,
            EventType = EventType.AbilityUse,
            Magnitude = ability.Cost,
            Details = ability.ActivationLog
            .Replace("{Actor}", actor.Name)
            .Replace("{Target}", formattedNames)
            .Replace("{Ability}", ability.Name)
        });

        foreach (var (target, effectInstance) in effectsToApply)
        {
            target.AddEffect(effectInstance);
        }
    }

    private List<Entity> SelectTargets(Targeting target, Entity caster, List<Entity> enemyTeam, List<Entity> allies)
    {
        List<Entity> targets = [];

        switch (target)
        {
            case Targeting.SingleEnemy:
                var enemyTarget = SelectTarget(enemyTeam);
                if (enemyTarget != null) targets.Add(enemyTarget);
                break;

            case Targeting.AllEnemies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;
            case Targeting.TwoEnemies:
                if(enemyTeam.Where(e => e.IsAlive).Count() >= 2) {
                    targets = enemyTeam.Where(e => e.IsAlive).Take(2).ToList();
                }
                else
                {
                    var enemyTargets = SelectTarget(enemyTeam);
                    if (enemyTargets != null) targets.Add(enemyTargets);
                }
                break;
            case Targeting.TwoAllies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;

            case Targeting.Self:
                targets.Add(caster);
                break;

            case Targeting.SingleAlly:
                var allyTarget = SelectTarget(allies);
                if (allyTarget != null) targets.Add(allyTarget);
                break;

            case Targeting.AllAllies:
                targets = allies.Where(a => a.IsAlive).ToList();
                break;

            default:
                throw new NotSupportedException($"Targeting type '{target}' is not supported.");
        }

        return targets;
    }

    private Entity? SelectTarget(List<Entity> potentialTargets)
    {
        // Select a random alive target
        var aliveTargets = potentialTargets.Where(c => c.IsAlive).ToList();
        if (aliveTargets.Count == 0) return null;

        var random = new Random();
        int index = random.Next(aliveTargets.Count);
        return aliveTargets[index];
    }

    private BattleOutcome DetermineOutcome()
    {
        if (_playerTeam.All(c => !c.IsAlive))
            return BattleOutcome.Defeat;
        else if (_enemyTeam.All(c => !c.IsAlive))
            return BattleOutcome.Victory;
        else
            return BattleOutcome.Draw; // In case max time reached
    }

    public void AddEntityToTeam(Entity caster, Entity newEntity)
    {
        if (_playerTeam.Contains(caster))
        {
            _playerTeam.Add(newEntity);
        }
        else if (_enemyTeam.Contains(caster))
        {
            _enemyTeam.Add(newEntity);
        }
        else
        {
            throw new InvalidOperationException("Caster not found on any team.");
        }
    }

    public void RemoveEntityFromTeam(Entity summonedCreature)
    {
        _playerTeam.Remove(summonedCreature);
        _enemyTeam.Remove(summonedCreature);
    }

    private void LogEffectExecution(EffectContext context)
    {
        var logEntry = new CombatEvent
        {
            Timestamp = CurrentTime,
            ActorId = context.Owner.Id,
            TargetId = context.Target.Id,
            EventType = context.EventType,
            Details = context.Details,
            Magnitude = context.Magnitude,
        };

        _eventLog.Add(logEntry);
    }
}
