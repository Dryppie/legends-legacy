using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Services.LL.Combat;
public class CombatSimulation : ICombatContext
{
    public ICombatEntityManager EntityManager { get; set; }
    public ICombatEffectManager EffectManager { get; set; }
    public ICombatInteractionManager InteractionManager { get; set; }

    private readonly List<CombatEvent> _eventLog;
    private const int MaxSimulationTime = 6000; // Max duration in milliseconds
    private const int TimeStep = 1; // Time step in milliseconds
    private int CurrentTime = 0;

    public CombatSimulation(List<CombatEntity> playerTeam, List<CombatEntity> enemyTeam)
    {
        _eventLog = [];

        // Managers
        EntityManager = new CombatEntityManager(playerTeam, enemyTeam);
        EffectManager = new CombatEffectManager(EntityManager, this, _eventLog);
        InteractionManager = new CombatInteractionManager(EffectManager);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="simulated">Whether this is being simulated for testing purposes</param>
    /// <returns></returns>
    public CombatResult RunSimulation(bool simulated = false)
    {
        InitiatePassiveAbilities(EntityManager.AllEntities);

        while (CurrentTime < MaxSimulationTime && EntityManager.IsCombatActive())
        {
            // Process actions for both teams
            ProcessTeamActions([.. EntityManager.PlayerTeam], [.. EntityManager.EnemyTeam], CurrentTime);
            ProcessTeamActions([.. EntityManager.EnemyTeam], [.. EntityManager.PlayerTeam], CurrentTime);

            // Advance time
            CurrentTime += TimeStep;
        }

        // Determine outcome
        var outcome = DetermineOutcome();
        if (simulated)
        {
            foreach (var log in _eventLog)
            {
                Console.WriteLine($"Time: {log.Timestamp} - {log.Details}");
            }
            Console.WriteLine(outcome);
        }

        return new CombatResult
        {
            EventLog = _eventLog,
            Outcome = outcome,
            Duration = CurrentTime // CurrentTime 10 (10 ticks) equals 1 second
        };
    }

    private void InitiatePassiveAbilities(List<CombatEntity> entities)
    {
        foreach (var entity in entities)
        {
            foreach (var ability in entity.Abilities)
            {
                ability.RemainingTimeUntilUse = ability.Cooldown;
                if (ability.Type.Equals(AbilityType.Passive))
                {
                    foreach (var effectDefinition in ability.Effects)
                    {
                        var effect = new Effect()
                        {
                            Definition = effectDefinition,
                            Caster = entity,
                            Owner = entity,
                        };
                        // TODO: Add Targeting logic, since a passive ability might read -
                        // 'At the start of combat, apply X to Y targets
                        EffectManager.AddEffect(entity, entity, effect);
                    }
                }
            }
        }
    }

    private void ProcessTeamActions(List<CombatEntity> actingTeam, List<CombatEntity> opposingTeam, int currentTime)
    {
        foreach (var entity in actingTeam)
        {
            if (!entity.IsAlive) // Perform specific logic if a player is currently dead
            {
                // PERFORM LOGIC HERE
                continue;
            }

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
                    PerformBasicAttackDamage(entity, [target], currentTime);
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

            EffectManager.UpdateEffectsForEntity(entity);

            entity.IncrementStep();
        }

    }

    private static void PerformHealing(CombatEntity entity, List<CombatEntity> targets, int currentTime)
    {
        //foreach (var target in targets)
        //{
        //    var healing = entity.CalculateHealing(5);
        //    var healingReceived = target.PerformReceiveHealing(healing);
        //    CombatEvent(currentTime, entity, target, EventType.Heal, healingReceived);
        //}
    }

    private void PerformBasicAttackDamage(CombatEntity actor, List<CombatEntity> targets, int currentTime)
    {
        foreach (var target in targets)
        {
            var damage = InteractionManager.CalculateBasicAttackDamage(actor, 5);
            damage = 5;
            CombatEvent(currentTime, actor, target, EventType.Damage, damage);


            var effectDefinition = new EffectDefinition(null, null, null, null, null, [], attackType: AttackType.Melee);
            var effectContext = new EffectContext(
                new Effect() { Definition = effectDefinition }, [], [], actor, target, damage,
                $"{actor.Name} hit {target.Name} with a basic attack, dealing {damage} damage.")
            {
                AttackType = AttackType.Melee
            };

            InteractionManager.ApplyDamage(effectContext);
        }
    }

    private void CombatEvent(int currentTime, CombatEntity actor, CombatEntity target, EventType actionType, int magnitude)
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

    private void UseAbility(CombatEntity actor, AbilityDefinition ability, List<CombatEntity> opposingTeam, List<CombatEntity> ownTeam, int currentTime)
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

        var battleContext = new BattleContext(ownTeam, opposingTeam); // Is this needed???

        var targetNames = new List<string>();
        var effectsToApply = new List<(CombatEntity target, Effect effectInstance)>();
        var targetsPerTargeting = new Dictionary<Targeting, List<CombatEntity>>(); // Cache for targets per Targeting type

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
                var effectDefinitionCopy = new EffectDefinition(action: effectTemplate.Action,
                    duration: effectTemplate.Duration.Clone(),
                    condition: effectTemplate.Condition.Clone(),
                    interval: effectTemplate.Interval.Clone(),
                    usage: effectTemplate.Usage.Clone(),
                    targeting: effectTemplate.Targeting,
                    trigger: effectTemplate.Trigger,
                    triggerTarget: effectTemplate.TriggerTarget,
                    isFlatAmount: effectTemplate.IsFlatAmount,
                    chance: effectTemplate.Chance,
                    effectTags: effectTemplate.EffectTags,
                    attackType: effectTemplate.AttackType,
                    damageType: effectTemplate.DamageType)
                {
                    Log = effectTemplate.Log
                };

                var effectInstance = new Effect()
                {
                    Definition = effectDefinitionCopy,
                    Caster = actor,
                    Owner = target,
                };

                //var effectInstance = new EffectDefinition(
                //action: effectTemplate.Action,
                //duration: effectTemplate.Duration.Clone(),
                //condition: effectTemplate.Condition.Clone(),
                //interval: effectTemplate.Interval.Clone(),
                //usage: effectTemplate.Usage.Clone(),
                //targeting: effectTemplate.Targeting,
                //trigger: effectTemplate.Trigger,
                //applyOnSelf: effectTemplate.ApplyOnSelf,
                //isFlatAmount: effectTemplate.IsFlatAmount,
                //chance: effectTemplate.Chance,
                //effectTags: effectTemplate.EffectTags,
                //attackType: effectTemplate.AttackType,
                //damageType: effectTemplate.DamageType
                //)
                //{
                //    Log = effectTemplate.Log
                //};

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
            EffectManager.AddEffect(actor, target, effectInstance);
        }
    }

    private static List<CombatEntity> SelectTargets(Targeting target, CombatEntity caster, List<CombatEntity> enemyTeam, List<CombatEntity> allies)
    {
        List<CombatEntity> targets = [];

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

    private static CombatEntity? SelectTarget(List<CombatEntity> potentialTargets)
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
        if (EntityManager.PlayerTeam.All(c => !c.IsAlive))
            return BattleOutcome.Defeat;
        else if (EntityManager.EnemyTeam.All(c => !c.IsAlive))
            return BattleOutcome.Victory;
        else
            return BattleOutcome.Draw; // In case max time reached
    }

    public void LogEffectExecution(EffectContext context)
    {
        var logEntry = new CombatEvent
        {
            Timestamp = CurrentTime,
            ActorId = context.Actor.Id,
            TargetId = context.Target.Id,
            EventType = context.EventType,
            Details = context.Details,
            Magnitude = context.Magnitude,
        };

        _eventLog.Add(logEntry);
    }
}
