using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.ResourceCosts;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Services.LL.Combat.CombatEngine;

namespace Services.LL.Combat;
public class CombatContext : ICombatContext
{
    public ICombatEntityManager EntityManager { get; set; }
    public ICombatEffectManager EffectManager { get; set; }
    public ICombatInteractionManager InteractionManager { get; set; }
    public IStatusDefinitionService StatusDefinitionService { get; set; }
    public ICombatEventBus EventBus { get; set; }
    private TriggerEngine _triggerEngine;

    private readonly List<CombatLogItem> _eventLog;
    private const int MaxSimulationTime = 6000; // Max duration in milliseconds
    private const int TimeStep = 1; // Time step in milliseconds
    public int CurrentTime { get; private set; } = 0;

    public CombatContext(ICombatEventBus eventBus, IStatusDefinitionService statusDefinitionService)
    {
        EventBus = new CombatEventBus();
        StatusDefinitionService = statusDefinitionService;

        EntityManager = new CombatEntityManager();
        EffectManager = new CombatEffectManager(this);
        InteractionManager = new CombatInteractionManager(this);
        _eventLog = [];

        _triggerEngine = new TriggerEngine(this, EventBus, EffectManager);
        _triggerEngine.Initialize();
    }

    public CombatResult InstantiateAndRunCombat(List<CombatEntity> playerEntities, List<CombatEntity> enemyEntities)
    {
        EntityManager.InitializeCombatEntityManager(playerEntities, enemyEntities);
        var combatResult = RunSimulation(false);
        Reset();
        return combatResult;
    }
    public void Reset()
    {
        CurrentTime = 0;
        _eventLog.Clear();

        EventBus = new CombatEventBus();
        EntityManager = new CombatEntityManager();
        EffectManager = new CombatEffectManager(this);
        InteractionManager = new CombatInteractionManager(this);

        _triggerEngine = new TriggerEngine(this, EventBus, EffectManager);
        _triggerEngine.Initialize();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="simulated">Whether this is being simulated for testing purposes</param>
    /// <returns></returns>
    private CombatResult RunSimulation(bool simulated = false)
    {
        InitiatePassiveAbilities(EntityManager.AllEntities);

        while (CurrentTime < MaxSimulationTime && EntityManager.IsCombatActive())
        {
            // Process actions for both teams
            ProcessTeamActions([.. EntityManager.PlayerTeam], [.. EntityManager.EnemyTeam]);
            ProcessTeamActions([.. EntityManager.EnemyTeam], [.. EntityManager.PlayerTeam]);

            EffectManager.Tick();

            // Advance time
            CurrentTime += TimeStep;
        }

        //// Determine outcome
        var outcome = DetermineOutcome();

        //if (true)
        //{
        //    foreach (var log in _eventLog)
        //    {
        //        Console.WriteLine($"Time: {log.Timestamp} - {log.Details}");
        //    }
        //    Console.WriteLine(outcome);
        //}

        return new CombatResult
        {
            EventLog = [.. _eventLog],
            Outcome = outcome,
            Duration = CurrentTime // CurrentTime 10 (10 ticks) equals 1 second
        };
    }

    private void InitiatePassiveAbilities(IEnumerable<CombatEntity> entities)
    {
        EventBus.Publish(new CombatEvent
        {
            Type = TriggerEvent.OnCombatStart,
            CurrentTime = CurrentTime,
        });
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
                
                EndTick(entity);
                continue;
            }

            foreach (var ability in entity.Abilities)
            {
                if (ability.Definition.Type == AbilityType.Active &&
                    ability.RemainingTimeUntilUse <= 0 &&
                    ability.Definition.Usage.CanUse())
                {
                    UseAbility(entity, ability);
                }
            }

            if (entity.NextBasicAttackIn <= 0)
            {
                // Perform basic
                //var weapon = entity.Equipment.FirstOrDefault(e => e.Type.Equals(ItemType.Weapon));
                //var target = SelectTarget(weapon.Targeting);
                UseBasicAttack(entity);

                entity.NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the Entity class
            }

            entity.NextBasicAttackIn -= (int)entity.CombatAttributes[AttributeType.BasicAttackSpeed];

            EndTick(entity);
        }
    }

    private void UseAbility(CombatEntity actor, AbilityInstance ability)
    {
        var def = ability.Definition;

        // Put ability on cooldown even if actor is out of mana/health
        ability.SetCooldown();

        var costType = def.Cost!.Type.Equals(ResourceType.Mana)
            ? AttributeType.Mana
            : AttributeType.Health;

        // Determine the minimum resource value allowed after paying the cost
        var minimumResourceAfterCost = (costType == AttributeType.Mana) ? 0 : 1;

        // Check if subtracting the cost would drop below the minimum
        if ((actor.CombatAttributes[costType] - def.Cost.Amount) < minimumResourceAfterCost)
            return;

        //var targetNames = new List<string>();
        //var effectsToApply = new List<(CombatEntity target, EffectInstance effectInstance)>();
        //var targetsPerTargeting = new Dictionary<Targeting, List<CombatEntity>>(); // Cache for targets per Targeting type


        // Apply each effect of the ability
        //foreach (var effectTemplate in ability.Effects)
        //{
        //    if (!effectTemplate.Usage.CanUse())
        //    {
        //        continue;
        //    }
        //    // If multiple effects on the same ability has the same targeting,
        //    // the effects should be applied to the same targets
        //    if (!targetsPerTargeting.TryGetValue(effectTemplate.Targeting, out var targets))
        //    {
        //        targets = TargetingManager.SelectTargets(effectTemplate.Targeting, actor, opposingTeam, ownTeam);
        //        targetsPerTargeting[effectTemplate.Targeting] = targets;
        //    }

        //    if (targets.Count == 0) return;

        //    foreach (var target in targets)
        //    {
        //        targetNames.Add(target.Name);
        //        var effectDefinitionCopy = effectTemplate.Clone();

        //        var effectInstance = new EffectInstance()
        //        {
        //            Definition = effectDefinitionCopy,
        //            Caster = actor,
        //            Owner = target,
        //        };

        //        // Defer effect application until after logging
        //        effectsToApply.Add((target, effectInstance));
        //    }
        //}
        //var uniqueNames = targetNames.Distinct();
        //var formattedNames = string.Join(", ", uniqueNames);

        //if (effectsToApply.Count == 0) return;

        // Deduct resource cost and usages in the end, as this should only happen if the ability
        // has actually been used and if there were any targets to use it on
        actor.CombatAttributes[costType] -= def.Cost.Amount;
        def.Usage.ConsumeUse();

        var simpleCombatEntity = new SimpleCombatEntity()
        {
            Id = actor.Id,
            MaxHealth = actor.GetAttributeValue(AttributeType.MaxHealth),
            Health = actor.GetAttributeValue(AttributeType.Health),
            MaxMana = actor.GetAttributeValue(AttributeType.MaxMana),
            Mana = actor.GetAttributeValue(AttributeType.Mana)
        };

        // Actor has cast Ability log
        //_eventLog.Add(new CombatLogItem()
        //{
        //    ActorId = actor.Id,
        //    Timestamp = CurrentTime,
        //    EventType = EventType.AbilityUse,
        //    Magnitude = ability.Cost,
        //    CombatEntity = simpleCombatEntity,
        //    Details = ability.ActivationLog
        //        .Replace("{Actor}", actor.Name)
        //        .Replace("{Target}", formattedNames)
        //        .Replace("{Ability}", ability.Name)
        //});

        // Optional: log basic info
        _eventLog.Add(new CombatLogItem
        {
            ActorId = actor.Id,
            Timestamp = CurrentTime,
            EventType = EventType.AbilityUse,
            Details = $"{actor.Name} used {def.Name}",
            CombatEntity = simpleCombatEntity
        });

        // Publish the ability use event (TriggerEngine will handle all effects)
        EventBus.Publish(new CombatEvent
        {
            Type = TriggerEvent.OnAbilityUsed,
            Source = actor,
            AbilityId = def.Id,
            CurrentTime = CurrentTime
        });
    }

    private void UseBasicAttack(CombatEntity entity)
    {
        EventBus.Publish(new CombatEvent
        {
            Type = TriggerEvent.BasicAttack,
            Source = entity,
            CurrentTime = CurrentTime
        });
    }

    private void CleanupDefeatedEntities()
    {
        foreach (var entity in EntityManager.AllEntities.Where(e => !e.IsAlive).ToList())
        {
            // TODO: Need a way to not count dead entities multiple times

            // Remove status effects if needed
            // Log death or raise events
            //_eventBus.Publish(new CombatEvent
            //{
            //    Type = "OnEntityDead",
            //    Source = entity,
            //    CurrentTime = CurrentTime
            //});

            // Remove if dead (optional, some systems keep them on field as corpses)
            // EntityManager.RemoveEntity(entity);
        }
    }

    private void EndTick(CombatEntity entity)
    {
        entity.IncrementStep();
        TickRecoveryRate(entity);
    }

    private void TickRecoveryRate(CombatEntity entity)
    {
        if (entity.NextRecoveryIn <= 0)
        {
            RegenerateHealthAndMana(entity);
            entity.NextRecoveryIn = 500;
        }
        entity.NextRecoveryIn -= (int)entity.CombatAttributes[AttributeType.RecoveryRate];
    }

    private void RegenerateHealthAndMana(CombatEntity entity)
    {
        entity.CombatAttributes[AttributeType.Health] += entity.CombatAttributes[AttributeType.HealthRegeneration];
        entity.CombatAttributes[AttributeType.Mana] += entity.CombatAttributes[AttributeType.ManaRegeneration];
        if (entity.CombatAttributes[AttributeType.Health] > entity.CombatAttributes[AttributeType.MaxHealth]) entity.CombatAttributes[AttributeType.Health] = entity.CombatAttributes[AttributeType.MaxHealth];
        if (entity.CombatAttributes[AttributeType.Mana] > entity.CombatAttributes[AttributeType.MaxMana]) entity.CombatAttributes[AttributeType.Mana] = entity.CombatAttributes[AttributeType.MaxMana];

        var combatEvent = CombatEvent(entity, entity, EventType.Regeneration, 0, $"{entity.Name} regenerated {entity.CombatAttributes[AttributeType.HealthRegeneration]} health and {entity.CombatAttributes[AttributeType.ManaRegeneration]} mana.");
        var combatEntity = new SimpleCombatEntity()
        {
            Id = entity.Id,
            MaxHealth = entity.GetAttributeValue(AttributeType.MaxHealth),
            Health = entity.GetAttributeValue(AttributeType.Health),
            MaxMana = entity.GetAttributeValue(AttributeType.MaxMana),
            Mana = entity.GetAttributeValue(AttributeType.Mana),
            Barrier = entity.GetAttributeValue(AttributeType.Barrier)
        };

        if (combatEntity.Health < 0) combatEntity.Health = 0;

        combatEvent.CombatEntity = combatEntity;
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
        //foreach (var target in targets)
        //{
        //    var attackOutcome = InteractionManager.CalculateAttackOutcomeForDamage(actor, target, []);
        //    if (attackOutcome.Equals(AttackOutcome.Miss))
        //    {
        //        CombatEvent(actor, target, EventType.Miss, 0, $"{actor.Name} missed {target.Name} with a basic attack.");
        //        EffectManager.TriggerEffects(TriggerEvent.OnDodge, target, actor);
        //        return;
        //    }
        //    var damage = InteractionManager.CalculateBasicAttackDamage(actor, target, 4);
        //    var damageResult = InteractionManager.CalculateDamageBreakdown(target, damage, attackOutcome, DamageType.Physical);

        //    var combatEvent = CombatEvent(actor, target, EventType.Damage, damageResult.HealthDamage, $"{actor.Name} hit {target.Name} with a basic attack, dealing {damageResult.TotalDamage} damage.");

        //    var effectDefinition = new EffectDefinition(null, null, null, null, null, [], [], attackType: AttackType.Melee);
        //    var effectContext = new EffectContext(
        //        new EffectInstance() { Definition = effectDefinition }, [], [], actor, target, damageResult.HealthDamage,
        //        $"{actor.Name} hit {target.Name} with a basic attack, dealing {damageResult.TotalDamage} damage.")
        //    {
        //        AttackType = AttackType.Melee
        //    };

        //    InteractionManager.ApplyDamage(effectContext);

        //    EffectManager.TriggerEffects(TriggerEvent.OnAttack, actor, target);

        //    var combatEntity = new SimpleCombatEntity()
        //    {
        //        Id = target.Id,
        //        MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
        //        Health = target.GetAttributeValue(AttributeType.Health),
        //        MaxMana = target.GetAttributeValue(AttributeType.MaxMana),
        //        Mana = target.GetAttributeValue(AttributeType.Mana),
        //        Barrier = target.GetAttributeValue(AttributeType.Barrier)
        //    };

        //    if (combatEntity.Health < 0) combatEntity.Health = 0;

        //    combatEvent.CombatEntity = combatEntity;
        //}
    }

    private CombatLogItem CombatEvent(CombatEntity actor, CombatEntity target, EventType actionType, int magnitude, string details, SimpleCombatEntity? combatEntity = null)
    {
        var combatEvent = new CombatLogItem
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
        var logEntry = new CombatLogItem
        {
            Timestamp = CurrentTime,
            ActorId = context.Source.Id,
            TargetId = context.Target.Id,
            EventType = context.EventType,
            Details = context.Details,
            Magnitude = context.Magnitude,
            //Attribute = context.Attribute,
            CombatEntity = combatEntity
        };

        _eventLog.Add(logEntry);
    }
}
