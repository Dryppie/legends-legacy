using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.ResourceCosts;
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
            ProcessTeamActions([.. EntityManager.PlayerTeam], [.. EntityManager.EnemyTeam]);
            ProcessTeamActions([.. EntityManager.EnemyTeam], [.. EntityManager.PlayerTeam]);

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
                    foreach (var effectTemplate in ability.Effects)
                    {
                        var effectDefinitionCopy = effectTemplate.Clone();

                        var effectInstance = new Effect()
                        {
                            Definition = effectDefinitionCopy,
                            Caster = entity,
                            Owner = entity,
                        };
                        // TODO: Add Targeting logic, since a passive ability might read -
                        // 'At the start of combat, apply X to Y targets
                        EffectManager.AddEffect(entity, entity, effectInstance);
                    }
                }
            }
        }
    }

    private void ProcessTeamActions(List<CombatEntity> actingTeam, List<CombatEntity> opposingTeam)
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
                            EffectManager.UpdateEffectsForEntity(entity);

                entity.IncrementStep();

                continue;
            }

            if (entity.NextBasicAttackIn <= 0)
            {
                // Perform basic
                //var weapon = entity.Equipment.FirstOrDefault(e => e.Type.Equals(ItemType.Weapon));
                //var target = SelectTarget(weapon.Targeting);
                var target = TargetingManager.SelectTarget(opposingTeam); // Replace this with the two previous lines
                if (target == null) continue;
                if (false/*weapon.Heal*/)
                {
                    PerformHealing(entity, [target]);
                }
                else
                {
                    PerformBasicAttackDamage(entity, [target]);
                }

                entity.NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the Entity class
            }

            entity.NextBasicAttackIn -= (int)entity.CombatAttributes[AttributeType.BasicAttackSpeed];

            foreach (var ability in entity.Abilities.Where(a => a.Type.Equals(AbilityType.Active)))
            {
                if (ability.RemainingTimeUntilUse <= 0)
                {  
                    UseAbility(entity, ability, opposingTeam, actingTeam);
                }
            }

            EffectManager.UpdateEffectsForEntity(entity);
            entity.IncrementStep();
        }

    }

    private static void PerformHealing(CombatEntity entity, List<CombatEntity> targets)
    {
        //foreach (var target in targets)
        //{
        //    var healing = entity.CalculateHealing(5);
        //    var healingReceived = target.PerformReceiveHealing(healing);
        //    CombatEvent(currentTime, entity, target, EventType.Heal, healingReceived);
        //}
    }

    private void PerformBasicAttackDamage(CombatEntity actor, List<CombatEntity> targets)
    {
        foreach (var target in targets)
        {
            var attackOutcome = InteractionManager.CalculateAttackOutcomeForDamage(actor, target, []);
            if (attackOutcome.Equals(AttackOutcome.Miss))
            {
                CombatEvent(actor, target, EventType.Miss, 0, $"{actor.Name} missed {target.Name} with a basic attack.");
                EffectManager.TriggerEffects(TriggerEvent.OnDodge, target, actor);
                return;
            }
            var damage = InteractionManager.CalculateBasicAttackDamage(actor, target, 4);
            var damageResult = InteractionManager.CalculateDamageBreakdown(target, damage, attackOutcome);

            var combatEvent = CombatEvent(actor, target, EventType.Damage, damageResult.HealthDamage, $"{actor.Name} hit {target.Name} with a basic attack, dealing {damageResult.TotalDamage} damage.");

            var effectDefinition = new EffectDefinition(null, null, null, null, null, [], [], attackType: AttackType.Melee);
            var effectContext = new EffectContext(
                new Effect() { Definition = effectDefinition }, [], [], actor, target, damageResult.HealthDamage,
                $"{actor.Name} hit {target.Name} with a basic attack, dealing {damageResult.TotalDamage} damage.")
            {
                AttackType = AttackType.Melee
            };

            InteractionManager.ApplyDamage(effectContext);

            var combatEntity = new SimpleCombatEntity()
            {
                Id = target.Id,
                MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
                Health = target.GetAttributeValue(AttributeType.Health),
                MaxMana = target.GetAttributeValue(AttributeType.MaxMana),
                Mana = target.GetAttributeValue(AttributeType.Mana),
                Barrier = target.GetAttributeValue(AttributeType.Barrier)
            };
            combatEntity.Health = Math.Max(0, combatEntity.Health - damageResult.HealthDamage);

            combatEvent.CombatEntity = combatEntity;
        }
    }

    private CombatEvent CombatEvent(CombatEntity actor, CombatEntity target, EventType actionType, int magnitude, string details, SimpleCombatEntity? combatEntity = null)
    {
        var combatEvent = new CombatEvent
        {
            Timestamp = CurrentTime,
            ActorId = actor.Id,
            TargetId = target.Id,
            EventType = actionType,
            Magnitude = magnitude,
            Details = details,
            CombatEntity = combatEntity
        };

        _eventLog.Add(combatEvent);

        return combatEvent;
    }

    private void UseAbility(CombatEntity actor, AbilityDefinition ability, List<CombatEntity> opposingTeam, List<CombatEntity> ownTeam)
    {
        // Put ability on cooldown even if actor is out of mana/health
        ability.RemainingTimeUntilUse = ability.Cooldown;

        var abilityResourceTypeCost = ability.ResourceTypeCost.Equals(ResourceType.Mana)
            ? AttributeType.Mana
            : AttributeType.Health;

        // Determine the minimum resource value allowed after paying the cost
        var minimumResourceAfterCost = (abilityResourceTypeCost == AttributeType.Mana) ? 0 : 1;

        // Check if subtracting the cost would drop below the minimum
        if ((actor.CombatAttributes[abilityResourceTypeCost] - ability.Cost) < minimumResourceAfterCost)
        {
            //_eventLog.Add(new CombatEvent()
            //{
            //    ActorId = actor.Id,
            //    Timestamp = currentTime,
            //    Details = $"{actor.Name} did not have enough mana to activate {ability.Name}"
            //});
            return;
        };

        // An ability must be able to be used
        if (!ability.Usage.CanUse()) return;

        var targetNames = new List<string>();
        var effectsToApply = new List<(CombatEntity target, Effect effectInstance)>();
        var targetsPerTargeting = new Dictionary<Targeting, List<CombatEntity>>(); // Cache for targets per Targeting type


        // Apply each effect of the ability
        foreach (var effectTemplate in ability.Effects)
        {
            if (!effectTemplate.Usage.CanUse())
            {
                continue;
            }
            // If multiple effects on the same ability has the same targeting,
            // the effects should be applied to the same targets
            if (!targetsPerTargeting.TryGetValue(effectTemplate.Targeting, out var targets))
            {
                targets = TargetingManager.SelectTargets(effectTemplate.Targeting, actor, opposingTeam, ownTeam);
                targetsPerTargeting[effectTemplate.Targeting] = targets;
            }

            if (targets.Count == 0) return;

            foreach (var target in targets)
            {
                targetNames.Add(target.Name);
                var effectDefinitionCopy = effectTemplate.Clone();

                var effectInstance = new Effect()
                {
                    Definition = effectDefinitionCopy,
                    Caster = actor,
                    Owner = target,
                };

                // Defer effect application until after logging
                effectsToApply.Add((target, effectInstance));
            }
        }
        var uniqueNames = targetNames.Distinct();
        var formattedNames = string.Join(", ", uniqueNames);

        if (effectsToApply.Count == 0) return;

        // Deduct resource cost and usages in the end, as this should only happen if the ability
        // has actually been used and if there were any targets to use it on
        actor.CombatAttributes[abilityResourceTypeCost] -= ability.Cost;
        ability.Usage.ConsumeUse();

        var simpleCombatEntity = new SimpleCombatEntity()
        {
            Id = actor.Id,
            MaxHealth = actor.GetAttributeValue(AttributeType.MaxHealth),
            Health = actor.GetAttributeValue(AttributeType.Health),
            MaxMana = actor.GetAttributeValue(AttributeType.MaxMana),
            Mana = actor.GetAttributeValue(AttributeType.Mana)
        };

        // Actor has cast Ability log
        _eventLog.Add(new CombatEvent()
        {
            ActorId = actor.Id,
            Timestamp = CurrentTime,
            EventType = EventType.AbilityUse,
            Magnitude = ability.Cost,
            CombatEntity = simpleCombatEntity,
            Details = ability.ActivationLog
            .Replace("{Actor}", actor.Name)
            .Replace("{Target}", formattedNames)
            .Replace("{Ability}", ability.Name)
        });

        foreach (var (target, effectInstance) in effectsToApply)
        {
            EffectManager.AddEffect(actor, target, effectInstance);
        }

        // Only trigger OnAbility used after all the ability's effects have been used
        EffectManager.TriggerEffects(TriggerEvent.OnAbilityUsed, actor, actor);
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

    public void LogEffectExecution(EffectContext context, SimpleCombatEntity? combatEntity)
    {
        var logEntry = new CombatEvent
        {
            Timestamp = CurrentTime,
            ActorId = context.Actor.Id,
            TargetId = context.Target.Id,
            EventType = context.EventType,
            Details = context.Details,
            Magnitude = context.Magnitude,
            Attribute = context.Attribute,
            CombatEntity = combatEntity
        };

        _eventLog.Add(logEntry);
    }
}
