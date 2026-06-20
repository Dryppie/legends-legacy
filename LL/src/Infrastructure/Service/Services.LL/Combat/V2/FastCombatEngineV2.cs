using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.V2;
using Domain.Models.Damages;
using Services.LL.Combat.Stats;

namespace Services.LL.Combat.V2;

public sealed record FastCombatEngineV2Options(int MaxTicks = 6000, int BasicAttackIntervalTicks = 30, int RandomSeed = 1337);

public sealed class FastCombatEngineV2
{
    private readonly IReadOnlyDictionary<string, CompiledStatusV2> _statusesById;
    private readonly IReadOnlyDictionary<string, CompiledSummonV2> _summonsById;
    private readonly IReadOnlyDictionary<string, CompiledAbilityV2> _abilitiesById;
    private readonly Random _random;
    private readonly int _maxTicks;
    private readonly int _basicAttackIntervalTicks;
    private readonly Dictionary<RuntimeCombatantV2, int> _basicAttackTimers = [];
    private readonly List<CombatLogItem> _log = [];
    private int _currentTick;

    public FastCombatEngineV2(
        IReadOnlyDictionary<string, CompiledStatusV2> statusesById,
        FastCombatEngineV2Options? options = null)
        : this(statusesById, new Dictionary<string, CompiledSummonV2>(), new Dictionary<string, CompiledAbilityV2>(), options)
    {
    }

    public FastCombatEngineV2(
        IReadOnlyDictionary<string, CompiledStatusV2> statusesById,
        IReadOnlyDictionary<string, CompiledSummonV2> summonsById,
        IReadOnlyDictionary<string, CompiledAbilityV2> abilitiesById,
        FastCombatEngineV2Options? options = null)
    {
        var resolved = options ?? new FastCombatEngineV2Options();
        _statusesById = statusesById;
        _summonsById = summonsById;
        _abilitiesById = abilitiesById;
        _random = new Random(resolved.RandomSeed);
        _maxTicks = resolved.MaxTicks;
        _basicAttackIntervalTicks = resolved.BasicAttackIntervalTicks;
    }

    public CombatResult Run(IReadOnlyList<RuntimeCombatantV2> friendly, IReadOnlyList<RuntimeCombatantV2> hostile)
    {
        var combatants = friendly.Concat(hostile).ToList();
        foreach (var combatant in combatants)
            _basicAttackTimers[combatant] = _basicAttackIntervalTicks;

        Publish(new CombatEventV2(AbilityTriggerEvent.OnCombatStart, null, null, null), combatants);

        while (_currentTick < _maxTicks
               && HasLivingTeam(combatants, CombatTeamV2.Friendly)
               && HasLivingTeam(combatants, CombatTeamV2.Hostile))
        {
            foreach (var combatant in combatants.Where(x => x.IsAlive).ToList())
            {
                if (IsActionBlocked(combatant) || !HasLivingOpponent(combatant, combatants))
                    continue;

                UseReadyActiveAbilities(combatant, combatants);

                if (HasLivingOpponent(combatant, combatants))
                    TickBasicAttack(combatant, combatants);
            }

            TickEffects(combatants);
            TickStatuses(combatants);

            foreach (var combatant in combatants)
                combatant.Tick();

            TickSummons(combatants);
            _currentTick++;
        }

        return new CombatResult
        {
            EventLog = [.. _log],
            Duration = _currentTick,
            Outcome = DetermineOutcome(combatants),
            EntityStats = [.. new CombatStatsAggregator().Aggregate(_log)]
        };
    }

    private void UseReadyActiveAbilities(RuntimeCombatantV2 actor, IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var ability in actor.Abilities.Where(x => x.Definition.Kind == AbilitySpecKind.Active && x.IsReady))
        {
            if (!HasLivingOpponent(actor, combatants) || !CanResolveActiveAbility(ability, actor, combatants))
                continue;

            ability.StartCooldown();
            Log(actor, null, ability.Definition.Name, EventType.AbilityUse, 0, $"{actor.Name} used {ability.Definition.Name}");
            Publish(new CombatEventV2(AbilityTriggerEvent.OnAbilityUsed, actor, null, ability.Definition.Id), combatants);
        }
    }

    private void TickBasicAttack(RuntimeCombatantV2 actor, IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        _basicAttackTimers[actor]--;
        if (_basicAttackTimers[actor] > 0)
            return;

        _basicAttackTimers[actor] = _basicAttackIntervalTicks;
        if (SelectFirstEnemy(actor, combatants) is not { } target)
            return;

        var damage = Math.Max(1, (int)Math.Round(1 + actor.GetAttribute(AttributeType.Power) / 10f));
        Log(actor, null, "Basic Attack", EventType.AbilityUse, 0, $"{actor.Name} used Basic Attack");
        ApplyDamage(actor, target, damage, AttackType.Melee, DamageType.Physical, combatants, "Basic Attack");
    }

    private static bool IsActionBlocked(RuntimeCombatantV2 combatant) =>
        combatant.Statuses.Any(status => status.Stacks > 0 && status.Definition.Tags.Contains("Control.Stun"));

    private static bool HasLivingOpponent(RuntimeCombatantV2 actor, IReadOnlyList<RuntimeCombatantV2> combatants) =>
        combatants.Any(x => x.Team != actor.Team && x.IsAlive);

    private static bool HasLivingTeam(IReadOnlyList<RuntimeCombatantV2> combatants, CombatTeamV2 team) =>
        combatants.Any(x => x.Team == team && x.IsAlive);

    private bool CanResolveActiveAbility(
        RuntimeAbilityV2 ability,
        RuntimeCombatantV2 actor,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        if (!ability.Definition.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers))
            return false;

        var combatEvent = new CombatEventV2(AbilityTriggerEvent.OnAbilityUsed, actor, null, ability.Definition.Id);
        return triggers
            .Where(trigger => ConditionsPass(trigger.Conditions, actor, combatEvent))
            .SelectMany(trigger => trigger.Effects)
            .Any(effect => SelectTargets(actor, effect.Target, combatEvent, combatants)
                .Any(target => target.IsAlive
                    && EffectCanResolve(effect, actor, combatants)
                    && ConditionsPass(effect.Conditions, actor, combatEvent with { Target = target })));
    }

    private void Publish(CombatEventV2 combatEvent, IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var combatant in combatants.Where(x => x.IsAlive).ToList())
        {
            if (combatant.AbilityTriggersByEvent.TryGetValue(combatEvent.Event, out var abilities))
            {
                foreach (var ability in abilities.ToList())
                {
                    if (ability.Definition.Kind == AbilitySpecKind.Active
                        && !string.Equals(combatEvent.AbilityId, ability.Definition.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    foreach (var trigger in ability.Definition.TriggersByEvent[combatEvent.Event])
                    {
                        if (!ability.CanUseTrigger(trigger) || !ConditionsPass(trigger.Conditions, combatant, combatEvent))
                            continue;

                        ExecuteTrigger(
                            trigger,
                            combatant,
                            combatEvent,
                            combatants,
                            ability.CanUseEffect,
                            ability.MarkEffectUsed,
                            countStatsActivation: ability.Definition.Kind == AbilitySpecKind.Passive);
                        ability.StartTriggerCooldown(trigger);
                    }
                }
            }

            foreach (var status in combatant.Statuses.ToList())
            {
                if (!status.Definition.TriggersByEvent.TryGetValue(combatEvent.Event, out var triggers))
                    continue;

                foreach (var trigger in triggers)
                {
                    if (IsStatusLifecycleEvent(combatEvent.Event)
                        && (!string.Equals(combatEvent.AbilityId, status.Definition.Id, StringComparison.OrdinalIgnoreCase)
                            || !ReferenceEquals(combatEvent.Target, status.Owner)))
                    {
                        continue;
                    }

                    if (!status.CanUseTrigger(trigger) || !ConditionsPass(trigger.Conditions, status.Source, combatEvent))
                        continue;

                    ExecuteTrigger(
                        trigger,
                        status.Source,
                        combatEvent,
                        combatants,
                        status.CanUseEffect,
                        status.MarkEffectUsed,
                        status.StatsSource,
                        countStatsActivation: false);
                    status.StartTriggerCooldown(trigger);
                }
            }
        }
    }

    private void ExecuteTrigger(
        CompiledTriggerV2 trigger,
        RuntimeCombatantV2 source,
        CombatEventV2 combatEvent,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        Func<CompiledEffectV2, bool> canUseEffect,
        Action<CompiledEffectV2> markEffectUsed,
        string? statsSourceOverride = null,
        bool countStatsActivation = false)
    {
        var activationCounted = false;
        foreach (var effect in trigger.Effects)
        {
            if (!canUseEffect(effect))
                continue;

            foreach (var target in SelectTargets(source, effect.Target, combatEvent, combatants))
            {
                if (!canUseEffect(effect))
                    break;

                if (!target.IsAlive || !ConditionsPass(effect.Conditions, source, combatEvent with { Target = target }))
                    continue;

                if (!IsPeriodicEffect(effect) && effect.ChancePercent < 100 && _random.Next(1, 101) > effect.ChancePercent)
                    continue;

                var countThisActivation = countStatsActivation && !activationCounted;
                ExecuteEffect(effect, source, target, combatants, statsSourceOverride, countThisActivation);
                if (countThisActivation)
                    activationCounted = true;

                markEffectUsed(effect);
            }
        }
    }

    private void ExecuteEffect(
        CompiledEffectV2 effect,
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string? statsSourceOverride = null,
        bool countStatsActivation = false)
    {
        var statsSource = statsSourceOverride ?? effect.StatsSource;
        if (effect.IntervalTicks > 0 && effect.DurationTicks > 0)
        {
            target.ActiveEffects.Add(new RuntimeEffectV2(effect, source, target, statsSource));
            return;
        }

        ApplyEffectOnce(effect, source, target, combatants, statsSource, countStatsActivation);

        if (effect.DurationTicks > 0 && effect.Operation == AbilityEffectOperation.ModifyAttribute)
            target.ActiveEffects.Add(new RuntimeEffectV2(effect, source, target, statsSource));
    }

    private void ApplyEffectOnce(
        CompiledEffectV2 effect,
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string? statsSourceOverride = null,
        bool countStatsActivation = false)
    {
        var value = CalculateValue(effect, source);
        var statsSource = statsSourceOverride ?? effect.StatsSource;

        switch (effect.Operation)
        {
            case AbilityEffectOperation.Damage:
                var healthDamage = ApplyDamage(source, target, value, effect.AttackType, effect.DamageType, combatants, effect.Id, statsSource, countStatsActivation);
                ApplyLifeSteal(effect, source, healthDamage, combatants, statsSource);
                break;
            case AbilityEffectOperation.Heal:
                RestoreHealth(source, target, value, combatants, effect.Id, statsSource, isLifeSteal: false, countStatsActivation);
                break;
            case AbilityEffectOperation.GrantBarrier:
                target.AdjustBarrier(value);
                Log(source, target, effect.Id, EventType.RestoreBarrier, value, $"{source.Name} granted {value} barrier to {target.Name}.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.RestoreResource:
                if (effect.Resource == AbilityResourceTypeV2.Cooldown)
                {
                    target.ReduceAbilityCooldowns(value);
                    Log(source, target, effect.Id, EventType.Buff, value, $"{source.Name} restored {value} cooldown ticks to {target.Name}.", statsSource, countStatsActivation);
                }
                else if (effect.Resource == AbilityResourceTypeV2.Barrier)
                {
                    target.AdjustBarrier(value);
                    Log(source, target, effect.Id, EventType.RestoreBarrier, value, $"{source.Name} restored {value} barrier to {target.Name}.", statsSource, countStatsActivation);
                }
                else
                {
                    RestoreHealth(source, target, value, combatants, effect.Id, statsSource, isLifeSteal: false, countStatsActivation);
                }

                break;
            case AbilityEffectOperation.ApplyStatus:
                ApplyStatus(source, target, effect.StatusId!, Math.Max(1, value), combatants, statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyStatusStacks:
                ModifyStatusStacks(source, target, effect.StatusId!, value, combatants);
                break;
            case AbilityEffectOperation.RemoveStatus:
                RemoveStatus(source, target, effect.StatusId!, combatants);
                Log(source, target, effect.Id, EventType.StatusEffectExpired, 0, $"{target.Name} lost {effect.StatusId}.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.Cleanse:
                CleanseStatuses(source, target, combatants);
                Log(source, target, effect.Id, EventType.StatusEffectExpired, 0, $"{target.Name} was cleansed.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyAttribute:
                target.AdjustAttribute(effect.Attribute!.Value, value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s {effect.Attribute} changed by {value}.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.Summon:
                SummonCombatant(source, effect, combatants, statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.SelfDestruct:
                target.SetHealth(0);
                Log(source, target, effect.Id, EventType.Death, 0, $"{target.Name} self-destructed.", statsSource, countStatsActivation);
                ExpireOwnedSummons(target, combatants, "owner death");
                break;
            default:
                throw new NotSupportedException($"Unsupported ability v2 operation '{effect.Operation}'.");
        }
    }

    private int ApplyDamage(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        int damage,
        AttackType attackType,
        DamageType damageType,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string sourceName,
        string? statsSource = null,
        bool countStatsActivation = false)
    {
        if (CanDodge(attackType) && target.GetAttribute(AttributeType.DodgeChance) > 0)
        {
            var dodgeChance = Math.Clamp(target.GetAttribute(AttributeType.DodgeChance), 0, 100);
            if (_random.NextDouble() * 100 < dodgeChance)
            {
                Log(source, target, sourceName, EventType.Miss, 0, $"{source.Name} missed {target.Name}.", statsSource, countStatsActivation);
                Publish(new CombatEventV2(AbilityTriggerEvent.OnDodge, target, source, null), combatants);
                return 0;
            }
        }

        var reducedDamage = ApplyDamageReduction(target, damage);
        var barrierBefore = target.Barrier;
        var absorbed = Math.Min(barrierBefore, reducedDamage);
        target.AdjustBarrier(-absorbed);
        var healthDamage = Math.Max(0, reducedDamage - (int)absorbed);
        target.AdjustHealth(-healthDamage);

        Log(source, target, sourceName, EventType.Damage, healthDamage, $"{source.Name} dealt {healthDamage} {damageType} damage to {target.Name}.", statsSource, countStatsActivation);
        Publish(new CombatEventV2(AbilityTriggerEvent.OnHit, source, target, null), combatants);
        PublishAttackTypeEvents(source, target, attackType, combatants);
        Publish(new CombatEventV2(AbilityTriggerEvent.OnDamaged, target, source, null), combatants);
        Publish(new CombatEventV2(AbilityTriggerEvent.OnAttacked, target, source, null), combatants);
        if (healthDamage > 0)
            Publish(new CombatEventV2(AbilityTriggerEvent.OnHealthChanged, target, source, null), combatants);

        if (!target.IsAlive)
        {
            Log(source, target, sourceName, EventType.Death, 0, $"{target.Name} was killed by {source.Name}.", statsSource);
            Publish(new CombatEventV2(AbilityTriggerEvent.OnKill, source, target, null), combatants);
            Publish(new CombatEventV2(AbilityTriggerEvent.OnDeath, target, source, null), combatants);
            ExpireOwnedSummons(target, combatants, "owner death");
        }

        return healthDamage;
    }

    private static bool CanDodge(AttackType attackType) =>
        attackType is AttackType.Melee or AttackType.Ranged;

    private static int ApplyDamageReduction(RuntimeCombatantV2 target, int damage)
    {
        var reduction = Math.Clamp(target.GetAttribute(AttributeType.DamageReduction), -100, 100);
        return Math.Max(0, (int)Math.Round(damage * (1 - reduction / 100f)));
    }

    private void PublishAttackTypeEvents(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        AttackType attackType,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        switch (attackType)
        {
            case AttackType.Melee:
                Publish(new CombatEventV2(AbilityTriggerEvent.OnMeleeAttack, source, target, null), combatants);
                Publish(new CombatEventV2(AbilityTriggerEvent.OnMeleeAttacked, target, source, null), combatants);
                break;
            case AttackType.Ranged:
                Publish(new CombatEventV2(AbilityTriggerEvent.OnRangedAttack, source, target, null), combatants);
                Publish(new CombatEventV2(AbilityTriggerEvent.OnRangedAttacked, target, source, null), combatants);
                break;
        }
    }

    private void RestoreHealth(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        int value,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string sourceName,
        string? statsSource,
        bool isLifeSteal,
        bool countStatsActivation = false)
    {
        var before = target.Health;
        target.AdjustHealth(value);
        var restored = Math.Max(0, (int)Math.Round(target.Health - before));
        Log(source, target, sourceName, EventType.Heal, restored, $"{source.Name} healed {target.Name} for {restored}.", statsSource, countStatsActivation);

        if (restored <= 0)
            return;

        Publish(new CombatEventV2(AbilityTriggerEvent.OnHeal, source, target, null), combatants);
        Publish(new CombatEventV2(AbilityTriggerEvent.OnHealed, target, source, null), combatants);
        Publish(new CombatEventV2(AbilityTriggerEvent.OnHealthChanged, target, source, null), combatants);

        if (isLifeSteal)
            Publish(new CombatEventV2(AbilityTriggerEvent.OnLifestealHeal, source, target, null), combatants);
    }

    private void ApplyLifeSteal(
        CompiledEffectV2 effect,
        RuntimeCombatantV2 source,
        int healthDamage,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string? statsSource)
    {
        if (effect.LifeStealPercentage <= 0 || healthDamage <= 0)
            return;

        var healing = (int)Math.Round(healthDamage * (effect.LifeStealPercentage / 100f));
        if (healing <= 0)
            return;

        RestoreHealth(source, source, healing, combatants, effect.Id, statsSource, isLifeSteal: true);
    }

    private void ApplyStatus(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        string statusId,
        int stacks,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string? statsSource = null,
        bool countStatsActivation = false)
    {
        if (!_statusesById.TryGetValue(statusId, out var statusDefinition))
            throw new InvalidOperationException($"Status '{statusId}' has not been compiled.");

        var existing = target.Statuses.FirstOrDefault(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (statusDefinition.StackingPolicy == AbilityStatusStackingPolicy.Replace)
                ExpireStatus(source, target, existing, combatants);
            else if (statusDefinition.StackingPolicy == AbilityStatusStackingPolicy.Refresh)
                existing.Refresh(stacks);
            else
                existing.AddStacks(stacks);
        }

        if (existing is null || statusDefinition.StackingPolicy == AbilityStatusStackingPolicy.Replace)
            target.Statuses.Add(new RuntimeStatusV2(statusDefinition, source, target, stacks, statsSource));

        Log(source, target, statusId, EventType.StatusEffect, stacks, $"{source.Name} applied {statusId} to {target.Name}.", statsSource, countStatsActivation);
        Publish(new CombatEventV2(AbilityTriggerEvent.OnStatusApplied, source, target, statusId), combatants);
    }

    private void SummonCombatant(
        RuntimeCombatantV2 source,
        CompiledEffectV2 effect,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        if (string.IsNullOrWhiteSpace(effect.SummonId))
            throw new InvalidOperationException($"Summon effect '{effect.Id}' requires summonId.");

        if (combatants is not List<RuntimeCombatantV2> mutableCombatants)
            throw new InvalidOperationException("Summon effects require a mutable combatant list.");

        if (!_summonsById.TryGetValue(effect.SummonId, out var summonDefinition))
            throw new InvalidOperationException($"Summon '{effect.SummonId}' has not been compiled.");

        if (HasReachedSummonCap(source, summonDefinition, combatants))
            return;

        var summon = CreateSummonedCombatant(source, effect, summonDefinition, _abilitiesById);
        mutableCombatants.Add(summon);
        _basicAttackTimers[summon] = _basicAttackIntervalTicks;

        Log(source, summon, effect.Id, EventType.Summon, 1, $"{source.Name} summoned {summon.Name}.", statsSource, countStatsActivation);
    }

    private static RuntimeCombatantV2 CreateSummonedCombatant(
        RuntimeCombatantV2 source,
        CompiledEffectV2 effect,
        CompiledSummonV2 summonDefinition,
        IReadOnlyDictionary<string, CompiledAbilityV2> abilitiesById)
    {
        var summonId = effect.SummonId!;
        var attributes = CreateSummonAttributes(source, summonDefinition);
        var abilities = summonDefinition.AbilityIds
            .Select(abilityId => abilitiesById.TryGetValue(abilityId, out var ability)
                ? ability
                : throw new InvalidOperationException($"Summon '{summonId}' references ability '{abilityId}' that has not been compiled."))
            .ToList();
        var tags = new HashSet<string>(summonDefinition.Tags, StringComparer.OrdinalIgnoreCase)
        {
            "Summoned",
            $"Summon.{summonId}"
        };

        return new RuntimeCombatantV2(
            id: $"{source.Id}:summon:{summonId}:{Guid.NewGuid():N}",
            name: summonDefinition.Name,
            team: source.Team,
            attributes: attributes,
            abilities: abilities,
            tags: tags,
            imagePath: summonDefinition.ImagePath,
            isSummoned: true,
            summonDurationTicks: effect.DurationTicks > 0 ? effect.DurationTicks : summonDefinition.DurationTicks,
            summonOwner: source);
    }

    private static Dictionary<AttributeType, float> CreateSummonAttributes(
        RuntimeCombatantV2 source,
        CompiledSummonV2 summonDefinition)
    {
        var attributes = summonDefinition.Attributes.ToDictionary(
            attribute => attribute.Attribute,
            attribute => (float)Math.Max(
                attribute.MinimumValue,
                (int)Math.Round(attribute.BaseValue + (attribute.ScalingAttribute is { } scalingAttribute
                    ? source.GetAttribute(scalingAttribute) * attribute.ScalingCoefficient
                    : 0))));

        attributes.TryAdd(AttributeType.MaxHealth, 1);
        attributes.TryAdd(AttributeType.Power, 0);
        return attributes;
    }

    private void ModifyStatusStacks(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        string statusId,
        int amount,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        var existing = target.Statuses.FirstOrDefault(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return;

        existing.AddStacks(amount);
        if (existing.Stacks <= 0)
            RemoveStatus(source, target, statusId, combatants);
    }

    private void RemoveStatus(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        string statusId,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var status in target.Statuses
                     .Where(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            ExpireStatus(source, target, status, combatants);
        }
    }

    private void CleanseStatuses(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var status in target.Statuses.ToList())
            ExpireStatus(source, target, status, combatants);
    }

    private void ExpireStatus(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 target,
        RuntimeStatusV2 status,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        if (!target.Statuses.Remove(status))
            return;

        Log(source, target, status.Definition.Id, EventType.StatusEffectExpired, 0, $"{status.Definition.Id} expired on {target.Name}.");
        Publish(new CombatEventV2(AbilityTriggerEvent.OnStatusExpired, status.Source, target, status.Definition.Id), combatants);
    }

    private void TickEffects(IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var combatant in combatants)
        {
            foreach (var effect in combatant.ActiveEffects.ToList())
            {
                if (effect.Tick() && (effect.Definition.ChancePercent >= 100 || _random.Next(1, 101) <= effect.Definition.ChancePercent))
                    ApplyEffectOnce(effect.Definition, effect.Source, effect.Target, combatants, effect.StatsSource);

                if (effect.IsExpired)
                {
                    if (effect.Definition.Operation == AbilityEffectOperation.ModifyAttribute)
                    {
                        var value = CalculateValue(effect.Definition, effect.Source);
                        effect.Target.AdjustAttribute(effect.Definition.Attribute!.Value, -value);
                        Log(effect.Source, effect.Target, effect.Definition.Id, EventType.BuffExpired, -value, $"{effect.Target.Name}'s {effect.Definition.Attribute} returned to normal.", effect.StatsSource);
                    }

                    combatant.ActiveEffects.Remove(effect);
                }
            }
        }
    }

    private void TickStatuses(IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var combatant in combatants)
        {
            foreach (var status in combatant.Statuses.ToList())
            {
                if (!status.IsExpired)
                    continue;

                ExpireStatus(status.Source, combatant, status, combatants);
            }
        }
    }

    private void TickSummons(IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        foreach (var summon in combatants.Where(x => x.IsSummoned && x.IsAlive).ToList())
        {
            if (!summon.TickSummonDuration())
                continue;

            summon.SetHealth(0);
            LogSummonExpired(summon, "expired");
        }
    }

    private static bool IsStatusLifecycleEvent(AbilityTriggerEvent triggerEvent) =>
        triggerEvent is AbilityTriggerEvent.OnStatusApplied or AbilityTriggerEvent.OnStatusExpired;

    private IEnumerable<RuntimeCombatantV2> SelectTargets(
        RuntimeCombatantV2 source,
        AbilityTargetSelectorV2 targetSelector,
        CombatEventV2 combatEvent,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        return targetSelector switch
        {
            AbilityTargetSelectorV2.Self => [source],
            AbilityTargetSelectorV2.Source => [source],
            AbilityTargetSelectorV2.EventSource => combatEvent.Source is null ? [] : [combatEvent.Source],
            AbilityTargetSelectorV2.EventTarget => combatEvent.Target is null ? [] : [combatEvent.Target],
            AbilityTargetSelectorV2.CurrentTarget => SelectFirstEnemy(source, combatants) is { } target ? [target] : [],
            AbilityTargetSelectorV2.RandomEnemy => SelectRandomEnemy(source, combatants) is { } target ? [target] : [],
            AbilityTargetSelectorV2.LowestHealthAlly => combatants.Where(x => x.Team == source.Team && x.IsAlive).OrderBy(x => x.Health).Take(1),
            AbilityTargetSelectorV2.AllEnemies => combatants.Where(x => x.Team != source.Team && x.IsAlive),
            AbilityTargetSelectorV2.AllAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive),
            AbilityTargetSelectorV2.EveryoneButSelf => combatants.Where(x => x.Id != source.Id && x.IsAlive),
            AbilityTargetSelectorV2.TwoEnemies => combatants.Where(x => x.Team != source.Team && x.IsAlive).Take(2),
            AbilityTargetSelectorV2.TwoAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive).Take(2),
            AbilityTargetSelectorV2.HighestMaxHealthAlly => combatants
                .Where(x => x.Team == source.Team && x.IsAlive)
                .OrderByDescending(x => x.GetAttribute(AttributeType.MaxHealth))
                .Take(1),
            AbilityTargetSelectorV2.SummonedAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive && x.IsSummoned),
            AbilityTargetSelectorV2.NonSummonedAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive && !x.IsSummoned),
            _ => []
        };
    }

    private bool EffectCanResolve(
        CompiledEffectV2 effect,
        RuntimeCombatantV2 source,
        IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        if (effect.Operation != AbilityEffectOperation.Summon || string.IsNullOrWhiteSpace(effect.SummonId))
            return true;

        return _summonsById.TryGetValue(effect.SummonId, out var summonDefinition)
            && !HasReachedSummonCap(source, summonDefinition, combatants);
    }

    private static bool HasReachedSummonCap(
        RuntimeCombatantV2 source,
        CompiledSummonV2 summonDefinition,
        IReadOnlyList<RuntimeCombatantV2> combatants) =>
        summonDefinition.MaxActive > 0
        && combatants.Count(x => x.IsAlive
            && x.IsSummoned
            && ReferenceEquals(x.SummonOwner, source)
            && x.Tags.Contains($"Summon.{summonDefinition.Id}")) >= summonDefinition.MaxActive;

    private void ExpireOwnedSummons(
        RuntimeCombatantV2 owner,
        IReadOnlyList<RuntimeCombatantV2> combatants,
        string reason)
    {
        foreach (var summon in combatants
                     .Where(x => x.IsAlive && x.IsSummoned && ReferenceEquals(x.SummonOwner, owner))
                     .ToList())
        {
            summon.SetHealth(0);
            LogSummonExpired(summon, reason);
        }
    }

    private void LogSummonExpired(RuntimeCombatantV2 summon, string reason)
    {
        var source = summon.SummonOwner ?? summon;
        Log(source, summon, summon.Name, EventType.SummonExpired, 0, $"{summon.Name} {reason}.");
    }

    private RuntimeCombatantV2? SelectFirstEnemy(RuntimeCombatantV2 source, IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        var enemies = combatants.Where(x => x.Team != source.Team && x.IsAlive).ToList();
        return enemies.FirstOrDefault(IsTaunting) ?? enemies.FirstOrDefault();
    }

    private RuntimeCombatantV2? SelectRandomEnemy(RuntimeCombatantV2 source, IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        var enemies = combatants.Where(x => x.Team != source.Team && x.IsAlive).ToList();
        var tauntingEnemies = enemies.Where(IsTaunting).ToList();
        if (tauntingEnemies.Count > 0)
            enemies = tauntingEnemies;

        return enemies.Count == 0 ? null : enemies[_random.Next(enemies.Count)];
    }

    private static bool IsTaunting(RuntimeCombatantV2 combatant) =>
        combatant.Statuses.Any(status =>
            status.Definition.Id.Equals("status.v2.taunt", StringComparison.OrdinalIgnoreCase)
            || status.Definition.Tags.Contains("Control.Taunt"));

    private bool ConditionsPass(
        IEnumerable<CompiledConditionV2> conditions,
        RuntimeCombatantV2 source,
        CombatEventV2 combatEvent) =>
        conditions.All(condition => ConditionPass(condition, source, combatEvent));

    private bool ConditionPass(CompiledConditionV2 condition, RuntimeCombatantV2 source, CombatEventV2 combatEvent)
    {
        var subject = ResolveSubject(condition.Subject, source, combatEvent);
        if (subject is null)
            return false;

        return condition.Type switch
        {
            AbilityConditionTypeV2.Always => true,
            AbilityConditionTypeV2.HealthBelowPercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 < condition.Value,
            AbilityConditionTypeV2.HealthAbovePercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 > condition.Value,
            AbilityConditionTypeV2.HasStatus => subject.GetStatusStacks(condition.StatusId!) > 0,
            AbilityConditionTypeV2.StatusStacksAtLeast => subject.GetStatusStacks(condition.StatusId!) >= condition.Value,
            AbilityConditionTypeV2.HasTag => subject.Tags.Contains(condition.Tag!),
            AbilityConditionTypeV2.ChancePercent => _random.Next(1, 101) <= condition.Value,
            _ => false
        };
    }

    private static RuntimeCombatantV2? ResolveSubject(
        AbilityConditionSubject subject,
        RuntimeCombatantV2 source,
        CombatEventV2 combatEvent) =>
        subject switch
        {
            AbilityConditionSubject.Source => source,
            AbilityConditionSubject.Target => combatEvent.Target,
            AbilityConditionSubject.EventSource => combatEvent.Source,
            AbilityConditionSubject.EventTarget => combatEvent.Target,
            _ => null
        };

    private static int CalculateValue(CompiledEffectV2 effect, RuntimeCombatantV2 source) =>
        Math.Max(AllowsNegativeValue(effect.Operation) ? int.MinValue : 0,
            (int)Math.Round(effect.BaseValue + (effect.ScalingAttribute is { } attribute
                ? source.GetAttribute(attribute) * effect.ScalingCoefficient
                : 0)));

    private static bool AllowsNegativeValue(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute or AbilityEffectOperation.ModifyStatusStacks;

    private static bool IsPeriodicEffect(CompiledEffectV2 effect) =>
        effect.IntervalTicks > 0 && effect.DurationTicks > 0;

    private void Log(
        RuntimeCombatantV2 source,
        RuntimeCombatantV2? target,
        string sourceName,
        EventType eventType,
        int magnitude,
        string details,
        string? statsSource = null,
        bool countsAsActivation = false)
    {
        _log.Add(new CombatLogItem
        {
            Source = sourceName,
            StatsSource = statsSource ?? string.Empty,
            CountsAsActivation = countsAsActivation,
            ActorId = source.Id,
            TargetId = target?.Id!,
            Timestamp = _currentTick,
            EventType = eventType,
            Magnitude = magnitude,
            Details = details,
            CombatEntity = target is null
                ? null
                : new SimpleCombatEntity
                {
                    Id = target.Id,
                    Name = target.Name,
                    ImagePath = target.ImagePath,
                    MaxHealth = (int)target.GetAttribute(AttributeType.MaxHealth),
                    Health = (int)target.Health,
                    Barrier = (int)target.Barrier
                }
        });
    }

    private static BattleOutcome DetermineOutcome(IReadOnlyList<RuntimeCombatantV2> combatants)
    {
        if (!HasLivingTeam(combatants, CombatTeamV2.Friendly))
            return BattleOutcome.Defeat;

        if (!HasLivingTeam(combatants, CombatTeamV2.Hostile))
            return BattleOutcome.Victory;

        return BattleOutcome.Draw;
    }

    private sealed record CombatEventV2(
        AbilityTriggerEvent Event,
        RuntimeCombatantV2? Source,
        RuntimeCombatantV2? Target,
        string? AbilityId);
}
