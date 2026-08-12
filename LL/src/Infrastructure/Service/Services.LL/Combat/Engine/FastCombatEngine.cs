using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Stats;

namespace Services.LL.Combat.Engine;

public sealed record FastCombatEngineOptions(
    int MaxTicks = 6000,
    int BasicAttackIntervalTicks = 30,
    int RandomSeed = 1337,
    bool StartActiveAbilitiesOnCooldown = false,
    float TauntThreatBonus = 100f,
    bool CaptureEventLog = true);

public sealed class FastCombatEngine
{
    public const int TicksPerSecond = 10;
    internal const double CombatMagnitudeVariance = 0.2d;
    private const int MagnitudeRandomSeedSalt = unchecked((int)0x9E3779B9);
    private const int HealthRegenerationIntervalSeconds = 5;
    private const int HealthRegenerationIntervalTicks =
        TicksPerSecond * HealthRegenerationIntervalSeconds;

    private readonly IReadOnlyDictionary<string, CompiledStatus> _statusesById;
    private readonly IReadOnlyDictionary<string, CompiledSummon> _summonsById;
    private readonly IReadOnlyDictionary<string, CompiledAbility> _abilitiesById;
    private readonly Random _random;
    private readonly Random _magnitudeRandom;
    private readonly int _maxTicks;
    private readonly int _basicAttackIntervalTicks;
    private readonly bool _startActiveAbilitiesOnCooldown;
    private readonly float _tauntThreatBonus;
    private readonly bool _captureEventLog;
    private readonly Dictionary<RuntimeCombatant, float> _basicAttackProgress = [];
    private readonly Dictionary<RuntimeCombatant, float> _healthRegenerationProgress = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationPotential = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationOverhealed = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationPulses = [];
    private readonly List<CombatLogItem> _log = [];
    private readonly Dictionary<string, int> _balanceDamageDone = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _balanceDamageTaken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RuntimeSummonGroup> _summonGroups = new(StringComparer.OrdinalIgnoreCase);
    private int _currentTick;
    private long _applicationOrder;
    private int _eventDepth;

    private enum DamageDelivery
    {
        Direct,
        Periodic,
        Reflected,
        Stored,
        Self
    }

    public FastCombatEngine(
        IReadOnlyDictionary<string, CompiledStatus> statusesById,
        FastCombatEngineOptions? options = null)
        : this(statusesById, new Dictionary<string, CompiledSummon>(), new Dictionary<string, CompiledAbility>(), options)
    {
    }

    public FastCombatEngine(
        IReadOnlyDictionary<string, CompiledStatus> statusesById,
        IReadOnlyDictionary<string, CompiledSummon> summonsById,
        IReadOnlyDictionary<string, CompiledAbility> abilitiesById,
        FastCombatEngineOptions? options = null)
    {
        var resolved = options ?? new FastCombatEngineOptions();
        _statusesById = statusesById;
        _summonsById = summonsById;
        _abilitiesById = abilitiesById;
        _random = new Random(resolved.RandomSeed);
        _magnitudeRandom = new Random(
            unchecked(resolved.RandomSeed ^ MagnitudeRandomSeedSalt));
        _maxTicks = resolved.MaxTicks;
        _basicAttackIntervalTicks = resolved.BasicAttackIntervalTicks;
        _startActiveAbilitiesOnCooldown = resolved.StartActiveAbilitiesOnCooldown;
        _tauntThreatBonus = Math.Max(0, resolved.TauntThreatBonus);
        _captureEventLog = resolved.CaptureEventLog;
    }

    public CombatResult Run(
        IReadOnlyList<RuntimeCombatant> friendly,
        IReadOnlyList<RuntimeCombatant> hostile,
        CancellationToken cancellationToken = default,
        Action<CombatCheckpoint>? checkpointObserver = null,
        int checkpointIntervalTicks = 0)
    {
        var combatants = friendly.Concat(hostile).ToList();
        foreach (var combatant in combatants)
        {
            _basicAttackProgress[combatant] = 0;
            _healthRegenerationProgress[combatant] = 0;
            InitializeActiveAbilityCooldowns(combatant);
        }

        Publish(new CombatEvent(AbilityTriggerEvent.OnCombatStart, null, null, null), combatants);
        var checkpointSequence = 0;
        var checkpointLogIndex = 0;
        if (checkpointObserver is not null && checkpointIntervalTicks > 0)
        {
            checkpointObserver(CreateCheckpoint(combatants, checkpointSequence++, checkpointLogIndex, false));
            checkpointLogIndex = _log.Count;
        }

        while (_currentTick < _maxTicks
               && HasLivingTeam(combatants, CombatTeam.Friendly)
               && HasLivingTeam(combatants, CombatTeam.Hostile))
        {
            if ((_currentTick & 63) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            PublishIntervalEvents(combatants);

            foreach (var combatant in combatants.Where(x => x.IsAlive).ToList())
            {
                if (IsActionBlocked(combatant) || !HasLivingOpponent(combatant, combatants))
                    continue;

                UseReadyActiveAbilities(combatant, combatants);

                if (combatant.CanBasicAttack && HasLivingOpponent(combatant, combatants))
                    TickBasicAttack(combatant, combatants);
            }

            TickEffects(combatants);
            TickStatuses(combatants);
            TickConditions(combatants);
            TickHealthRegeneration(combatants);
            TickBarrierContributions(combatants);

            foreach (var combatant in combatants)
                combatant.Tick();

            TickSummons(combatants);
            _currentTick++;

            if (checkpointObserver is not null
                && checkpointIntervalTicks > 0
                && _currentTick % checkpointIntervalTicks == 0)
            {
                var isFinalCheckpoint = _currentTick >= _maxTicks
                    || !HasLivingTeam(combatants, CombatTeam.Friendly)
                    || !HasLivingTeam(combatants, CombatTeam.Hostile);
                checkpointObserver(CreateCheckpoint(
                    combatants,
                    checkpointSequence++,
                    checkpointLogIndex,
                    isFinalCheckpoint));
                checkpointLogIndex = _log.Count;
            }
        }

        if (checkpointObserver is not null && checkpointIntervalTicks > 0)
        {
            if (_currentTick % checkpointIntervalTicks != 0)
            {
                checkpointObserver(CreateCheckpoint(
                    combatants,
                    checkpointSequence,
                    checkpointLogIndex,
                    true));
            }
        }

        var entityStats = _captureEventLog
            ? CreateDetailedStats(combatants)
            : CreateBalanceStats(combatants);

        return new CombatResult
        {
            EventLog = [.. _log],
            Duration = _currentTick,
            Outcome = DetermineOutcome(combatants),
            EntityStats = [.. entityStats]
        };
    }

    private CombatCheckpoint CreateCheckpoint(
        IReadOnlyList<RuntimeCombatant> combatants,
        int sequence,
        int logIndex,
        bool isFinal)
    {
        var intervalEvents = _log.Skip(logIndex).ToArray();
        var teams = combatants.ToDictionary(
            combatant => combatant.Id,
            combatant => combatant.Team.ToString(),
            StringComparer.OrdinalIgnoreCase);
        var entityStats = AddFinalCombatantState(
            AddHealthRegenerationTelemetry(
                new CombatStatsAggregator().Aggregate(_log, teams),
                combatants),
            combatants);
        return new CombatCheckpoint(
            sequence,
            _currentTick,
            combatants.Where(x => x.Team == CombatTeam.Friendly).Select(ToSimpleEntity).ToArray(),
            combatants.Where(x => x.Team == CombatTeam.Hostile).Select(ToSimpleEntity).ToArray(),
            entityStats,
            intervalEvents,
            isFinal);
    }

    private static SimpleCombatEntity ToSimpleEntity(RuntimeCombatant combatant) => new()
    {
        Id = combatant.Id,
        Name = combatant.Name,
        ImagePath = combatant.ImagePath,
        Health = (int)combatant.Health,
        MaxHealth = (int)combatant.GetAttribute(AttributeType.MaxHealth),
        Barrier = (int)combatant.Barrier
    };

    private IReadOnlyList<EntityStats> CreateDetailedStats(IReadOnlyList<RuntimeCombatant> combatants)
    {
        var teamsByEntityId = combatants.ToDictionary(
            combatant => combatant.Id,
            combatant => combatant.Team.ToString(),
            StringComparer.OrdinalIgnoreCase);
        return AddFinalCombatantState(
            AddHealthRegenerationTelemetry(
                new CombatStatsAggregator().Aggregate(_log, teamsByEntityId),
                combatants),
            combatants);
    }

    private IReadOnlyList<EntityStats> CreateBalanceStats(IReadOnlyList<RuntimeCombatant> combatants) =>
        combatants
            .Select(combatant => new EntityStats(
                combatant.Id,
                combatant.Name,
                [],
                DamageDone: _balanceDamageDone.GetValueOrDefault(combatant.Id),
                DamageTaken: _balanceDamageTaken.GetValueOrDefault(combatant.Id),
                Team: combatant.Team.ToString()))
            .ToList();

    private void PublishIntervalEvents(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var combatant in combatants.Where(x => x.IsAlive).ToList())
        {
            var hasAbilityListener =
                combatant.AbilityTriggersByEvent.ContainsKey(AbilityTriggerEvent.OnInterval);
            var hasStatusListener = combatant.Statuses.Any(
                status => status.Definition.TriggersByEvent.ContainsKey(AbilityTriggerEvent.OnInterval));
            if (!hasAbilityListener && !hasStatusListener)
                continue;

            Publish(
                new CombatEvent(
                    AbilityTriggerEvent.OnInterval,
                    combatant,
                    combatant,
                    null),
                combatants);
        }
    }

    private void UseReadyActiveAbilities(RuntimeCombatant actor, IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var ability in actor.Abilities.Where(x => x.Definition.Kind == AbilitySpecKind.Active && x.IsReady))
        {
            if (!HasLivingOpponent(actor, combatants)
                || !CanResolveActiveAbility(ability, actor, combatants)
                || !CanPayAbilityCosts(actor, ability.Definition))
            {
                continue;
            }

            var additionalCooldownTicks = PayAbilityCosts(actor, ability.Definition, combatants);
            ability.StartCooldown(
                actor.GetAttribute(AttributeType.Cooldown),
                additionalCooldownTicks);
            Log(actor, null, ability.Definition.Name, EventType.AbilityUse, 0, $"{actor.Name} used {ability.Definition.Name}");
            var primaryTarget = SelectActiveAbilityPrimaryTarget(ability, actor, combatants);
            Publish(new CombatEvent(AbilityTriggerEvent.OnAbilityUsed, actor, primaryTarget, ability.Definition.Id), combatants);
        }
    }

    private void TickBasicAttack(RuntimeCombatant actor, IReadOnlyList<RuntimeCombatant> combatants)
    {
        var threshold = GetBasicAttackChargeThreshold();
        var progress = _basicAttackProgress.GetValueOrDefault(actor) + GetBasicAttackRate(actor);
        if (progress < threshold)
        {
            _basicAttackProgress[actor] = progress;
            return;
        }

        _basicAttackProgress[actor] = progress - threshold;
        if (SelectFirstEnemy(actor, combatants) is not { } target)
            return;

        var baseDamage = Math.Max(
            1,
            (int)Math.Round(
                (1 + GetEffectivePower(actor) * AttributeCombatRules.BasicAttackPowerCoefficient) *
                actor.BasicAttackDamageMultiplier));
        var damage = Math.Max(1, ApplyCombatMagnitudeVariance(baseDamage));
        Log(actor, null, "Basic Attack", EventType.AbilityUse, 0, $"{actor.Name} used Basic Attack");
        Publish(new CombatEvent(AbilityTriggerEvent.OnBasicAttack, actor, target, "basic_attack"), combatants);
        var basicAttackModifiers = actor.ConsumeNextBasicAttackModifiers();
        damage = Math.Max(0, (int)Math.Round(damage * (1 + basicAttackModifiers.DamagePercent / 100f)));
        var healthDamage = ApplyDamage(
            actor,
            target,
            damage,
            actor.BasicAttackType,
            actor.BasicAttackDamageType,
            null,
            combatants,
            "Basic Attack",
            armorPenetrationBonus: basicAttackModifiers.ArmorPenetration);
        ApplyLifeSteal(actor, healthDamage, 0, combatants, "Basic Attack", "Basic Attack");
    }

    private static bool IsActionBlocked(RuntimeCombatant combatant) =>
        combatant.Statuses.Any(status => status.Stacks > 0 && status.Definition.Tags.Contains("Control.Stun"))
        || combatant.HasCondition(StandardConditionType.Stun)
        || combatant.HasCondition(StandardConditionType.Freeze);

    private static bool HasLivingOpponent(RuntimeCombatant actor, IReadOnlyList<RuntimeCombatant> combatants) =>
        combatants.Any(x => x.Team != actor.Team && x.IsAlive);

    private static bool HasLivingTeam(IReadOnlyList<RuntimeCombatant> combatants, CombatTeam team) =>
        combatants.Any(x => x.Team == team && x.IsAlive);

    private bool CanResolveActiveAbility(
        RuntimeAbility ability,
        RuntimeCombatant actor,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!ability.Definition.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers))
            return false;

        var combatEvent = new CombatEvent(AbilityTriggerEvent.OnAbilityUsed, actor, null, ability.Definition.Id);
        return triggers
            .Where(trigger => ConditionsPass(trigger.Conditions, actor, combatEvent, combatants))
            .SelectMany(trigger => trigger.Effects)
            .Any(effect => SelectTargets(actor, effect.Target, combatEvent, combatants)
                .Any(target => target.IsAlive
                    && EffectCanResolve(effect, actor, combatants)
                    && ConditionsPass(effect.Conditions, actor, combatEvent with { Target = target }, combatants)));
    }

    private static bool CanPayAbilityCosts(RuntimeCombatant actor, CompiledAbility ability)
    {
        foreach (var cost in ability.Costs)
        {
            var value = CalculateCostValue(cost, actor);
            if (value <= 0)
                continue;

            if (cost.Resource == AbilityResourceType.Health && actor.Health <= value)
                return false;

            if (cost.Resource == AbilityResourceType.Barrier && actor.Barrier < value)
                return false;

            if (cost.Resource == AbilityResourceType.Mana)
                return false;
        }

        return true;
    }

    private int PayAbilityCosts(
        RuntimeCombatant actor,
        CompiledAbility ability,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var additionalCooldownTicks = 0;
        var healthChanged = false;

        foreach (var cost in ability.Costs)
        {
            var value = CalculateCostValue(cost, actor);
            if (value <= 0)
                continue;

            switch (cost.Resource)
            {
                case AbilityResourceType.Health:
                    actor.AdjustHealth(-value);
                    healthChanged = true;
                    break;
                case AbilityResourceType.Barrier:
                    actor.ConsumeBarrier(value);
                    break;
                case AbilityResourceType.Cooldown:
                    additionalCooldownTicks += value;
                    break;
                case AbilityResourceType.Mana:
                    throw new InvalidOperationException(
                        $"Ability '{ability.Id}' requires Mana, but combat mana is not implemented.");
                default:
                    throw new NotSupportedException($"Unsupported ability cost resource '{cost.Resource}'.");
            }
        }

        if (healthChanged)
            Publish(new CombatEvent(AbilityTriggerEvent.OnHealthChanged, actor, actor, null), combatants);

        return additionalCooldownTicks;
    }

    private void Publish(CombatEvent combatEvent, IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (_eventDepth >= 64)
            throw new InvalidOperationException("Combat event recursion exceeded the maximum depth of 64.");

        _eventDepth++;
        try
        {
            foreach (var combatant in combatants
                         .Where(x => x.IsAlive
                                     || (combatEvent.Event == AbilityTriggerEvent.OnDeath
                                         && ReferenceEquals(x, combatEvent.Source)))
                         .ToList())
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

                        if (!IsSourceScopedTriggerRelevant(combatant, combatEvent))
                            continue;

                        foreach (var trigger in ability.Definition.TriggersByEvent[combatEvent.Event])
                        {
                            if (!ability.CanUseTrigger(trigger, _currentTick)
                                || !ConditionsPass(trigger.Conditions, combatant, combatEvent, combatants))
                                continue;

                            ability.StartTriggerCooldown(trigger);
                            ability.BeginTriggerExecution(trigger);
                            try
                            {
                                ExecuteTrigger(
                                    trigger,
                                    combatant,
                                    combatEvent,
                                    combatants,
                                    ability.CanUseEffect,
                                    ability.MarkEffectUsed,
                                    countStatsActivation: ability.Definition.Kind == AbilitySpecKind.Passive);
                            }
                            finally
                            {
                                ability.EndTriggerExecution(trigger);
                            }
                        }
                    }
                }

                foreach (var status in combatant.Statuses.ToList())
                {
                    if (!status.Definition.TriggersByEvent.TryGetValue(combatEvent.Event, out var triggers))
                        continue;

                    if (!IsSourceScopedTriggerRelevant(status.Owner, combatEvent))
                        continue;

                    foreach (var trigger in triggers)
                    {
                        if (IsStatusLifecycleEvent(combatEvent.Event)
                            && (!string.Equals(combatEvent.AbilityId, status.Definition.Id, StringComparison.OrdinalIgnoreCase)
                                || !ReferenceEquals(combatEvent.Target, status.Owner)))
                        {
                            continue;
                        }

                        if (!status.CanUseTrigger(trigger, _currentTick)
                            || !ConditionsPass(trigger.Conditions, status.Source, combatEvent, combatants))
                            continue;

                        status.StartTriggerCooldown(trigger);
                        status.BeginTriggerExecution(trigger);
                        try
                        {
                            ExecuteTrigger(
                                trigger,
                                status.Source,
                                combatEvent,
                                combatants,
                                status.CanUseEffect,
                                status.MarkEffectUsed,
                                status.StatsSource,
                                countStatsActivation: false,
                                durationMultiplier: CalculateStatusEffectDurationMultiplier(status));
                        }
                        finally
                        {
                            status.EndTriggerExecution(trigger);
                        }
                    }
                }
            }
        }
        finally
        {
            _eventDepth--;
        }
    }

    private void ExecuteTrigger(
        CompiledTrigger trigger,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        Func<CompiledEffect, bool> canUseEffect,
        Action<CompiledEffect> markEffectUsed,
        string? statsSourceOverride = null,
        bool countStatsActivation = false,
        double durationMultiplier = 1d)
    {
        var activationCounted = false;
        var executionContext = new EffectExecutionContext();
        foreach (var effect in trigger.Effects)
        {
            if (!canUseEffect(effect))
                continue;

            // Effects such as Summon can append combatants while resolving this target set.
            // Snapshot it so one cast keeps its originally selected targets and does not
            // invalidate the underlying list enumerator.
            foreach (var target in SelectTargets(source, effect.Target, combatEvent, combatants).ToArray())
            {
                if (!canUseEffect(effect))
                    break;

                if (!target.IsAlive
                    || !ConditionsPass(effect.Conditions, source, combatEvent with { Target = target }, combatants))
                    continue;

                if (effect.Operation != AbilityEffectOperation.ApplyRandomCondition
                    && !IsPeriodicEffect(effect)
                    && effect.ChancePercent < 100
                    && _random.Next(1, 101) > effect.ChancePercent)
                    continue;

                var countThisActivation = countStatsActivation && !activationCounted;
                markEffectUsed(effect);
                ExecuteEffect(
                    effect,
                    source,
                    target,
                    combatants,
                    combatEvent,
                    statsSourceOverride,
                    countThisActivation,
                    durationMultiplier,
                    executionContext);
                if (countThisActivation)
                    activationCounted = true;
            }
        }
    }

    private void ExecuteEffect(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatEvent? combatEvent = null,
        string? statsSourceOverride = null,
        bool countStatsActivation = false,
        double durationMultiplier = 1d,
        EffectExecutionContext? executionContext = null)
    {
        var statsSource = statsSourceOverride ?? effect.StatsSource;
        if (effect.IntervalTicks > 0 && effect.DurationTicks > 0)
        {
            target.ActiveEffects.Add(
                new RuntimeEffect(
                    effect,
                    source,
                    target,
                    statsSource,
                    durationMultiplier,
                    executionContext?.ActivationId));
            if (effect.Operation == AbilityEffectOperation.Heal)
            {
                Publish(
                    new CombatEvent(AbilityTriggerEvent.OnHeal, source, target, effect.Id),
                    combatants);
            }

            return;
        }

        ApplyEffectOnce(
            effect,
            source,
            target,
            combatants,
            combatEvent,
            statsSource,
            countStatsActivation,
            executionContext);

        if (effect.DurationTicks > 0 && IsTimedModifierOperation(effect.Operation))
            target.ActiveEffects.Add(new RuntimeEffect(effect, source, target, statsSource, durationMultiplier));
    }

    private void ApplyEffectOnce(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatEvent? combatEvent = null,
        string? statsSourceOverride = null,
        bool countStatsActivation = false,
        EffectExecutionContext? executionContext = null)
    {
        var value = CalculateValue(effect, source, combatEvent);
        if (effect.ScalingAttribute == AttributeType.Power
            && effect.Operation is AbilityEffectOperation.Damage or AbilityEffectOperation.Heal)
        {
            value = ApplyCombatMagnitudeVariance(value);
        }

        var statsSource = statsSourceOverride ?? effect.StatsSource;

        switch (effect.Operation)
        {
            case AbilityEffectOperation.Damage:
                var delivery = IsPeriodicEffect(effect) || effect.AttackType == AttackType.DamageOverTime
                    ? DamageDelivery.Periodic
                    : effect.Tags.Contains("Damage.Secondary")
                        ? DamageDelivery.Stored
                    : ReferenceEquals(source, target)
                        ? DamageDelivery.Self
                        : DamageDelivery.Direct;
                var healthDamage = ApplyDamage(
                    source,
                    target,
                    value,
                    effect.AttackType,
                    effect.DamageType,
                    effect,
                    combatants,
                    effect.Id,
                    statsSource,
                    countStatsActivation,
                    delivery);
                if (delivery == DamageDelivery.Direct)
                    ApplyLifeSteal(effect, source, healthDamage, combatants, statsSource);
                break;
            case AbilityEffectOperation.Heal:
                RestoreHealth(
                    source,
                    target,
                    value,
                    combatants,
                    effect.Id,
                    statsSource,
                    isLifeSteal: false,
                    effect,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.GrantBarrier:
                var grantedBarrier = CanCrit(effect, AbilityEffectOperation.GrantBarrier)
                                     && RollCriticalStrike(source, effect.CritChanceBonus)
                    ? ApplyCriticalMultiplier(source, value)
                    : value;
                GrantBarrier(
                    source,
                    target,
                    grantedBarrier,
                    effect,
                    statsSource,
                    countStatsActivation,
                    combatants,
                    executionContext?.ActivationId);
                break;
            case AbilityEffectOperation.RestoreResource:
                if (effect.Resource == AbilityResourceType.Cooldown)
                {
                    target.ReduceAbilityCooldowns(value);
                    Log(source, target, effect.Id, EventType.Buff, value, $"{source.Name} restored {value} cooldown ticks to {target.Name}.", statsSource, countStatsActivation);
                }
                else if (effect.Resource == AbilityResourceType.Barrier)
                {
                    var restoredBarrier = CanCrit(effect, AbilityEffectOperation.GrantBarrier)
                                          && RollCriticalStrike(source, effect.CritChanceBonus)
                        ? ApplyCriticalMultiplier(source, value)
                        : value;
                    GrantBarrier(
                        source,
                        target,
                        restoredBarrier,
                        effect,
                        statsSource,
                        countStatsActivation,
                        combatants,
                        executionContext?.ActivationId);
                }
                else
                {
                    RestoreHealth(
                        source,
                        target,
                        value,
                        combatants,
                        effect.Id,
                        statsSource,
                        isLifeSteal: false,
                        effect,
                        countStatsActivation);
                }

                break;
            case AbilityEffectOperation.ApplyStatus:
                ApplyStatus(source, target, effect.StatusId!, Math.Max(1, value), combatants, statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ApplyCondition:
                ApplyCondition(
                    source,
                    target,
                    effect.Condition!.Value,
                    Math.Max(1, value),
                    effect.DurationTicks,
                    combatants,
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyStatusStacks:
                ModifyStatusStacks(source, target, effect.StatusId!, value, combatants);
                break;
            case AbilityEffectOperation.RemoveStatus:
                if (RemoveStatus(source, target, effect.StatusId!, combatants))
                {
                    Log(source, target, effect.Id, EventType.StatusEffectRemoved, 0, $"{target.Name} lost {effect.StatusId}.", statsSource, countStatsActivation);
                }
                break;
            case AbilityEffectOperation.ApplyRandomCondition:
                var selectedCondition = _random.Next(1, 101) <= effect.ChancePercent
                    ? effect.Condition!.Value
                    : effect.AlternativeCondition!.Value;
                ApplyCondition(
                    source,
                    target,
                    selectedCondition,
                    Math.Max(1, value),
                    effect.DurationTicks,
                    combatants,
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.Cleanse:
                CleanseStatuses(source, target, combatants);
                CleanseConditions(source, target, effect.Condition, combatants);
                Log(source, target, effect.Id, EventType.StatusEffectCleansed, 0, $"{target.Name} was cleansed.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.Dispel:
                DispelConditions(source, target, effect.Condition, combatants);
                Log(source, target, effect.Id, EventType.StatusEffectDispelled, 0, $"{target.Name} was dispelled.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyAttribute:
                target.AdjustAttribute(effect.Attribute!.Value, value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s {effect.Attribute} changed by {value}.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ConsumeConditionStacks:
                ResolveConditionConsumption(
                    effect,
                    source,
                    target,
                    combatants,
                    statsSource,
                    countStatsActivation,
                    executionContext ?? new EffectExecutionContext());
                break;
            case AbilityEffectOperation.RemoveCondition:
                RemoveConditionInstances(
                    source,
                    target,
                    effect.Condition!.Value,
                    int.MaxValue,
                    ConditionRemovalReason.Removed,
                    combatants);
                break;
            case AbilityEffectOperation.ModifyAttributePercentOfInitial:
                var initialAttributeChange = (int)Math.Round(
                    target.GetInitialAttribute(effect.Attribute!.Value) * effect.ScalingCoefficient);
                target.AdjustAttribute(effect.Attribute.Value, initialAttributeChange);
                Log(
                    source,
                    target,
                    effect.Id,
                    initialAttributeChange >= 0 ? EventType.Buff : EventType.Debuff,
                    initialAttributeChange,
                    $"{target.Name}'s {effect.Attribute} changed by {initialAttributeChange}.",
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.TransferAttributePercent:
                var transferred = Math.Max(
                    0,
                    (int)Math.Round(target.GetAttribute(effect.Attribute!.Value) * effect.ScalingCoefficient));
                transferred = Math.Min(transferred, Math.Max(0, (int)Math.Floor(target.GetAttribute(effect.Attribute.Value))));
                target.AdjustAttribute(effect.Attribute.Value, -transferred);
                source.AdjustAttribute(effect.Attribute.Value, transferred);
                Log(
                    source,
                    target,
                    effect.Id,
                    EventType.Debuff,
                    transferred,
                    $"{source.Name} absorbed {transferred} {effect.Attribute} from {target.Name}.",
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyThreat:
                target.AdjustThreat(value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s Threat changed by {value}.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyRegenerationRate:
                target.AdjustRegenerationRate(value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s Regeneration Rate changed by {value}%.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyRegenerationInterval:
                target.AdjustRegenerationInterval(value);
                Log(source, target, effect.Id, value <= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s Regeneration Interval changed by {value} ticks.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyHealingReceived:
                target.AdjustHealingReceived(value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s healing received changed by {value}%.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyDamageDealt:
                target.AdjustDamageDealt(effect.DamageType, value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s {effect.DamageType} damage dealt changed by {value}%.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyDamageTaken:
                target.AdjustDamageTaken(effect.DamageType, value);
                Log(source, target, effect.Id, value <= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s {effect.DamageType} damage taken changed by {value}%.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyDamageTakenFromCondition:
                target.AdjustDamageTakenFromCondition(effect.Condition!.Value, value);
                Log(source, target, effect.Id, value <= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s damage taken from {effect.Condition} attackers changed by {value}%.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyNextBasicAttackDamage:
                target.ModifyNextBasicAttackDamage(value);
                Log(source, target, effect.Id, EventType.Buff, value, $"{target.Name}'s current Basic Attack damage changed by {value}%.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyNextBasicAttackArmorPenetration:
                target.ModifyNextBasicAttackArmorPenetration(value);
                Log(source, target, effect.Id, EventType.Buff, value, $"{target.Name}'s current Basic Attack gained {value}% Armor Penetration.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.Summon:
                SummonCombatant(
                    source,
                    effect,
                    combatants,
                    statsSource,
                    countStatsActivation,
                    executionContext ?? new EffectExecutionContext());
                break;
            case AbilityEffectOperation.SelfDestruct:
                target.SetHealth(0);
                Log(source, target, effect.Id, EventType.Death, 0, $"{target.Name} self-destructed.", statsSource, countStatsActivation);
                NotifySummonChanged(target, combatants);
                ExpireOwnedSummons(target, combatants, "owner death");
                break;
            case AbilityEffectOperation.SynchronizeAttributePerOwnedSummon:
                SynchronizeAttributePerOwnedSummon(
                    effect,
                    source,
                    target,
                    combatants,
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.ConsumeOwnedSummon:
                ConsumeOwnedSummon(
                    effect,
                    source,
                    target,
                    value,
                    combatants,
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.SynchronizeAttributePerStatusStack:
                SynchronizeAttributePerStatusStack(
                    effect,
                    source,
                    target,
                    statsSource,
                    countStatsActivation);
                break;
            default:
                throw new NotSupportedException($"Unsupported ability operation '{effect.Operation}'.");
        }
    }

    private int ApplyDamage(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int damage,
        AttackType attackType,
        DamageType damageType,
        CompiledEffect? effect,
        IReadOnlyList<RuntimeCombatant> combatants,
        string sourceName,
        string? statsSource = null,
        bool countStatsActivation = false,
        DamageDelivery delivery = DamageDelivery.Direct,
        float armorPenetrationBonus = 0)
    {
        if (!target.IsAlive || damage <= 0)
            return 0;

        if (delivery == DamageDelivery.Direct
            && CanDodge(attackType)
            && target.GetAttribute(AttributeType.DodgeChance) > 0)
        {
            var dodgeChance = Math.Clamp(
                target.GetAttribute(AttributeType.DodgeChance),
                0,
                AttributeCatalog.GetFixedCap(AttributeType.DodgeChance));
            if (_random.NextDouble() * 100 < dodgeChance)
            {
                Log(
                    source,
                    target,
                    sourceName,
                    EventType.Miss,
                    0,
                    $"{source.Name} missed {target.Name}.",
                    statsSource,
                    countStatsActivation,
                    incomingRawDamage: damage,
                    avoidedDamage: damage);
                Publish(new CombatEvent(AbilityTriggerEvent.OnDodge, target, source, null), combatants);
                return 0;
            }
        }

        var damageModifier = source.GetDamageDealtPercent(damageType)
                             + target.GetDamageTakenPercent(damageType, source);
        damage = Math.Max(0, (int)Math.Round(damage * Math.Max(0, 1 + damageModifier / 100f)));
        var isCritical = delivery == DamageDelivery.Direct
                         && CanCrit(effect, AbilityEffectOperation.Damage)
                         && RollCriticalStrike(source, effect?.CritChanceBonus ?? 0);
        var criticalDamage = isCritical
            ? ApplyCriticalMultiplier(source, damage)
            : damage;
        var vulnerableDamage = criticalDamage;
        if (delivery == DamageDelivery.Direct
            && criticalDamage > 0
            && TryConsumeConditionCharge(target, StandardConditionType.Vulnerable, source, combatants))
        {
            vulnerableDamage = ApplyVulnerable(criticalDamage);
        }
        var vulnerableAmplified = Math.Max(0, vulnerableDamage - criticalDamage);
        var typedDamage = ApplyTypedDefense(
            source,
            target,
            vulnerableDamage,
            damageType,
            (effect?.ArmorPenetrationBonus ?? 0) + armorPenetrationBonus);
        var typedMitigationPrevented = Math.Max(0, vulnerableDamage - typedDamage);
        var physicalMitigationPrevented = damageType is DamageType.Physical or DamageType.Bleed
            ? typedMitigationPrevented
            : 0;
        var magicalMitigationPrevented = damageType is DamageType.Magical
            or DamageType.Burn
            or DamageType.Poison
            or DamageType.Shadow
                ? typedMitigationPrevented
                : 0;
        var blocked = delivery == DamageDelivery.Direct && CanBlock(attackType) && RollBlock(target);
        var blockedDamage = blocked
            ? Math.Max(
                0,
                (int)Math.Round(
                    typedDamage
                    * (1 - AttributeCombatRules.BlockDamageReductionPercent / 100f)))
            : typedDamage;
        var blockPrevented = Math.Max(0, typedDamage - blockedDamage);
        var reducedDamage = ApplyDamageReduction(target, blockedDamage);
        var damageReductionPrevented = Math.Max(0, blockedDamage - reducedDamage);
        var guardedDamage = reducedDamage;
        if (delivery == DamageDelivery.Direct
            && reducedDamage > 0
            && TryConsumeConditionCharge(target, StandardConditionType.Guard, source, combatants))
        {
            guardedDamage = Math.Max(0, (int)Math.Round(reducedDamage * 0.75f));
        }
        var damageAmplified = vulnerableAmplified + Math.Max(0, reducedDamage - blockedDamage);
        var barrierBefore = target.Barrier;
        var barrierConsumption = target.ConsumeBarrierWithSources(guardedDamage);
        var barrierAbsorbed = (int)barrierConsumption.Total;
        if (barrierAbsorbed > 0)
        {
            foreach (var contribution in barrierConsumption.Contributions)
            {
                var barrierSource = contribution.Source ?? target;
                var contributionAmount = Math.Max(0, (int)Math.Round(contribution.Amount));
                Log(
                    barrierSource,
                    target,
                    "Barrier",
                    EventType.BarrierAbsorbed,
                    contributionAmount,
                    $"{contributionAmount} barrier from {barrierSource.Name} was consumed on {target.Name}.");
                Publish(
                    new CombatEvent(
                        AbilityTriggerEvent.OnBarrierAbsorbed,
                        barrierSource,
                        target,
                        null,
                        contributionAmount,
                        Instigator: source,
                        BarrierApplicationOrder: contribution.ApplicationOrder),
                    combatants);

                if (contribution.IsDepleted && !string.IsNullOrWhiteSpace(contribution.EffectId))
                {
                    RemoveLinkedActiveEffects(
                        contribution.ActivationId,
                        contribution.LinkedEffectId,
                        combatants);
                    Publish(
                        new CombatEvent(
                            AbilityTriggerEvent.OnBarrierContributionBroken,
                            barrierSource,
                            target,
                            contribution.EffectId,
                            Instigator: source,
                            BarrierApplicationOrder: contribution.ApplicationOrder),
                        combatants);
                }
            }

            if (barrierBefore > 0 && target.Barrier <= 0)
            {
                var finalContribution = barrierConsumption.Contributions[^1];
                var barrierSource = finalContribution.Source ?? target;
                Log(barrierSource, target, "Barrier", EventType.BarrierBroken, 0, $"{target.Name}'s barrier broke.");
                Publish(
                    new CombatEvent(
                        AbilityTriggerEvent.OnBarrierBroken,
                        barrierSource,
                        target,
                        null,
                        Instigator: source,
                        BarrierApplicationOrder: finalContribution.ApplicationOrder),
                    combatants);
            }
        }
        var pendingHealthDamage = Math.Max(0, guardedDamage - barrierAbsorbed);
        var healthBefore = target.Health;
        target.AdjustHealth(-pendingHealthDamage);
        var healthDamage = Math.Max(0, (int)Math.Round(healthBefore - target.Health));
        TrackBalanceDamage(source, target, healthDamage, delivery);

        Log(
            source,
            target,
            sourceName,
            delivery switch
            {
                DamageDelivery.Periodic => EventType.Damage,
                DamageDelivery.Reflected => EventType.ReflectedDamage,
                _ => isCritical ? EventType.DamageCrit : EventType.Damage
            },
            healthDamage,
            $"{source.Name} dealt {healthDamage} {damageType} damage to {target.Name}{(isCritical ? " (critical)" : string.Empty)}{(blocked ? " (blocked)" : string.Empty)}.",
            statsSource,
            countStatsActivation,
            barrierAbsorbed,
            criticalDamage,
            avoidedDamage: 0,
            typedMitigationPrevented,
            physicalMitigationPrevented,
            magicalMitigationPrevented,
            blockPrevented,
            damageReductionPrevented,
            damageAmplified,
            pendingHealthDamage);
        if (delivery == DamageDelivery.Direct)
        {
            var directEvent = new CombatEvent(
                AbilityTriggerEvent.OnHit,
                source,
                target,
                sourceName.Equals("Basic Attack", StringComparison.Ordinal) ? "basic_attack" : effect?.Id,
                healthDamage,
                DamageType: damageType,
                AttackType: attackType,
                WasCritical: isCritical,
                WasDirectHit: true);
            Publish(directEvent, combatants);
            PublishAttackTypeEvents(source, target, attackType, combatants, directEvent);
            Publish(directEvent with { Event = AbilityTriggerEvent.OnDamaged, Source = target, Target = source }, combatants);
            Publish(directEvent with { Event = AbilityTriggerEvent.OnAttacked, Source = target, Target = source }, combatants);
        }
        if (healthDamage > 0)
            Publish(new CombatEvent(AbilityTriggerEvent.OnHealthChanged, target, source, null), combatants);

        if (delivery == DamageDelivery.Direct
            && healthDamage > 0
            && !ReferenceEquals(source, target)
            && source.IsAlive)
        {
            ResolveThorns(target, source, healthDamage, combatants);
        }

        if (!target.IsAlive)
        {
            Log(source, target, sourceName, EventType.Death, 0, $"{target.Name} was killed by {source.Name}.", statsSource);
            var deathEvent = new CombatEvent(
                AbilityTriggerEvent.OnKill,
                source,
                target,
                null,
                healthDamage,
                DamageType: damageType,
                AttackType: attackType,
                WasCritical: isCritical,
                WasDirectHit: delivery == DamageDelivery.Direct);
            Publish(deathEvent, combatants);
            Publish(deathEvent with { Event = AbilityTriggerEvent.OnDeath, Source = target, Target = source }, combatants);
            NotifySummonChanged(target, combatants);
            ExpireOwnedSummons(target, combatants, "owner death");
        }

        return healthDamage;
    }

    private static bool CanDodge(AttackType attackType) =>
        attackType is AttackType.Melee or AttackType.Ranged;

    private static bool CanBlock(AttackType attackType) =>
        attackType is AttackType.Melee or AttackType.Ranged;

    private static int ApplyDamageReduction(RuntimeCombatant target, int damage)
    {
        var reduction = Math.Clamp(
            target.GetAttribute(AttributeType.DamageReduction),
            -100,
            AttributeCatalog.GetFixedCap(AttributeType.DamageReduction));
        return Math.Max(0, (int)Math.Round(damage * (1 - reduction / 100f)));
    }

    private static int ApplyVulnerable(int damage) =>
        Math.Max(
            0,
            (int)Math.Min(
                int.MaxValue,
                Math.Round(damage * 1.25d)));

    private void ResolveThorns(
        RuntimeCombatant defender,
        RuntimeCombatant attacker,
        int healthDamage,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var reflectedPercent = defender.Conditions
            .Where(x => x.Type == StandardConditionType.Thorns)
            .Sum(x => (long)x.Value);
        if (reflectedPercent <= 0)
            return;

        var reflectedDamage = Math.Max(
            0,
            (int)Math.Min(
                int.MaxValue,
                Math.Round(
                    healthDamage * reflectedPercent / 100d,
                    MidpointRounding.AwayFromZero)));
        if (reflectedDamage <= 0)
            return;

        ApplyDamage(
            defender,
            attacker,
            reflectedDamage,
            AttackType.None,
            DamageType.None,
            null,
            combatants,
            GetConditionId(StandardConditionType.Thorns),
            "Thorns",
            delivery: DamageDelivery.Reflected);
    }

    private void GrantBarrier(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int requested,
        CompiledEffect effect,
        string? statsSource,
        bool countStatsActivation,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? activationId)
    {
        var applicationOrder = ++_applicationOrder;
        var accepted = target.GrantBarrier(
            source,
            requested,
            applicationOrder,
            effect.Id,
            effect.DurationTicks,
            activationId,
            effect.LinkedEffectId);
        var granted = Math.Max(0, (int)Math.Round(accepted));
        if (granted <= 0)
            return;

        Log(
            source,
            target,
            effect.Id,
            EventType.RestoreBarrier,
            granted,
            $"{source.Name} granted {granted} barrier to {target.Name}.",
            statsSource,
            countStatsActivation);
        Publish(
            new CombatEvent(
                AbilityTriggerEvent.OnBarrierApplied,
                source,
                target,
                null,
                granted,
                BarrierApplicationOrder: applicationOrder),
            combatants);
    }

    private bool RollBlock(RuntimeCombatant target)
    {
        var blockChance = Math.Clamp(
            target.GetAttribute(AttributeType.BlockChance),
            0,
            AttributeCatalog.GetFixedCap(AttributeType.BlockChance));
        return blockChance > 0 && _random.NextDouble() * 100 < blockChance;
    }

    private bool RollCriticalStrike(RuntimeCombatant source, float bonusChance = 0)
    {
        var critChance = Math.Clamp(
            source.GetAttribute(AttributeType.CritChance) + bonusChance,
            0,
            AttributeCatalog.GetFixedCap(AttributeType.CritChance));
        return critChance > 0 && _random.NextDouble() * 100 < critChance;
    }

    private static int ApplyCriticalMultiplier(RuntimeCombatant source, int value)
    {
        var multiplier = 1 + Math.Max(0, source.GetAttribute(AttributeType.CritDamage)) / 100f;
        return Math.Max(0, (int)Math.Round(value * multiplier));
    }

    private static bool CanCrit(CompiledEffect? effect, AbilityEffectOperation operation)
    {
        if (effect?.CritEligibility == CritEligibility.Allowed)
            return true;

        if (effect?.CritEligibility == CritEligibility.Disallowed)
            return false;

        return operation switch
        {
            AbilityEffectOperation.Heal =>
                effect is null || !IsPeriodicEffect(effect),
            AbilityEffectOperation.Damage =>
                effect is null
                || (!IsPeriodicEffect(effect)
                    && effect.AttackType != AttackType.DamageOverTime
                    && effect.AbilityKind == AbilitySpecKind.Active),
            _ => false
        };
    }

    private static int ApplyTypedDefense(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int damage,
        DamageType damageType,
        float penetrationBonus = 0)
    {
        var (defenseAttribute, penetrationAttribute) = damageType switch
        {
            DamageType.Physical or DamageType.Bleed =>
                (AttributeType.Armor, AttributeType.ArmorPenetration),
            DamageType.Magical or DamageType.Burn or DamageType.Poison or DamageType.Shadow =>
                (AttributeType.Resistance, AttributeType.MagicPenetration),
            _ => ((AttributeType?)null, (AttributeType?)null)
        };

        if (defenseAttribute is null || penetrationAttribute is null)
            return damage;

        var corrosionStacks = Math.Min(
            50,
            target.GetConditionStacks(StandardConditionType.Corrosion));
        var corrodedDefense = Math.Max(
            0,
            target.GetAttribute(defenseAttribute.Value) * (1 - corrosionStacks / 100f));
        var mitigation = AttributeCombatRules.CalculateDefenseMitigation(
            corrodedDefense,
            source.GetAttribute(penetrationAttribute.Value) + penetrationBonus);
        return Math.Max(0, (int)Math.Round(damage * (1 - mitigation)));
    }

    private void PublishAttackTypeEvents(
        RuntimeCombatant source,
        RuntimeCombatant target,
        AttackType attackType,
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatEvent combatEvent)
    {
        switch (attackType)
        {
            case AttackType.Melee:
                Publish(combatEvent with { Event = AbilityTriggerEvent.OnMeleeAttack }, combatants);
                Publish(combatEvent with { Event = AbilityTriggerEvent.OnMeleeAttacked, Source = target, Target = source }, combatants);
                break;
            case AttackType.Ranged:
                Publish(combatEvent with { Event = AbilityTriggerEvent.OnRangedAttack }, combatants);
                Publish(combatEvent with { Event = AbilityTriggerEvent.OnRangedAttacked, Source = target, Target = source }, combatants);
                break;
        }
    }

    private void RestoreHealth(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int value,
        IReadOnlyList<RuntimeCombatant> combatants,
        string sourceName,
        string? statsSource,
        bool isLifeSteal,
        CompiledEffect? effect = null,
        bool countStatsActivation = false,
        bool applyHealingModifiers = true)
    {
        var healingPowerMultiplier = applyHealingModifiers
            ? Math.Max(0, 1 + source.GetAttribute(AttributeType.HealingPowerPercent) / 100f)
            : 1f;
        var modifiedValue = Math.Max(0, (int)Math.Round(value * healingPowerMultiplier));
        var isCritical = applyHealingModifiers
                         && !isLifeSteal
                         && CanCrit(effect, AbilityEffectOperation.Heal)
                         && RollCriticalStrike(source);
        if (isCritical)
            modifiedValue = ApplyCriticalMultiplier(source, modifiedValue);

        modifiedValue = ApplyHealingReceivedModifier(target, modifiedValue);
        var before = target.Health;
        target.AdjustHealth(modifiedValue);
        var restored = Math.Max(0, (int)Math.Round(target.Health - before));
        Log(
            source,
            target,
            sourceName,
            isCritical ? EventType.HealCrit : EventType.Heal,
            restored,
            $"{source.Name} healed {target.Name} for {restored}{(isCritical ? " (critical)" : string.Empty)}.",
            statsSource,
            countStatsActivation);

        if (restored <= 0)
            return;

        if (effect is null || !IsPeriodicEffect(effect))
            Publish(new CombatEvent(AbilityTriggerEvent.OnHeal, source, target, null), combatants);
        Publish(new CombatEvent(AbilityTriggerEvent.OnHealed, target, source, null), combatants);
        Publish(new CombatEvent(AbilityTriggerEvent.OnHealthChanged, target, source, null), combatants);

        if (isLifeSteal)
            Publish(new CombatEvent(AbilityTriggerEvent.OnLifestealHeal, source, target, null), combatants);
    }

    private void ApplyLifeSteal(
        CompiledEffect effect,
        RuntimeCombatant source,
        int healthDamage,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource)
    {
        ApplyLifeSteal(
            source,
            healthDamage,
            effect.LifeStealPercentage,
            combatants,
            effect.Id,
            statsSource,
            effect);
    }

    private void ApplyLifeSteal(
        RuntimeCombatant source,
        int healthDamage,
        float effectPercentage,
        IReadOnlyList<RuntimeCombatant> combatants,
        string sourceName,
        string? statsSource,
        CompiledEffect? effect = null)
    {
        var lifeStealPercentage = Math.Max(
            source.GetAttribute(AttributeType.LifeSteal) + effectPercentage,
            0);
        if (lifeStealPercentage <= 0 || healthDamage <= 0)
            return;

        var healing = (int)Math.Round(healthDamage * (lifeStealPercentage / 100f));
        if (healing <= 0)
            return;

        RestoreHealth(
            source,
            source,
            healing,
            combatants,
            sourceName,
            statsSource,
            isLifeSteal: true,
            effect);
    }

    private void ApplyStatus(
        RuntimeCombatant source,
        RuntimeCombatant target,
        string statusId,
        int stacks,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource = null,
        bool countStatsActivation = false)
    {
        if (!_statusesById.TryGetValue(statusId, out var statusDefinition))
            throw new InvalidOperationException($"Status '{statusId}' has not been compiled.");

        var isControl = statusDefinition.Tags.Any(tag =>
            tag.StartsWith("Control.", StringComparison.OrdinalIgnoreCase));
        var isHarmful = isControl || statusDefinition.Tags.Any(tag =>
            tag.StartsWith("Debuff", StringComparison.OrdinalIgnoreCase)
            || tag.StartsWith("Affliction", StringComparison.OrdinalIgnoreCase));
        if (isControl && target.HasCondition(StandardConditionType.Unstoppable))
            return;

        if (isHarmful && TryConsumeConditionCharge(target, StandardConditionType.Ward, source, combatants))
            return;

        var existing = target.Statuses.FirstOrDefault(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (statusDefinition.StackingPolicy == AbilityStatusStackingPolicy.Replace)
                RemoveStatusInstance(
                    source,
                    target,
                    existing,
                    ConditionRemovalReason.Removed,
                    combatants);
            else if (statusDefinition.StackingPolicy == AbilityStatusStackingPolicy.Refresh)
                existing.Refresh(stacks);
            else
                existing.AddStacks(stacks);
        }

        if (existing is null || statusDefinition.StackingPolicy == AbilityStatusStackingPolicy.Replace)
        {
            target.Statuses.Add(
                new RuntimeStatus(
                    statusDefinition,
                    source,
                    target,
                    stacks,
                    statsSource,
                    CalculateStatusDuration(statusDefinition, target)));
        }

        Log(source, target, statusId, EventType.StatusEffect, stacks, $"{source.Name} applied {statusId} to {target.Name}.", statsSource, countStatsActivation);
        Publish(new CombatEvent(AbilityTriggerEvent.OnStatusApplied, source, target, statusId), combatants);
    }

    private void ApplyCondition(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType type,
        int value,
        int authoredDurationTicks,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        if (IsControlCondition(type) && target.HasCondition(StandardConditionType.Unstoppable))
            return;

        if (type is StandardConditionType.Freeze or StandardConditionType.Stun
            && _random.Next(1, 101) > 80)
        {
            return;
        }

        if (IsHarmfulCondition(type)
            && TryConsumeConditionCharge(target, StandardConditionType.Ward, source, combatants))
        {
            return;
        }

        var normalizedValue = Math.Max(1, value);
        switch (type)
        {
            case StandardConditionType.Empower:
            case StandardConditionType.Weaken:
            case StandardConditionType.Haste:
            case StandardConditionType.Slow:
                ApplyOrRefreshUniqueCondition(source, target, type, 1, 10 * TicksPerSecond, statsSource);
                break;
            case StandardConditionType.Freeze:
            case StandardConditionType.Stun:
            case StandardConditionType.Taunt:
            case StandardConditionType.Stealth:
            case StandardConditionType.Unstoppable:
                ApplyOrRefreshUniqueCondition(
                    source,
                    target,
                    type,
                    1,
                    normalizedValue * TicksPerSecond,
                    statsSource);
                break;
            case StandardConditionType.Chill:
                ApplyOrStackSharedCondition(source, target, type, normalizedValue, 20, 10 * TicksPerSecond, statsSource);
                break;
            case StandardConditionType.Corrosion:
                ApplyOrStackSharedCondition(source, target, type, normalizedValue, 50, 12 * TicksPerSecond, statsSource);
                break;
            case StandardConditionType.Vulnerable:
                ApplyOrStackSharedCondition(source, target, type, normalizedValue, int.MaxValue, 0, statsSource);
                break;
            case StandardConditionType.Guard:
            case StandardConditionType.Ward:
                ApplyOrStackSharedCondition(source, target, type, normalizedValue, int.MaxValue, 0, statsSource);
                break;
            case StandardConditionType.Poison:
                AddIndependentCondition(source, target, type, normalizedValue, 12 * TicksPerSecond, statsSource, 2 * TicksPerSecond);
                break;
            case StandardConditionType.Burn:
                AddIndependentCondition(source, target, type, normalizedValue, 4 * TicksPerSecond, statsSource, TicksPerSecond);
                break;
            case StandardConditionType.Bleed:
                AddIndependentCondition(source, target, type, normalizedValue, 8 * TicksPerSecond, statsSource, 2 * TicksPerSecond);
                break;
            case StandardConditionType.Doom:
                AddIndependentCondition(source, target, type, normalizedValue, 15 * TicksPerSecond, statsSource);
                break;
            case StandardConditionType.Thorns:
                AddIndependentCondition(source, target, type, normalizedValue, Math.Max(0, authoredDurationTicks), statsSource);
                break;
            case StandardConditionType.Wound:
            case StandardConditionType.Recovery:
            case StandardConditionType.Decay:
            case StandardConditionType.Renewal:
                AddIndependentCondition(
                    source,
                    target,
                    type,
                    1,
                    normalizedValue * TicksPerSecond,
                    statsSource);
                break;
            default:
                throw new NotSupportedException($"Unsupported standard condition '{type}'.");
        }

        var conditionId = GetConditionId(type);
        Log(
            source,
            target,
            conditionId,
            EventType.StatusEffect,
            normalizedValue,
            $"{source.Name} applied {type} to {target.Name}.",
            statsSource,
            countStatsActivation);
        Publish(new CombatEvent(AbilityTriggerEvent.OnStatusApplied, source, target, conditionId), combatants);
    }

    private void ApplyOrRefreshUniqueCondition(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType type,
        int value,
        int durationTicks,
        string? statsSource)
    {
        var existing = target.Conditions.FirstOrDefault(x => x.Type == type);
        if (existing is null)
        {
            AddIndependentCondition(source, target, type, value, durationTicks, statsSource);
            return;
        }

        existing.ReplaceValue(value);
        existing.RefreshDuration(durationTicks);
    }

    private void ApplyOrStackSharedCondition(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType type,
        int value,
        int maximum,
        int durationTicks,
        string? statsSource)
    {
        var existing = target.Conditions.FirstOrDefault(x => x.Type == type);
        if (existing is null)
        {
            AddIndependentCondition(
                source,
                target,
                type,
                Math.Min(value, maximum),
                durationTicks,
                statsSource);
            return;
        }

        existing.AddValue(value, maximum);
        if (durationTicks > 0)
            existing.RefreshDuration(durationTicks);
    }

    private void AddIndependentCondition(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType type,
        int value,
        int durationTicks,
        string? statsSource,
        int intervalTicks = 0)
    {
        target.Conditions.Add(
            new RuntimeCondition(
                type,
                source,
                target,
                value,
                durationTicks,
                GetEffectivePower(source),
                ++_applicationOrder,
                statsSource ?? type.ToString(),
                intervalTicks));
    }

    private static bool IsControlCondition(StandardConditionType type) =>
        type is StandardConditionType.Freeze or StandardConditionType.Stun;

    private static bool IsHarmfulCondition(StandardConditionType type) =>
        type is StandardConditionType.Slow
            or StandardConditionType.Weaken
            or StandardConditionType.Vulnerable
            or StandardConditionType.Wound
            or StandardConditionType.Decay
            or StandardConditionType.Poison
            or StandardConditionType.Burn
            or StandardConditionType.Bleed
            or StandardConditionType.Stun
            or StandardConditionType.Chill
            or StandardConditionType.Freeze
            or StandardConditionType.Corrosion
            or StandardConditionType.Doom;

    private static bool IsBeneficialCondition(StandardConditionType type) =>
        !IsHarmfulCondition(type);

    private static string GetConditionId(StandardConditionType type) =>
        type == StandardConditionType.Vulnerable
            ? "condition.vulnerability"
            : $"condition.{type.ToString().ToLowerInvariant()}";

    private static int CalculateStatusDuration(
        CompiledStatus statusDefinition,
        RuntimeCombatant target)
    {
        if (statusDefinition.DurationTicks <= 0)
            return statusDefinition.DurationTicks;

        var isCrowdControl = statusDefinition.Tags.Any(tag =>
            tag.StartsWith("Control.", StringComparison.OrdinalIgnoreCase));
        var resistanceAttribute = isCrowdControl
            ? AttributeType.CrowdControlResistance
            : AttributeType.StatusResistance;
        var resistance = Math.Max(0, target.GetAttribute(resistanceAttribute));
        if (isCrowdControl)
        {
            return AttributeCombatRules.CalculateCrowdControlDurationTicks(
                statusDefinition.DurationTicks,
                resistance);
        }

        return Math.Max(
            1,
            (int)Math.Ceiling(statusDefinition.DurationTicks / (1 + resistance / 100f)));
    }

    private static double CalculateStatusEffectDurationMultiplier(RuntimeStatus status) =>
        status.Definition.DurationTicks <= 0
            ? 1d
            : status.DurationTicks / (double)status.Definition.DurationTicks;

    private void SummonCombatant(
        RuntimeCombatant source,
        CompiledEffect effect,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation,
        EffectExecutionContext executionContext)
    {
        if (string.IsNullOrWhiteSpace(effect.SummonId))
            throw new InvalidOperationException($"Summon effect '{effect.Id}' requires summonId.");

        if (combatants is not List<RuntimeCombatant> mutableCombatants)
            throw new InvalidOperationException("Summon effects require a mutable combatant list.");

        if (!_summonsById.TryGetValue(effect.SummonId, out var summonDefinition))
            throw new InvalidOperationException($"Summon '{effect.SummonId}' has not been compiled.");

        if (HasReachedSummonCap(source, summonDefinition, combatants))
            return;

        var groupInstanceId = string.IsNullOrWhiteSpace(effect.SummonGroupId)
            ? null
            : executionContext.GetSummonGroupInstanceId(effect.SummonGroupId);
        var summon = CreateSummonedCombatant(
            source,
            effect,
            summonDefinition,
            _abilitiesById,
            groupInstanceId);
        mutableCombatants.Add(summon);
        _basicAttackProgress[summon] = GetBasicAttackChargeThreshold();
        _healthRegenerationProgress[summon] = 0;

        if (groupInstanceId is not null)
        {
            var durationTicks = effect.DurationTicks > 0
                ? effect.DurationTicks
                : summonDefinition.DurationTicks;
            if (!_summonGroups.TryGetValue(groupInstanceId, out var group))
            {
                group = new RuntimeSummonGroup(
                    groupInstanceId,
                    effect.SummonGroupId!,
                    source,
                    _currentTick + Math.Max(1, durationTicks) - 1);
                _summonGroups[groupInstanceId] = group;
            }

            group.Members.Add(summon);
        }

        Log(source, summon, effect.Id, EventType.Summon, 1, $"{source.Name} summoned {summon.Name}.", statsSource, countStatsActivation);
        NotifySummonChanged(summon, combatants);
    }

    private void SynchronizeAttributePerOwnedSummon(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        var summonTag = $"Summon.{effect.SummonId}";
        var livingSummons = combatants.Count(combatant =>
            combatant.IsAlive
            && combatant.IsSummoned
            && ReferenceEquals(combatant.SummonOwner, source)
            && combatant.Tags.Contains(summonTag));
        var desiredAmount = livingSummons * effect.BaseValue;
        var delta = target.SynchronizeAttributeContribution(
            effect.Id,
            effect.Attribute!.Value,
            desiredAmount);
        if (Math.Abs(delta) <= float.Epsilon)
            return;

        var roundedDelta = (int)Math.Round(delta);
        Log(
            source,
            target,
            effect.Id,
            delta > 0 ? EventType.Buff : EventType.BuffExpired,
            roundedDelta,
            $"{target.Name}'s {effect.Attribute} changed by {roundedDelta} from {livingSummons} living summon(s).",
            statsSource,
            countStatsActivation);
    }

    private void ConsumeOwnedSummon(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant _,
        int healing,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        var target = combatants
            .Where(combatant =>
                combatant.IsAlive
                && combatant.IsSummoned
                && ReferenceEquals(combatant.SummonOwner, source)
                && combatant.Tags.Contains($"Summon.{effect.SummonId}"))
            .OrderBy(combatant => combatant.Health)
            .FirstOrDefault();
        if (target is null)
            return;

        target.SetHealth(0);
        Log(
            source,
            target,
            effect.Id,
            EventType.SummonExpired,
            0,
            $"{source.Name} devoured {target.Name}.",
            statsSource,
            countStatsActivation);
        NotifySummonChanged(target, combatants);
        RestoreHealth(
            source,
            source,
            healing,
            combatants,
            effect.Id,
            statsSource,
            isLifeSteal: false,
            effect,
            countStatsActivation,
            applyHealingModifiers: false);
    }

    private void SynchronizeAttributePerStatusStack(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        string? statsSource,
        bool countStatsActivation)
    {
        var stacks = target.GetStatusStacks(effect.StatusId!);
        var perStack = effect.BaseValue
                       + target.GetInitialAttribute(effect.Attribute!.Value) * effect.ScalingCoefficient;
        var desiredAmount = stacks * perStack;
        var delta = target.SynchronizeAttributeContribution(
            effect.Id,
            effect.Attribute.Value,
            desiredAmount);
        if (Math.Abs(delta) <= float.Epsilon)
            return;

        var roundedDelta = (int)Math.Round(delta);
        Log(
            source,
            target,
            effect.Id,
            delta > 0 ? EventType.Buff : EventType.BuffExpired,
            roundedDelta,
            $"{target.Name}'s {effect.Attribute} changed by {roundedDelta} from {stacks} status stack(s).",
            statsSource,
            countStatsActivation);
    }

    private int GetBasicAttackChargeThreshold() => Math.Max(1, _basicAttackIntervalTicks);

    private static float GetBasicAttackRate(RuntimeCombatant actor)
    {
        var baseRate =
            (1d + actor.GetAttribute(AttributeType.AttackSpeed) / 100d)
            / Math.Max(0.01d, actor.BasicAttackIntervalMultiplier);
        var hasteSlowMultiplier =
            1d
            + (actor.HasCondition(StandardConditionType.Haste) ? 0.25d : 0d)
            - (actor.HasCondition(StandardConditionType.Slow) ? 0.25d : 0d);
        var chillStacks = Math.Min(20, actor.GetConditionStacks(StandardConditionType.Chill));
        var chillMultiplier = 1d - chillStacks / 100d;
        return (float)Math.Clamp(
            baseRate * hasteSlowMultiplier * chillMultiplier,
            AttributeCombatRules.MinimumBasicAttackRate,
            AttributeCombatRules.MaximumBasicAttackRate);
    }

    private void InitializeActiveAbilityCooldowns(RuntimeCombatant combatant)
    {
        if (!_startActiveAbilitiesOnCooldown)
            return;

        foreach (var ability in combatant.Abilities.Where(x =>
                     x.Definition.Kind == AbilitySpecKind.Active
                     && !IsSummonAbility(x.Definition)))
            ability.StartInitialCooldown(combatant.GetAttribute(AttributeType.Cooldown));
    }

    private static bool IsSummonAbility(CompiledAbility ability) =>
        ability.TriggersByEvent
            .GetValueOrDefault(AbilityTriggerEvent.OnAbilityUsed)?
            .SelectMany(trigger => trigger.Effects)
            .Any(effect => effect.Operation == AbilityEffectOperation.Summon) == true;

    private static RuntimeCombatant CreateSummonedCombatant(
        RuntimeCombatant source,
        CompiledEffect effect,
        CompiledSummon summonDefinition,
        IReadOnlyDictionary<string, CompiledAbility> abilitiesById,
        string? summonGroupInstanceId)
    {
        var summonId = effect.SummonId!;
        var attributes = CreateSummonAttributes(source, effect, summonDefinition);
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

        return new RuntimeCombatant(
            id: $"{source.Id}:summon:{summonId}:{Guid.NewGuid():N}",
            name: summonDefinition.Name,
            team: source.Team,
            attributes: attributes,
            abilities: abilities,
            tags: tags,
            imagePath: summonDefinition.ImagePath,
            isSummoned: true,
            summonDurationTicks: effect.DurationTicks > 0 ? effect.DurationTicks : summonDefinition.DurationTicks,
            summonOwner: source,
            canBasicAttack: summonDefinition.CanBasicAttack,
            summonGroupId: effect.SummonGroupId,
            summonGroupInstanceId: summonGroupInstanceId);
    }

    private static Dictionary<AttributeType, float> CreateSummonAttributes(
        RuntimeCombatant source,
        CompiledEffect effect,
        CompiledSummon summonDefinition)
    {
        var attributes = summonDefinition.Attributes.ToDictionary(
            attribute => attribute.Attribute,
            attribute => (float)Math.Max(
                attribute.MinimumValue,
                (int)Math.Round(
                    (attribute.BaseValue + (attribute.ScalingAttribute is { } scalingAttribute
                        ? GetEffectiveAttribute(source, scalingAttribute) * attribute.ScalingCoefficient
                        : 0))
                    * GetSummonAttributeMultiplier(attribute.Attribute, effect))));

        attributes.TryAdd(AttributeType.MaxHealth, 1);
        attributes.TryAdd(AttributeType.Power, 0);
        attributes.TryAdd(AttributeType.AttackSpeed, 0);
        return attributes;
    }

    private static double GetSummonAttributeMultiplier(
        AttributeType attribute,
        CompiledEffect effect) =>
        attribute == AttributeType.MaxHealth
            ? Math.Max(0d, effect.SummonHealthMultiplier)
            : attribute == AttributeType.Power
                ? Math.Max(0d, effect.SummonPowerMultiplier)
                : 1d;

    private void ModifyStatusStacks(
        RuntimeCombatant source,
        RuntimeCombatant target,
        string statusId,
        int amount,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var existing = target.Statuses.FirstOrDefault(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            return;

        var previousStacks = existing.Stacks;
        existing.AddStacks(amount);
        if (existing.Stacks == previousStacks)
            return;

        if (existing.Stacks <= 0)
            RemoveStatus(source, target, statusId, combatants);
        else
            Publish(
                new CombatEvent(
                    AbilityTriggerEvent.OnStatusChanged,
                    source,
                    target,
                    statusId,
                    existing.Stacks - previousStacks),
                combatants);
    }

    private bool RemoveStatus(
        RuntimeCombatant source,
        RuntimeCombatant target,
        string statusId,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var removed = false;
        foreach (var status in target.Statuses
                     .Where(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            var wasPresent = target.Statuses.Contains(status);
            RemoveStatusInstance(source, target, status, ConditionRemovalReason.Removed, combatants);
            removed |= wasPresent && !target.Statuses.Contains(status);
        }

        return removed;
    }

    private void CleanseStatuses(
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var status in target.Statuses.ToList())
            RemoveStatusInstance(source, target, status, ConditionRemovalReason.Cleansed, combatants);
    }

    private void CleanseConditions(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType? selectedType,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var types = selectedType is { } selected
            ? [selected]
            : target.Conditions
                .Where(condition => IsHarmfulCondition(condition.Type))
                .Select(condition => condition.Type)
                .Distinct()
                .ToArray();

        foreach (var type in types.Where(IsHarmfulCondition))
        {
            var removeOne = type is StandardConditionType.Doom
                or StandardConditionType.Wound
                or StandardConditionType.Decay;
            RemoveConditionInstances(
                source,
                target,
                type,
                removeOne ? 1 : int.MaxValue,
                ConditionRemovalReason.Cleansed,
                combatants);
        }
    }

    private void DispelConditions(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType? selectedType,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var types = selectedType is { } selected
            ? [selected]
            : target.Conditions
                .Where(condition => IsBeneficialCondition(condition.Type))
                .Select(condition => condition.Type)
                .Distinct()
                .ToArray();

        foreach (var type in types.Where(IsBeneficialCondition))
        {
            if (type is StandardConditionType.Guard or StandardConditionType.Ward)
                continue;

            var removeOne = type is StandardConditionType.Thorns
                or StandardConditionType.Recovery
                or StandardConditionType.Renewal;
            RemoveConditionInstances(
                source,
                target,
                type,
                removeOne ? 1 : int.MaxValue,
                ConditionRemovalReason.Dispelled,
                combatants);
        }
    }

    private void RemoveConditionInstances(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType type,
        int count,
        ConditionRemovalReason removalReason,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var condition in target.Conditions
                     .Where(x => x.Type == type)
                     .OrderBy(x => x.DurationTicks <= 0 ? int.MaxValue : x.RemainingDurationTicks)
                     .ThenBy(x => x.ApplicationOrder)
                     .Take(count)
                     .ToList())
        {
            RemoveCondition(source, target, condition, removalReason, combatants);
        }
    }

    private void ResolveConditionConsumption(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        string statsSource,
        bool countStatsActivation,
        EffectExecutionContext executionContext)
    {
        var consumed = 0;
        while (consumed < effect.BaseValue
               && TryConsumeConditionCharge(target, effect.Condition!.Value, source, combatants))
        {
            consumed++;
        }

        if (consumed <= 0)
            return;

        var damage = (int)Math.Round(
            GetEffectiveAttribute(source, effect.ScalingAttribute!.Value)
            * effect.ScalingCoefficient
            * consumed);
        if (effect.ScalingAttribute == AttributeType.Power)
            damage = ApplyCombatMagnitudeVariance(damage);

        var healthDamage = ApplyDamage(
            source,
            target,
            Math.Max(0, damage),
            effect.AttackType,
            effect.DamageType,
            effect,
            combatants,
            effect.Id,
            statsSource,
            countStatsActivation,
            DamageDelivery.Direct);
        ApplyLifeSteal(effect, source, healthDamage, combatants, statsSource);

        if (effect.HealingScalingAttribute is not { } healingAttribute
            || effect.HealingScalingCoefficient <= 0)
        {
            return;
        }

        var healingBasis = source.GetAttribute(healingAttribute);
        var generatedHealing = Math.Max(
            0,
            (int)Math.Round(healingBasis * effect.HealingScalingCoefficient * consumed));
        var healingCap = effect.MaximumHealingScalingCoefficient > 0
            ? Math.Max(0, (int)Math.Round(healingBasis * effect.MaximumHealingScalingCoefficient))
            : int.MaxValue;
        var remainingHealing = Math.Max(
            0,
            healingCap - executionContext.GetGeneratedHealing(effect.Id));
        var healing = Math.Min(generatedHealing, remainingHealing);
        if (healing <= 0)
            return;

        executionContext.AddGeneratedHealing(effect.Id, healing);
        RestoreHealth(
            source,
            source,
            healing,
            combatants,
            effect.Id,
            statsSource,
            isLifeSteal: false,
            effect,
            countStatsActivation: false,
            applyHealingModifiers: false);
    }

    private bool TryConsumeConditionCharge(
        RuntimeCombatant target,
        StandardConditionType type,
        RuntimeCombatant eventSource,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var condition = target.Conditions
            .Where(x => x.Type == type && x.Value > 0)
            .OrderBy(x => x.ApplicationOrder)
            .FirstOrDefault();
        if (condition is null)
            return false;

        condition.AddValue(-1);
        if (condition.Value <= 0)
            RemoveCondition(
                eventSource,
                target,
                condition,
                ConditionRemovalReason.Consumed,
                combatants);

        return true;
    }

    private void RemoveCondition(
        RuntimeCombatant eventSource,
        RuntimeCombatant target,
        RuntimeCondition condition,
        ConditionRemovalReason removalReason,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!target.Conditions.Remove(condition))
            return;

        var conditionId = GetConditionId(condition.Type);
        Log(
            eventSource,
            target,
            conditionId,
            GetRemovalLogEvent(removalReason),
            0,
            $"{condition.Type} was {GetRemovalDescription(removalReason)} on {target.Name}.",
            condition.StatsSource);
        Publish(
            new CombatEvent(
                GetRemovalTriggerEvent(removalReason),
                condition.Source,
                target,
                conditionId,
                RemovalReason: removalReason),
            combatants);
    }

    private void RemoveStatusInstance(
        RuntimeCombatant source,
        RuntimeCombatant target,
        RuntimeStatus status,
        ConditionRemovalReason removalReason,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (status.IsRemovalLocked)
            return;

        if (!target.Statuses.Remove(status))
            return;

        Log(
            source,
            target,
            status.Definition.Id,
            GetRemovalLogEvent(removalReason),
            0,
            $"{status.Definition.Id} was {GetRemovalDescription(removalReason)} on {target.Name}.");
        Publish(
            new CombatEvent(
                GetRemovalTriggerEvent(removalReason),
                status.Source,
                target,
                status.Definition.Id,
                RemovalReason: removalReason),
            combatants);
    }

    private static AbilityTriggerEvent GetRemovalTriggerEvent(ConditionRemovalReason removalReason) =>
        removalReason switch
        {
            ConditionRemovalReason.Expired => AbilityTriggerEvent.OnStatusExpired,
            ConditionRemovalReason.Cleansed => AbilityTriggerEvent.OnStatusCleansed,
            ConditionRemovalReason.Dispelled => AbilityTriggerEvent.OnStatusDispelled,
            _ => AbilityTriggerEvent.OnStatusRemoved
        };

    private static EventType GetRemovalLogEvent(ConditionRemovalReason removalReason) =>
        removalReason switch
        {
            ConditionRemovalReason.Expired => EventType.StatusEffectExpired,
            ConditionRemovalReason.Cleansed => EventType.StatusEffectCleansed,
            ConditionRemovalReason.Dispelled => EventType.StatusEffectDispelled,
            _ => EventType.StatusEffectRemoved
        };

    private static string GetRemovalDescription(ConditionRemovalReason removalReason) =>
        removalReason switch
        {
            ConditionRemovalReason.Expired => "expired",
            ConditionRemovalReason.Cleansed => "cleansed",
            ConditionRemovalReason.Dispelled => "dispelled",
            ConditionRemovalReason.Consumed => "consumed",
            _ => "removed"
        };

    private void TickEffects(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var combatant in combatants)
        {
            foreach (var effect in combatant.ActiveEffects.ToList())
            {
                if (effect.Tick() && (effect.Definition.ChancePercent >= 100 || _random.Next(1, 101) <= effect.Definition.ChancePercent))
                    ApplyEffectOnce(
                        effect.Definition,
                        effect.Source,
                        effect.Target,
                        combatants,
                        statsSourceOverride: effect.StatsSource);

                if (effect.IsExpired)
                {
                    if (IsTimedModifierOperation(effect.Definition.Operation))
                    {
                        var value = CalculateValue(effect.Definition, effect.Source);
                        switch (effect.Definition.Operation)
                        {
                            case AbilityEffectOperation.ModifyAttribute:
                                effect.Target.AdjustAttribute(effect.Definition.Attribute!.Value, -value);
                                break;
                            case AbilityEffectOperation.ModifyThreat:
                                effect.Target.AdjustThreat(-value);
                                break;
                            case AbilityEffectOperation.ModifyRegenerationRate:
                                effect.Target.AdjustRegenerationRate(-value);
                                break;
                            case AbilityEffectOperation.ModifyRegenerationInterval:
                                effect.Target.AdjustRegenerationInterval(-value);
                                break;
                            case AbilityEffectOperation.ModifyHealingReceived:
                                effect.Target.AdjustHealingReceived(-value);
                                break;
                            case AbilityEffectOperation.ModifyDamageDealt:
                                effect.Target.AdjustDamageDealt(effect.Definition.DamageType, -value);
                                break;
                            case AbilityEffectOperation.ModifyDamageTaken:
                                effect.Target.AdjustDamageTaken(effect.Definition.DamageType, -value);
                                break;
                            case AbilityEffectOperation.ModifyDamageTakenFromCondition:
                                effect.Target.AdjustDamageTakenFromCondition(effect.Definition.Condition!.Value, -value);
                                break;
                        }
                        Log(effect.Source, effect.Target, effect.Definition.Id, EventType.BuffExpired, -value, $"{effect.Target.Name}'s modifier returned to normal.", effect.StatsSource);
                    }

                    combatant.ActiveEffects.Remove(effect);
                }
            }
        }
    }

    private void TickStatuses(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var combatant in combatants)
        {
            foreach (var status in combatant.Statuses.ToList())
            {
                if (!status.IsExpired)
                    continue;

                RemoveStatusInstance(
                    status.Source,
                    combatant,
                    status,
                    ConditionRemovalReason.Expired,
                    combatants);
            }
        }
    }

    private void TickHealthRegeneration(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var combatant in combatants.Where(x => x.IsAlive))
        {
            var rate = Math.Max(
                0,
                1 + combatant.RegenerationRatePercent / 100f);
            var interval = Math.Max(
                1,
                HealthRegenerationIntervalTicks + combatant.RegenerationIntervalModifierTicks);
            var progress = _healthRegenerationProgress.GetValueOrDefault(combatant) + rate;
            if (progress < interval)
            {
                _healthRegenerationProgress[combatant] = progress;
                continue;
            }

            _healthRegenerationProgress[combatant] = progress - interval;

            var regeneration = ApplyRegenerationAmountModifier(
                combatant,
                Math.Max(0, combatant.GetAttribute(AttributeType.HealthRegeneration)));
            regeneration = ApplyHealingReceivedModifier(combatant, regeneration);
            var potential = Math.Max(0, (int)Math.Round(regeneration));
            if (potential <= 0)
                continue;

            _healthRegenerationPotential[combatant] =
                _healthRegenerationPotential.GetValueOrDefault(combatant) + potential;
            _healthRegenerationPulses[combatant] =
                _healthRegenerationPulses.GetValueOrDefault(combatant) + 1;

            if (combatant.Health >= combatant.GetAttribute(AttributeType.MaxHealth))
            {
                _healthRegenerationOverhealed[combatant] =
                    _healthRegenerationOverhealed.GetValueOrDefault(combatant) + potential;
                continue;
            }

            var healthBefore = combatant.Health;
            combatant.AdjustHealth(regeneration);
            var restored = Math.Max(0, (int)Math.Round(combatant.Health - healthBefore));
            _healthRegenerationOverhealed[combatant] =
                _healthRegenerationOverhealed.GetValueOrDefault(combatant)
                + Math.Max(0, potential - restored);
            if (restored <= 0)
                continue;

            Log(
                combatant,
                combatant,
                "Health Regeneration",
                EventType.HealthRegeneration,
                restored,
                $"{combatant.Name} regenerated {restored} health.");
            Publish(new CombatEvent(AbilityTriggerEvent.OnHeal, combatant, combatant, null), combatants);
            Publish(new CombatEvent(AbilityTriggerEvent.OnHealed, combatant, combatant, null), combatants);
            Publish(new CombatEvent(AbilityTriggerEvent.OnHealthChanged, combatant, combatant, null), combatants);
        }
    }

    private static int ApplyHealingReceivedModifier(RuntimeCombatant target, int healing)
    {
        var modifier =
            (target.HasCondition(StandardConditionType.Recovery) ? 0.30f : 0f)
            - (target.HasCondition(StandardConditionType.Wound) ? 0.30f : 0f)
            + target.HealingReceivedPercent / 100f;
        return Math.Max(0, (int)Math.Round(healing * (1 + modifier)));
    }

    private static float ApplyHealingReceivedModifier(RuntimeCombatant target, float healing)
    {
        var modifier =
            (target.HasCondition(StandardConditionType.Recovery) ? 0.30f : 0f)
            - (target.HasCondition(StandardConditionType.Wound) ? 0.30f : 0f)
            + target.HealingReceivedPercent / 100f;
        return Math.Max(0, healing * (1 + modifier));
    }

    private static float ApplyRegenerationAmountModifier(RuntimeCombatant target, float regeneration)
    {
        var modifier =
            (target.HasCondition(StandardConditionType.Renewal) ? 0.30f : 0f)
            - (target.HasCondition(StandardConditionType.Decay) ? 0.30f : 0f);
        return Math.Max(0, regeneration * (1 + modifier));
    }

    private void TickConditions(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var combatant in combatants)
        {
            foreach (var condition in combatant.Conditions.ToList())
            {
                var intervalDue = condition.Tick();
                if (intervalDue && combatant.IsAlive)
                    ResolvePeriodicCondition(condition, combatants);

                if (!condition.IsExpired)
                    continue;

                if (condition.Type == StandardConditionType.Doom && combatant.IsAlive)
                    ResolveDoom(condition, combatants);

                RemoveCondition(
                    condition.Source,
                    combatant,
                    condition,
                    ConditionRemovalReason.Expired,
                    combatants);
            }
        }
    }

    private void ResolvePeriodicCondition(
        RuntimeCondition condition,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var damageType = condition.Type switch
        {
            StandardConditionType.Bleed => DamageType.Bleed,
            StandardConditionType.Burn => DamageType.Burn,
            StandardConditionType.Poison => DamageType.Poison,
            _ => DamageType.None
        };
        if (damageType == DamageType.None)
            return;

        var damage = Math.Max(
            0,
            (int)Math.Round(
                condition.PowerSnapshot * 0.01f * condition.Value,
                MidpointRounding.AwayFromZero));
        if (damage <= 0)
            return;

        ApplyDamage(
            condition.Source,
            condition.Owner,
            damage,
            AttackType.DamageOverTime,
            damageType,
            null,
            combatants,
            GetConditionId(condition.Type),
            condition.StatsSource,
            delivery: DamageDelivery.Periodic);
    }

    private void ResolveDoom(
        RuntimeCondition condition,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var damage = Math.Max(0, (int)Math.Round(condition.PowerSnapshot * condition.Value / 100f));
        if (damage <= 0)
            return;

        ApplyDamage(
            condition.Source,
            condition.Owner,
            damage,
            AttackType.None,
            DamageType.Magical,
            null,
            combatants,
            GetConditionId(StandardConditionType.Doom),
            condition.StatsSource,
            delivery: DamageDelivery.Stored);
    }

    private IReadOnlyList<EntityStats> AddHealthRegenerationTelemetry(
        IReadOnlyList<EntityStats> aggregatedStats,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var result = aggregatedStats.ToList();

        foreach (var combatant in combatants)
        {
            var potential = _healthRegenerationPotential.GetValueOrDefault(combatant);
            var pulses = _healthRegenerationPulses.GetValueOrDefault(combatant);
            if (potential <= 0 && pulses <= 0)
                continue;

            var overhealed = _healthRegenerationOverhealed.GetValueOrDefault(combatant);
            var index = result.FindIndex(stats =>
                stats.EntityId.Equals(combatant.Id, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                result[index] = result[index] with
                {
                    HealthRegenerationPotential = potential,
                    HealthRegenerationOverhealed = overhealed,
                    HealthRegenerationPulses = pulses
                };
                continue;
            }

            result.Add(new EntityStats(
                combatant.Id,
                combatant.Name,
                [],
                Team: combatant.Team.ToString(),
                HealthRegenerationPotential: potential,
                HealthRegenerationOverhealed: overhealed,
                HealthRegenerationPulses: pulses));
        }

        return result;
    }

    private static IReadOnlyList<EntityStats> AddFinalCombatantState(
        IReadOnlyList<EntityStats> aggregatedStats,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var result = aggregatedStats.ToList();

        foreach (var combatant in combatants)
        {
            var index = result.FindIndex(stats =>
                stats.EntityId.Equals(combatant.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                continue;

            result[index] = result[index] with
            {
                Health = (int)combatant.Health,
                MaxHealth = (int)combatant.GetAttribute(AttributeType.MaxHealth),
                Barrier = (int)combatant.Barrier
            };
        }

        return result;
    }

    private void TickBarrierContributions(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var target in combatants)
        {
            foreach (var contribution in target.BarrierContributions.ToList())
            {
                if (!contribution.TickDuration())
                    continue;

                target.BarrierContributions.Remove(contribution);
                RemoveLinkedActiveEffects(
                    contribution.ActivationId,
                    contribution.LinkedEffectId,
                    combatants);
                var source = contribution.Source ?? target;
                var effectId = contribution.EffectId ?? "Barrier";
                Log(
                    source,
                    target,
                    effectId,
                    EventType.BuffExpired,
                    0,
                    $"{target.Name}'s barrier expired.");
                Publish(
                    new CombatEvent(
                        AbilityTriggerEvent.OnBarrierExpired,
                        source,
                        target,
                        contribution.EffectId,
                        BarrierApplicationOrder: contribution.ApplicationOrder),
                    combatants);
            }
        }
    }

    private static void RemoveLinkedActiveEffects(
        string? activationId,
        string? linkedEffectId,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (string.IsNullOrWhiteSpace(activationId) || string.IsNullOrWhiteSpace(linkedEffectId))
            return;

        foreach (var combatant in combatants)
        {
            combatant.ActiveEffects.RemoveAll(effect =>
                effect.ActivationId == activationId
                && effect.Definition.Id.Equals(linkedEffectId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void TickSummons(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var group in _summonGroups.Values
                     .Where(group => group.ExpiresAtTick <= _currentTick)
                     .ToList())
        {
            _summonGroups.Remove(group.InstanceId);
            var survivingCount = group.Members.Count(member => member.IsAlive);
            foreach (var member in group.Members.Where(member => member.IsAlive))
            {
                member.SetHealth(0);
                LogSummonExpired(member, "expired");
                NotifySummonChanged(member, combatants);
            }

            if (group.Owner.IsAlive)
            {
                Publish(
                    new CombatEvent(
                        AbilityTriggerEvent.OnSummonGroupResolved,
                        group.Owner,
                        group.Owner,
                        group.GroupId,
                        survivingCount),
                    combatants);
            }
        }

        foreach (var summon in combatants
                     .Where(x => x.IsSummoned
                         && x.IsAlive
                         && string.IsNullOrWhiteSpace(x.SummonGroupInstanceId))
                     .ToList())
        {
            if (!summon.TickSummonDuration())
                continue;

            summon.SetHealth(0);
            LogSummonExpired(summon, "expired");
            NotifySummonChanged(summon, combatants);
        }
    }

    private static bool IsStatusLifecycleEvent(AbilityTriggerEvent triggerEvent) =>
        triggerEvent is AbilityTriggerEvent.OnStatusApplied
            or AbilityTriggerEvent.OnStatusExpired
            or AbilityTriggerEvent.OnStatusRemoved
            or AbilityTriggerEvent.OnStatusCleansed
            or AbilityTriggerEvent.OnStatusDispelled
            or AbilityTriggerEvent.OnStatusChanged;

    private static bool IsSourceScopedTriggerRelevant(RuntimeCombatant listener, CombatEvent combatEvent) =>
        combatEvent.Event switch
        {
            AbilityTriggerEvent.OnMeleeAttack
                or AbilityTriggerEvent.OnAbilityUsed
                or AbilityTriggerEvent.OnBasicAttack
                or AbilityTriggerEvent.OnRangedAttack
                or AbilityTriggerEvent.OnHit
                or AbilityTriggerEvent.OnKill
                or AbilityTriggerEvent.OnMeleeAttacked
                or AbilityTriggerEvent.OnRangedAttacked
                or AbilityTriggerEvent.OnDamaged
                or AbilityTriggerEvent.OnAttacked
                or AbilityTriggerEvent.OnHeal
                or AbilityTriggerEvent.OnHealed
                or AbilityTriggerEvent.OnLifestealHeal
                or AbilityTriggerEvent.OnInterval
                or AbilityTriggerEvent.OnSummonChanged
                or AbilityTriggerEvent.OnSummonGroupResolved => ReferenceEquals(combatEvent.Source, listener),
            AbilityTriggerEvent.OnStatusApplied
                or AbilityTriggerEvent.OnStatusExpired
                or AbilityTriggerEvent.OnStatusRemoved
                or AbilityTriggerEvent.OnStatusCleansed
                or AbilityTriggerEvent.OnStatusDispelled
                or AbilityTriggerEvent.OnStatusChanged =>
                ReferenceEquals(combatEvent.Source, listener)
                || ReferenceEquals(combatEvent.Target, listener),
            AbilityTriggerEvent.OnBarrierApplied
                or AbilityTriggerEvent.OnBarrierAbsorbed
                or AbilityTriggerEvent.OnBarrierBroken
                or AbilityTriggerEvent.OnBarrierContributionBroken
                or AbilityTriggerEvent.OnBarrierExpired =>
                ReferenceEquals(combatEvent.Source, listener)
                || ReferenceEquals(combatEvent.Target, listener),
            _ => true
        };

    private IEnumerable<RuntimeCombatant> SelectTargets(
        RuntimeCombatant source,
        AbilityTargetSelector targetSelector,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        return targetSelector switch
        {
            AbilityTargetSelector.Self => [source],
            AbilityTargetSelector.Source => [source],
            AbilityTargetSelector.EventSource => combatEvent.Source is null ? [] : [combatEvent.Source],
            AbilityTargetSelector.EventTarget => combatEvent.Target is null ? [] : [combatEvent.Target],
            AbilityTargetSelector.CurrentTarget => SelectLockedEnemy(source, combatEvent) is { } lockedTarget
                ? [lockedTarget]
                : SelectFirstEnemy(source, combatants) is { } target ? [target] : [],
            AbilityTargetSelector.RandomEnemy => SelectLockedEnemy(source, combatEvent) is { } lockedTarget
                ? [lockedTarget]
                : SelectRandomEnemy(source, combatants) is { } target ? [target] : [],
            AbilityTargetSelector.LowestHealthAlly => combatants.Where(x => x.Team == source.Team && x.IsAlive).OrderBy(x => x.Health).Take(1),
            AbilityTargetSelector.AllEnemies => combatants.Where(x => x.Team != source.Team && x.IsAlive),
            AbilityTargetSelector.AllAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive),
            AbilityTargetSelector.EveryoneButSelf => combatants.Where(x => x.Id != source.Id && x.IsAlive),
            AbilityTargetSelector.TwoEnemies => combatants.Where(x => x.Team != source.Team && x.IsAlive).Take(2),
            AbilityTargetSelector.TwoAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive).Take(2),
            AbilityTargetSelector.HighestMaxHealthAlly => combatants
                .Where(x => x.Team == source.Team && x.IsAlive)
                .OrderByDescending(x => x.GetAttribute(AttributeType.MaxHealth))
                .Take(1),
            AbilityTargetSelector.SummonedAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive && x.IsSummoned),
            AbilityTargetSelector.NonSummonedAllies => combatants.Where(x => x.Team == source.Team && x.IsAlive && !x.IsSummoned),
            AbilityTargetSelector.SummonedEnemies => combatants.Where(x => x.Team != source.Team && x.IsAlive && x.IsSummoned),
            AbilityTargetSelector.LowestHealthEnemy => SelectLockedEnemy(source, combatEvent) is { } lockedTarget
                ? [lockedTarget]
                : combatants
                    .Where(x => x.Team != source.Team && x.IsAlive)
                    .OrderBy(x => x.Health / Math.Max(1, x.GetAttribute(AttributeType.MaxHealth)))
                    .Take(1),
            AbilityTargetSelector.HighestHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderByDescending(x => x.Health)
                .Take(1),
            AbilityTargetSelector.LowestCurrentHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderBy(x => x.Health)
                .Take(1),
            AbilityTargetSelector.HighestMaxHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderByDescending(x => x.GetAttribute(AttributeType.MaxHealth))
                .Take(1),
            _ => []
        };
    }

    private RuntimeCombatant? SelectActiveAbilityPrimaryTarget(
        RuntimeAbility ability,
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!ability.Definition.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers))
            return null;

        var selector = triggers
            .SelectMany(trigger => trigger.Effects)
            .Select(effect => effect.Target)
            .FirstOrDefault(target => target is AbilityTargetSelector.CurrentTarget
                or AbilityTargetSelector.RandomEnemy
                or AbilityTargetSelector.LowestHealthEnemy
                or AbilityTargetSelector.HighestHealthEnemy
                or AbilityTargetSelector.LowestCurrentHealthEnemy
                or AbilityTargetSelector.HighestMaxHealthEnemy);

        return selector switch
        {
            AbilityTargetSelector.CurrentTarget => SelectFirstEnemy(source, combatants),
            AbilityTargetSelector.RandomEnemy => SelectRandomEnemy(source, combatants),
            AbilityTargetSelector.LowestHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderBy(x => x.Health / Math.Max(1, x.GetAttribute(AttributeType.MaxHealth)))
                .FirstOrDefault(),
            AbilityTargetSelector.HighestHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderByDescending(x => x.Health)
                .FirstOrDefault(),
            AbilityTargetSelector.LowestCurrentHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderBy(x => x.Health)
                .FirstOrDefault(),
            AbilityTargetSelector.HighestMaxHealthEnemy => combatants
                .Where(x => x.Team != source.Team && x.IsAlive)
                .OrderByDescending(x => x.GetAttribute(AttributeType.MaxHealth))
                .FirstOrDefault(),
            _ => null
        };
    }

    private static RuntimeCombatant? SelectLockedEnemy(RuntimeCombatant source, CombatEvent combatEvent) =>
        combatEvent.Event == AbilityTriggerEvent.OnAbilityUsed
        && combatEvent.Target is { IsAlive: true } target
        && target.Team != source.Team
            ? target
            : null;

    private bool EffectCanResolve(
        CompiledEffect effect,
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (effect.Operation == AbilityEffectOperation.ConsumeOwnedSummon)
        {
            return !string.IsNullOrWhiteSpace(effect.SummonId)
                   && combatants.Any(combatant =>
                       combatant.IsAlive
                       && combatant.IsSummoned
                       && ReferenceEquals(combatant.SummonOwner, source)
                       && combatant.Tags.Contains($"Summon.{effect.SummonId}"));
        }

        if (effect.Operation != AbilityEffectOperation.Summon || string.IsNullOrWhiteSpace(effect.SummonId))
            return true;

        return _summonsById.TryGetValue(effect.SummonId, out var summonDefinition)
            && !HasReachedSummonCap(source, summonDefinition, combatants);
    }

    private static bool HasReachedSummonCap(
        RuntimeCombatant source,
        CompiledSummon summonDefinition,
        IReadOnlyList<RuntimeCombatant> combatants) =>
        summonDefinition.MaxActive > 0
        && combatants.Count(x => x.IsAlive
            && x.IsSummoned
            && ReferenceEquals(x.SummonOwner, source)
            && x.Tags.Contains($"Summon.{summonDefinition.Id}")) >= summonDefinition.MaxActive;

    private void ExpireOwnedSummons(
        RuntimeCombatant owner,
        IReadOnlyList<RuntimeCombatant> combatants,
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

    private void LogSummonExpired(RuntimeCombatant summon, string reason)
    {
        var source = summon.SummonOwner ?? summon;
        Log(source, summon, summon.Name, EventType.SummonExpired, 0, $"{summon.Name} {reason}.");
    }

    private void NotifySummonChanged(
        RuntimeCombatant summon,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!summon.IsSummoned || summon.SummonOwner is not { IsAlive: true } owner)
            return;

        Publish(
            new CombatEvent(AbilityTriggerEvent.OnSummonChanged, owner, summon, null),
            combatants);
    }

    private RuntimeCombatant? SelectFirstEnemy(RuntimeCombatant source, IReadOnlyList<RuntimeCombatant> combatants)
    {
        var enemies = combatants.Where(x => x.Team != source.Team && x.IsAlive).ToList();
        return SelectThreatWeightedEnemy(enemies);
    }

    private RuntimeCombatant? SelectRandomEnemy(RuntimeCombatant source, IReadOnlyList<RuntimeCombatant> combatants)
    {
        var enemies = combatants.Where(x => x.Team != source.Team && x.IsAlive).ToList();
        return enemies.Count == 0 ? null : enemies[_random.Next(enemies.Count)];
    }

    private RuntimeCombatant? SelectThreatWeightedEnemy(IReadOnlyList<RuntimeCombatant> enemies)
    {
        if (enemies.Count == 0)
            return null;

        var weights = enemies.Select(GetEffectiveThreat).ToArray();
        var total = weights.Sum();
        if (total <= 0)
            return enemies[0];

        var roll = _random.NextDouble() * total;
        for (var index = 0; index < enemies.Count; index++)
        {
            roll -= weights[index];
            if (roll < 0)
                return enemies[index];
        }

        return enemies[^1];
    }

    private double GetEffectiveThreat(RuntimeCombatant combatant)
    {
        if (combatant.HasCondition(StandardConditionType.Stealth))
            return 1d;

        var threat = combatant.Threat;
        if (combatant.HasCondition(StandardConditionType.Taunt))
        {
            threat += _tauntThreatBonus;
        }

        return threat;
    }

    private bool ConditionsPass(
        IEnumerable<CompiledCondition> conditions,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants) =>
        conditions.All(condition => ConditionPass(condition, source, combatEvent, combatants));

    private bool ConditionPass(
        CompiledCondition condition,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (condition.Type == AbilityConditionType.AnyEnemyHealthBelowPercent)
        {
            return combatants.Any(combatant =>
                combatant.Team != source.Team
                && combatant.IsAlive
                && IsHealthBelowPercent(combatant, condition.Value));
        }

        if (condition.Type == AbilityConditionType.NoEnemyHealthBelowPercent)
        {
            return combatants.All(combatant =>
                combatant.Team == source.Team
                || !combatant.IsAlive
                || !IsHealthBelowPercent(combatant, condition.Value));
        }

        if (condition.Type == AbilityConditionType.EventSourceIsEnemy)
            return combatEvent.Source is { } eventSource && eventSource.Team != source.Team;

        if (condition.Type == AbilityConditionType.EventMagnitudeAtLeast)
            return combatEvent.Magnitude >= condition.Value;

        if (condition.Type == AbilityConditionType.EventMagnitudeAtMost)
            return combatEvent.Magnitude <= condition.Value;

        var subject = ResolveSubject(condition.Subject, source, combatEvent);
        if (subject is null)
            return false;

        return condition.Type switch
        {
            AbilityConditionType.Always => true,
            AbilityConditionType.HealthBelowPercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 < condition.Value,
            AbilityConditionType.HealthAtOrBelowPercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 <= condition.Value,
            AbilityConditionType.HealthAbovePercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 > condition.Value,
            AbilityConditionType.HasStatus => subject.GetStatusStacks(condition.StatusId!) > 0,
            AbilityConditionType.StatusStacksAtLeast => subject.GetStatusStacks(condition.StatusId!) >= condition.Value,
            AbilityConditionType.HasCondition => subject.HasCondition(condition.Condition!.Value),
            AbilityConditionType.ConditionStacksAtLeast =>
                subject.GetConditionStacks(condition.Condition!.Value) >= condition.Value,
            AbilityConditionType.EventDamageTypeIs => combatEvent.DamageType == condition.DamageType,
            AbilityConditionType.EventAttackTypeIs => combatEvent.AttackType == condition.AttackType,
            AbilityConditionType.EventWasCritical => combatEvent.WasCritical,
            AbilityConditionType.EventWasDirectHit => combatEvent.WasDirectHit,
            AbilityConditionType.EventIdIs => string.Equals(
                combatEvent.AbilityId,
                condition.StatusId,
                StringComparison.OrdinalIgnoreCase),
            AbilityConditionType.EventSourceIsSelf => ReferenceEquals(combatEvent.Source, source),
            AbilityConditionType.HasTag => subject.Tags.Contains(condition.Tag!),
            AbilityConditionType.ChancePercent => _random.Next(1, 101) <= condition.Value,
            _ => false
        };
    }

    private static bool IsHealthBelowPercent(RuntimeCombatant combatant, int percent) =>
        combatant.GetAttribute(AttributeType.MaxHealth) > 0
        && combatant.Health / combatant.GetAttribute(AttributeType.MaxHealth) * 100 < percent;

    private static RuntimeCombatant? ResolveSubject(
        AbilityConditionSubject subject,
        RuntimeCombatant source,
        CombatEvent combatEvent) =>
        subject switch
        {
            AbilityConditionSubject.Source => source,
            AbilityConditionSubject.Target => combatEvent.Target,
            AbilityConditionSubject.EventSource => combatEvent.Source,
            AbilityConditionSubject.EventTarget => combatEvent.Target,
            _ => null
        };

    private int CalculateValue(
        CompiledEffect effect,
        RuntimeCombatant source,
        CombatEvent? combatEvent = null)
    {
        var scalingCoefficient = effect.ScalingCoefficient;
        if (effect.MaximumScalingCoefficient > effect.ScalingCoefficient)
        {
            scalingCoefficient += (float)_random.NextDouble()
                                  * (effect.MaximumScalingCoefficient - effect.ScalingCoefficient);
        }

        var value = effect.BaseValue
                    + (effect.ScalingAttribute is { } attribute
                        ? GetEffectiveAttribute(source, attribute) * scalingCoefficient
                        : 0)
                    + (combatEvent?.Magnitude ?? 0) * effect.EventMagnitudeCoefficient;
        if (effect.ScalingCondition is { } condition)
        {
            value += source.GetConditionStacks(condition)
                     * GetEffectivePower(source)
                     * effect.ConditionScalingCoefficient;
        }
        if (!string.IsNullOrWhiteSpace(effect.ScalingStatusId))
        {
            value += source.GetStatusStacks(effect.ScalingStatusId)
                     * GetEffectivePower(source)
                     * effect.StatusScalingCoefficient;
        }

        return Math.Max(
            AllowsNegativeValue(effect.Operation) ? int.MinValue : 0,
            (int)Math.Round(value));
    }

    private static float GetEffectiveAttribute(RuntimeCombatant combatant, AttributeType attribute) =>
        attribute == AttributeType.Power
            ? GetEffectivePower(combatant)
            : combatant.GetAttribute(attribute);

    private static float GetEffectivePower(RuntimeCombatant combatant)
    {
        var modifier =
            (combatant.HasCondition(StandardConditionType.Empower) ? 0.20f : 0f)
            - (combatant.HasCondition(StandardConditionType.Weaken) ? 0.20f : 0f);
        return Math.Max(0, combatant.GetAttribute(AttributeType.Power) * (1 + modifier));
    }

    private int ApplyCombatMagnitudeVariance(int value)
    {
        if (value <= 0)
            return value;

        var minimumMultiplier = 1d - CombatMagnitudeVariance;
        var multiplier =
            minimumMultiplier + _magnitudeRandom.NextDouble() * CombatMagnitudeVariance * 2d;
        return Math.Max(0, (int)Math.Round(value * multiplier));
    }

    private static int CalculateCostValue(CompiledCost cost, RuntimeCombatant source) =>
        Math.Max(0, (int)Math.Round(cost.BaseValue + (cost.ScalingAttribute is { } attribute
            ? source.GetAttribute(attribute) * cost.ScalingCoefficient
            : 0)));

    private static bool AllowsNegativeValue(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute
            or AbilityEffectOperation.ModifyAttributePercentOfInitial
            or AbilityEffectOperation.ModifyStatusStacks
            or AbilityEffectOperation.ModifyThreat
            or AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyRegenerationInterval
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition;

    private static bool IsTimedModifierOperation(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute
            or AbilityEffectOperation.ModifyThreat
            or AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyRegenerationInterval
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition;

    private static bool IsPeriodicEffect(CompiledEffect effect) =>
        effect.IntervalTicks > 0 && effect.DurationTicks > 0;

    private void Log(
        RuntimeCombatant source,
        RuntimeCombatant? target,
        string sourceName,
        EventType eventType,
        int magnitude,
        string details,
        string? statsSource = null,
        bool countsAsActivation = false,
        int barrierAbsorbed = 0,
        int incomingRawDamage = 0,
        int avoidedDamage = 0,
        int typedMitigationPrevented = 0,
        int physicalMitigationPrevented = 0,
        int magicalMitigationPrevented = 0,
        int blockPrevented = 0,
        int damageReductionPrevented = 0,
        int damageAmplified = 0,
        int finalHealthDamage = 0)
    {
        if (!_captureEventLog)
            return;

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
            BarrierAbsorbed = barrierAbsorbed,
            IncomingRawDamage = incomingRawDamage,
            AvoidedDamage = avoidedDamage,
            TypedMitigationPrevented = typedMitigationPrevented,
            PhysicalMitigationPrevented = physicalMitigationPrevented,
            MagicalMitigationPrevented = magicalMitigationPrevented,
            BlockPrevented = blockPrevented,
            DamageReductionPrevented = damageReductionPrevented,
            DamageAmplified = damageAmplified,
            FinalHealthDamage = finalHealthDamage,
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

    private void TrackBalanceDamage(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int healthDamage,
        DamageDelivery delivery)
    {
        if (_captureEventLog
            || delivery == DamageDelivery.Reflected
            || healthDamage <= 0
            || source.Team == target.Team)
            return;

        _balanceDamageDone[source.Id] = _balanceDamageDone.GetValueOrDefault(source.Id) + healthDamage;
        _balanceDamageTaken[target.Id] = _balanceDamageTaken.GetValueOrDefault(target.Id) + healthDamage;
    }

    private static BattleOutcome DetermineOutcome(IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!HasLivingTeam(combatants, CombatTeam.Friendly))
            return BattleOutcome.Defeat;

        if (!HasLivingTeam(combatants, CombatTeam.Hostile))
            return BattleOutcome.Victory;

        return BattleOutcome.Draw;
    }

    private sealed record CombatEvent(
        AbilityTriggerEvent Event,
        RuntimeCombatant? Source,
        RuntimeCombatant? Target,
        string? AbilityId,
        int Magnitude = 0,
        RuntimeCombatant? Instigator = null,
        long? BarrierApplicationOrder = null,
        ConditionRemovalReason? RemovalReason = null,
        DamageType DamageType = DamageType.None,
        AttackType AttackType = AttackType.None,
        bool WasCritical = false,
        bool WasDirectHit = false);

    private sealed class EffectExecutionContext
    {
        private readonly Dictionary<string, int> _generatedHealingByEffect =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _summonGroupInstances =
            new(StringComparer.OrdinalIgnoreCase);

        public string ActivationId { get; } = Guid.NewGuid().ToString("N");

        public int GetGeneratedHealing(string effectId) =>
            _generatedHealingByEffect.GetValueOrDefault(effectId);

        public void AddGeneratedHealing(string effectId, int amount) =>
            _generatedHealingByEffect[effectId] = GetGeneratedHealing(effectId) + Math.Max(0, amount);

        public string GetSummonGroupInstanceId(string groupId)
        {
            if (_summonGroupInstances.TryGetValue(groupId, out var existing))
                return existing;

            var created = $"{groupId}:{Guid.NewGuid():N}";
            _summonGroupInstances[groupId] = created;
            return created;
        }
    }

    private sealed class RuntimeSummonGroup(
        string instanceId,
        string groupId,
        RuntimeCombatant owner,
        int expiresAtTick)
    {
        public string InstanceId { get; } = instanceId;
        public string GroupId { get; } = groupId;
        public RuntimeCombatant Owner { get; } = owner;
        public int ExpiresAtTick { get; } = expiresAtTick;
        public List<RuntimeCombatant> Members { get; } = [];
    }

    private enum ConditionRemovalReason
    {
        Expired,
        Removed,
        Cleansed,
        Dispelled,
        Consumed
    }
}
