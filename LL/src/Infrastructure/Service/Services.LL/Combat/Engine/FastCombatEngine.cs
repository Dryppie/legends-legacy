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
    bool StartActiveAbilitiesOnCooldown = false);

public sealed class FastCombatEngine
{
    public const int TicksPerSecond = 10;
    private const int HealthRegenerationIntervalSeconds = 5;
    private const int HealthRegenerationIntervalTicks =
        TicksPerSecond * HealthRegenerationIntervalSeconds;

    private readonly IReadOnlyDictionary<string, CompiledStatus> _statusesById;
    private readonly IReadOnlyDictionary<string, CompiledSummon> _summonsById;
    private readonly IReadOnlyDictionary<string, CompiledAbility> _abilitiesById;
    private readonly Random _random;
    private readonly int _maxTicks;
    private readonly int _basicAttackIntervalTicks;
    private readonly bool _startActiveAbilitiesOnCooldown;
    private readonly Dictionary<RuntimeCombatant, float> _basicAttackProgress = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationProgress = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationPotential = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationOverhealed = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationPulses = [];
    private readonly List<CombatLogItem> _log = [];
    private int _currentTick;

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
        _maxTicks = resolved.MaxTicks;
        _basicAttackIntervalTicks = resolved.BasicAttackIntervalTicks;
        _startActiveAbilitiesOnCooldown = resolved.StartActiveAbilitiesOnCooldown;
    }

    public CombatResult Run(
        IReadOnlyList<RuntimeCombatant> friendly,
        IReadOnlyList<RuntimeCombatant> hostile,
        CancellationToken cancellationToken = default)
    {
        var combatants = friendly.Concat(hostile).ToList();
        foreach (var combatant in combatants)
        {
            _basicAttackProgress[combatant] = 0;
            _healthRegenerationProgress[combatant] = 0;
            InitializeActiveAbilityCooldowns(combatant);
        }

        Publish(new CombatEvent(AbilityTriggerEvent.OnCombatStart, null, null, null), combatants);

        while (_currentTick < _maxTicks
               && HasLivingTeam(combatants, CombatTeam.Friendly)
               && HasLivingTeam(combatants, CombatTeam.Hostile))
        {
            if ((_currentTick & 63) == 0)
                cancellationToken.ThrowIfCancellationRequested();

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
            TickHealthRegeneration(combatants);

            foreach (var combatant in combatants)
                combatant.Tick();

            TickSummons(combatants);
            _currentTick++;
        }

        var teamsByEntityId = combatants.ToDictionary(
            combatant => combatant.Id,
            combatant => combatant.Team.ToString(),
            StringComparer.OrdinalIgnoreCase);
        var entityStats = AddHealthRegenerationTelemetry(
            new CombatStatsAggregator().Aggregate(_log, teamsByEntityId),
            combatants);

        return new CombatResult
        {
            EventLog = [.. _log],
            Duration = _currentTick,
            Outcome = DetermineOutcome(combatants),
            EntityStats = [.. entityStats]
        };
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
            Publish(new CombatEvent(AbilityTriggerEvent.OnAbilityUsed, actor, null, ability.Definition.Id), combatants);
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

        var damage = Math.Max(
            1,
            (int)Math.Round(
                (Math.Max(1, actor.GetAttribute(AttributeType.WeaponDamage))
                 + actor.GetAttribute(AttributeType.Power) * AttributeCombatRules.BasicAttackPowerCoefficient) *
                actor.BasicAttackDamageMultiplier));
        Log(actor, null, "Basic Attack", EventType.AbilityUse, 0, $"{actor.Name} used Basic Attack");
        Publish(new CombatEvent(AbilityTriggerEvent.OnBasicAttack, actor, target, null), combatants);
        ApplyDamage(
            actor,
            target,
            damage,
            actor.BasicAttackType,
            actor.BasicAttackDamageType,
            null,
            combatants,
            "Basic Attack");
    }

    private static bool IsActionBlocked(RuntimeCombatant combatant) =>
        combatant.Statuses.Any(status => status.Stacks > 0 && status.Definition.Tags.Contains("Control.Stun"));

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
            .Where(trigger => ConditionsPass(trigger.Conditions, actor, combatEvent))
            .SelectMany(trigger => trigger.Effects)
            .Any(effect => SelectTargets(actor, effect.Target, combatEvent, combatants)
                .Any(target => target.IsAlive
                    && EffectCanResolve(effect, actor, combatants)
                    && ConditionsPass(effect.Conditions, actor, combatEvent with { Target = target })));
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
                    actor.AdjustBarrier(-value);
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

                    if (!IsSourceScopedTriggerRelevant(combatant, combatEvent))
                        continue;

                    foreach (var trigger in ability.Definition.TriggersByEvent[combatEvent.Event])
                    {
                        if (!ability.CanUseTrigger(trigger) || !ConditionsPass(trigger.Conditions, combatant, combatEvent))
                            continue;

                        ability.StartTriggerCooldown(trigger);
                        ExecuteTrigger(
                            trigger,
                            combatant,
                            combatEvent,
                            combatants,
                            ability.CanUseEffect,
                            ability.MarkEffectUsed,
                            countStatsActivation: ability.Definition.Kind == AbilitySpecKind.Passive);
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

                    if (!status.CanUseTrigger(trigger) || !ConditionsPass(trigger.Conditions, status.Source, combatEvent))
                        continue;

                    status.StartTriggerCooldown(trigger);
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
            }
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
                markEffectUsed(effect);
                ExecuteEffect(
                    effect,
                    source,
                    target,
                    combatants,
                    statsSourceOverride,
                    countThisActivation,
                    durationMultiplier);
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
        string? statsSourceOverride = null,
        bool countStatsActivation = false,
        double durationMultiplier = 1d)
    {
        var statsSource = statsSourceOverride ?? effect.StatsSource;
        if (effect.IntervalTicks > 0 && effect.DurationTicks > 0)
        {
            target.ActiveEffects.Add(new RuntimeEffect(effect, source, target, statsSource, durationMultiplier));
            return;
        }

        ApplyEffectOnce(effect, source, target, combatants, statsSource, countStatsActivation);

        if (effect.DurationTicks > 0 && effect.Operation == AbilityEffectOperation.ModifyAttribute)
            target.ActiveEffects.Add(new RuntimeEffect(effect, source, target, statsSource, durationMultiplier));
    }

    private void ApplyEffectOnce(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSourceOverride = null,
        bool countStatsActivation = false)
    {
        var value = CalculateValue(effect, source);
        var statsSource = statsSourceOverride ?? effect.StatsSource;

        switch (effect.Operation)
        {
            case AbilityEffectOperation.Damage:
                var healthDamage = ApplyDamage(source, target, value, effect.AttackType, effect.DamageType, effect, combatants, effect.Id, statsSource, countStatsActivation);
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
                                     && RollCriticalStrike(source)
                    ? ApplyCriticalMultiplier(source, value)
                    : value;
                target.AdjustBarrier(grantedBarrier);
                Log(source, target, effect.Id, EventType.RestoreBarrier, grantedBarrier, $"{source.Name} granted {grantedBarrier} barrier to {target.Name}.", statsSource, countStatsActivation);
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
                                          && RollCriticalStrike(source)
                        ? ApplyCriticalMultiplier(source, value)
                        : value;
                    target.AdjustBarrier(restoredBarrier);
                    Log(source, target, effect.Id, EventType.RestoreBarrier, restoredBarrier, $"{source.Name} restored {restoredBarrier} barrier to {target.Name}.", statsSource, countStatsActivation);
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
        bool countStatsActivation = false)
    {
        if (CanDodge(attackType) && target.GetAttribute(AttributeType.DodgeChance) > 0)
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

        var isCritical = CanCrit(effect, AbilityEffectOperation.Damage)
                         && RollCriticalStrike(source);
        var criticalDamage = isCritical
            ? ApplyCriticalMultiplier(source, damage)
            : damage;
        var typedDamage = ApplyTypedDefense(source, target, criticalDamage, damageType);
        var typedMitigationPrevented = Math.Max(0, criticalDamage - typedDamage);
        var physicalMitigationPrevented = damageType is DamageType.Physical or DamageType.Bleed
            ? typedMitigationPrevented
            : 0;
        var magicalMitigationPrevented = damageType is DamageType.Magical
            or DamageType.Burn
            or DamageType.Poison
                ? typedMitigationPrevented
                : 0;
        var blocked = CanBlock(attackType) && RollBlock(target);
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
        var damageAmplified = Math.Max(0, reducedDamage - blockedDamage);
        var barrierBefore = target.Barrier;
        var absorbed = Math.Min(barrierBefore, reducedDamage);
        target.AdjustBarrier(-absorbed);
        var barrierAbsorbed = (int)absorbed;
        var pendingHealthDamage = Math.Max(0, reducedDamage - barrierAbsorbed);
        var healthBefore = target.Health;
        target.AdjustHealth(-pendingHealthDamage);
        var healthDamage = Math.Max(0, (int)Math.Round(healthBefore - target.Health));

        Log(
            source,
            target,
            sourceName,
            isCritical ? EventType.DamageCrit : EventType.Damage,
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
        Publish(new CombatEvent(AbilityTriggerEvent.OnHit, source, target, null), combatants);
        PublishAttackTypeEvents(source, target, attackType, combatants);
        Publish(new CombatEvent(AbilityTriggerEvent.OnDamaged, target, source, null), combatants);
        Publish(new CombatEvent(AbilityTriggerEvent.OnAttacked, target, source, null), combatants);
        if (healthDamage > 0)
            Publish(new CombatEvent(AbilityTriggerEvent.OnHealthChanged, target, source, null), combatants);

        if (!target.IsAlive)
        {
            Log(source, target, sourceName, EventType.Death, 0, $"{target.Name} was killed by {source.Name}.", statsSource);
            Publish(new CombatEvent(AbilityTriggerEvent.OnKill, source, target, null), combatants);
            Publish(new CombatEvent(AbilityTriggerEvent.OnDeath, target, source, null), combatants);
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

    private bool RollBlock(RuntimeCombatant target)
    {
        var blockChance = Math.Clamp(
            target.GetAttribute(AttributeType.BlockChance),
            0,
            AttributeCatalog.GetFixedCap(AttributeType.BlockChance));
        return blockChance > 0 && _random.NextDouble() * 100 < blockChance;
    }

    private bool RollCriticalStrike(RuntimeCombatant source)
    {
        var critChance = Math.Clamp(
            source.GetAttribute(AttributeType.CritChance),
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
        DamageType damageType)
    {
        var (defenseAttribute, penetrationAttribute) = damageType switch
        {
            DamageType.Physical or DamageType.Bleed =>
                (AttributeType.Armor, AttributeType.ArmorPenetration),
            DamageType.Magical or DamageType.Burn or DamageType.Poison =>
                (AttributeType.Resistance, AttributeType.MagicPenetration),
            _ => ((AttributeType?)null, (AttributeType?)null)
        };

        if (defenseAttribute is null || penetrationAttribute is null)
            return damage;

        var mitigation = AttributeCombatRules.CalculateDefenseMitigation(
            target.GetAttribute(defenseAttribute.Value),
            source.GetAttribute(penetrationAttribute.Value));
        return Math.Max(0, (int)Math.Round(damage * (1 - mitigation)));
    }

    private void PublishAttackTypeEvents(
        RuntimeCombatant source,
        RuntimeCombatant target,
        AttackType attackType,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        switch (attackType)
        {
            case AttackType.Melee:
                Publish(new CombatEvent(AbilityTriggerEvent.OnMeleeAttack, source, target, null), combatants);
                Publish(new CombatEvent(AbilityTriggerEvent.OnMeleeAttacked, target, source, null), combatants);
                break;
            case AttackType.Ranged:
                Publish(new CombatEvent(AbilityTriggerEvent.OnRangedAttack, source, target, null), combatants);
                Publish(new CombatEvent(AbilityTriggerEvent.OnRangedAttacked, target, source, null), combatants);
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
        bool countStatsActivation = false)
    {
        var healingPowerMultiplier =
            Math.Max(0, 1 + source.GetAttribute(AttributeType.HealingPowerPercent) / 100f);
        var modifiedValue = Math.Max(0, (int)Math.Round(value * healingPowerMultiplier));
        var isCritical = !isLifeSteal
                         && CanCrit(effect, AbilityEffectOperation.Heal)
                         && RollCriticalStrike(source);
        if (isCritical)
            modifiedValue = ApplyCriticalMultiplier(source, modifiedValue);

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
        var lifeStealPercentage = Math.Clamp(
            source.GetAttribute(AttributeType.LifeSteal) + effect.LifeStealPercentage,
            0,
            AttributeCatalog.GetFixedCap(AttributeType.LifeSteal));
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
            effect.Id,
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

    private static int CalculateStatusDuration(
        CompiledStatus statusDefinition,
        RuntimeCombatant target)
    {
        if (statusDefinition.DurationTicks <= 0)
            return statusDefinition.DurationTicks;

        var resistanceAttribute = statusDefinition.Tags.Contains("Control.Stun")
            ? AttributeType.CrowdControlResistance
            : AttributeType.StatusResistance;
        var resistance = Math.Max(0, target.GetAttribute(resistanceAttribute));
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
        bool countStatsActivation)
    {
        if (string.IsNullOrWhiteSpace(effect.SummonId))
            throw new InvalidOperationException($"Summon effect '{effect.Id}' requires summonId.");

        if (combatants is not List<RuntimeCombatant> mutableCombatants)
            throw new InvalidOperationException("Summon effects require a mutable combatant list.");

        if (!_summonsById.TryGetValue(effect.SummonId, out var summonDefinition))
            throw new InvalidOperationException($"Summon '{effect.SummonId}' has not been compiled.");

        if (HasReachedSummonCap(source, summonDefinition, combatants))
            return;

        var summon = CreateSummonedCombatant(source, effect, summonDefinition, _abilitiesById);
        mutableCombatants.Add(summon);
        _basicAttackProgress[summon] = GetBasicAttackChargeThreshold();
        _healthRegenerationProgress[summon] = 0;

        Log(source, summon, effect.Id, EventType.Summon, 1, $"{source.Name} summoned {summon.Name}.", statsSource, countStatsActivation);
    }

    private int GetBasicAttackChargeThreshold() => Math.Max(1, _basicAttackIntervalTicks);

    private static float GetBasicAttackRate(RuntimeCombatant actor)
        => AttributeCombatRules.CalculateBasicAttackRate(
            actor.GetAttribute(AttributeType.AttackSpeed),
            actor.BasicAttackIntervalMultiplier);

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
        IReadOnlyDictionary<string, CompiledAbility> abilitiesById)
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
            summonOwner: source);
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
                        ? source.GetAttribute(scalingAttribute) * attribute.ScalingCoefficient
                        : 0))
                    * GetSummonAttributeMultiplier(attribute.Attribute, effect, source))));

        attributes.TryAdd(AttributeType.MaxHealth, 1);
        attributes.TryAdd(AttributeType.Power, 0);
        attributes.TryAdd(AttributeType.AttackSpeed, 0);
        return attributes;
    }

    private static double GetSummonAttributeMultiplier(
        AttributeType attribute,
        CompiledEffect effect,
        RuntimeCombatant source) =>
        attribute == AttributeType.MaxHealth
            ? Math.Max(0d, effect.SummonHealthMultiplier)
              * Math.Max(0d, 1d + source.GetAttribute(AttributeType.SummonHealth) / 100d)
            : attribute == AttributeType.Power
                ? Math.Max(0d, effect.SummonPowerMultiplier)
                  * Math.Max(0d, 1d + source.GetAttribute(AttributeType.SummonPower) / 100d)
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

        existing.AddStacks(amount);
        if (existing.Stacks <= 0)
            RemoveStatus(source, target, statusId, combatants);
    }

    private void RemoveStatus(
        RuntimeCombatant source,
        RuntimeCombatant target,
        string statusId,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var status in target.Statuses
                     .Where(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            ExpireStatus(source, target, status, combatants);
        }
    }

    private void CleanseStatuses(
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var status in target.Statuses.ToList())
            ExpireStatus(source, target, status, combatants);
    }

    private void ExpireStatus(
        RuntimeCombatant source,
        RuntimeCombatant target,
        RuntimeStatus status,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!target.Statuses.Remove(status))
            return;

        Log(source, target, status.Definition.Id, EventType.StatusEffectExpired, 0, $"{status.Definition.Id} expired on {target.Name}.");
        Publish(new CombatEvent(AbilityTriggerEvent.OnStatusExpired, status.Source, target, status.Definition.Id), combatants);
    }

    private void TickEffects(IReadOnlyList<RuntimeCombatant> combatants)
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

    private void TickStatuses(IReadOnlyList<RuntimeCombatant> combatants)
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

    private void TickHealthRegeneration(IReadOnlyList<RuntimeCombatant> combatants)
    {
        foreach (var combatant in combatants.Where(x => x.IsAlive))
        {
            var progress = _healthRegenerationProgress.GetValueOrDefault(combatant) + 1;
            if (progress < HealthRegenerationIntervalTicks)
            {
                _healthRegenerationProgress[combatant] = progress;
                continue;
            }

            _healthRegenerationProgress[combatant] = 0;

            var regeneration = Math.Max(
                0,
                combatant.GetAttribute(AttributeType.HealthRegeneration));
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
        }
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

    private void TickSummons(IReadOnlyList<RuntimeCombatant> combatants)
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

    private static bool IsSourceScopedTriggerRelevant(RuntimeCombatant listener, CombatEvent combatEvent) =>
        combatEvent.Event switch
        {
            AbilityTriggerEvent.OnMeleeAttack
                or AbilityTriggerEvent.OnBasicAttack
                or AbilityTriggerEvent.OnRangedAttack
                or AbilityTriggerEvent.OnMeleeAttacked
                or AbilityTriggerEvent.OnRangedAttacked
                or AbilityTriggerEvent.OnDamaged
                or AbilityTriggerEvent.OnAttacked
                or AbilityTriggerEvent.OnHeal
                or AbilityTriggerEvent.OnHealed
                or AbilityTriggerEvent.OnLifestealHeal => ReferenceEquals(combatEvent.Source, listener),
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
            AbilityTargetSelector.CurrentTarget => SelectFirstEnemy(source, combatants) is { } target ? [target] : [],
            AbilityTargetSelector.RandomEnemy => SelectRandomEnemy(source, combatants) is { } target ? [target] : [],
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
            _ => []
        };
    }

    private bool EffectCanResolve(
        CompiledEffect effect,
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
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

    private RuntimeCombatant? SelectFirstEnemy(RuntimeCombatant source, IReadOnlyList<RuntimeCombatant> combatants)
    {
        var enemies = combatants.Where(x => x.Team != source.Team && x.IsAlive).ToList();
        return enemies.FirstOrDefault(IsTaunting) ?? enemies.FirstOrDefault();
    }

    private RuntimeCombatant? SelectRandomEnemy(RuntimeCombatant source, IReadOnlyList<RuntimeCombatant> combatants)
    {
        var enemies = combatants.Where(x => x.Team != source.Team && x.IsAlive).ToList();
        var tauntingEnemies = enemies.Where(IsTaunting).ToList();
        if (tauntingEnemies.Count > 0)
            enemies = tauntingEnemies;

        return enemies.Count == 0 ? null : enemies[_random.Next(enemies.Count)];
    }

    private static bool IsTaunting(RuntimeCombatant combatant) =>
        combatant.Statuses.Any(status =>
            status.Definition.Id.Equals("status.taunt", StringComparison.OrdinalIgnoreCase)
            || status.Definition.Tags.Contains("Control.Taunt"));

    private bool ConditionsPass(
        IEnumerable<CompiledCondition> conditions,
        RuntimeCombatant source,
        CombatEvent combatEvent) =>
        conditions.All(condition => ConditionPass(condition, source, combatEvent));

    private bool ConditionPass(CompiledCondition condition, RuntimeCombatant source, CombatEvent combatEvent)
    {
        var subject = ResolveSubject(condition.Subject, source, combatEvent);
        if (subject is null)
            return false;

        return condition.Type switch
        {
            AbilityConditionType.Always => true,
            AbilityConditionType.HealthBelowPercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 < condition.Value,
            AbilityConditionType.HealthAbovePercent => subject.GetAttribute(AttributeType.MaxHealth) > 0
                && subject.Health / subject.GetAttribute(AttributeType.MaxHealth) * 100 > condition.Value,
            AbilityConditionType.HasStatus => subject.GetStatusStacks(condition.StatusId!) > 0,
            AbilityConditionType.StatusStacksAtLeast => subject.GetStatusStacks(condition.StatusId!) >= condition.Value,
            AbilityConditionType.HasTag => subject.Tags.Contains(condition.Tag!),
            AbilityConditionType.ChancePercent => _random.Next(1, 101) <= condition.Value,
            _ => false
        };
    }

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

    private static int CalculateValue(CompiledEffect effect, RuntimeCombatant source) =>
        Math.Max(AllowsNegativeValue(effect.Operation) ? int.MinValue : 0,
            (int)Math.Round(effect.BaseValue + (effect.ScalingAttribute is { } attribute
                ? source.GetAttribute(attribute) * effect.ScalingCoefficient
                : 0)));

    private static int CalculateCostValue(CompiledCost cost, RuntimeCombatant source) =>
        Math.Max(0, (int)Math.Round(cost.BaseValue + (cost.ScalingAttribute is { } attribute
            ? source.GetAttribute(attribute) * cost.ScalingCoefficient
            : 0)));

    private static bool AllowsNegativeValue(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute or AbilityEffectOperation.ModifyStatusStacks;

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
        string? AbilityId);
}
