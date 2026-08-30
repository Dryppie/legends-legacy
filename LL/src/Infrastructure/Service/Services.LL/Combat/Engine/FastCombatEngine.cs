using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Services.LL.Combat.Stats;
using Services.LL.Interfaces.Combat.Resolution;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Services.LL.Combat.Engine;

public sealed record FastCombatEngineOptions(
    int MaxTicks = 6000,
    int BasicAttackIntervalTicks = 30,
    int RandomSeed = 1337,
    bool StartActiveAbilitiesOnCooldown = false,
    float MarkThreatBonus = 100f,
    bool CaptureEventLog = true,
    int? OvertimeStartsAtTick = null,
    int OvertimePowerIncreaseIntervalTicks = 0,
    float OvertimePowerIncreasePercent = 0,
    bool ThreatAndTankingEnabled = true,
    double AttentionExponent = 2.5d,
    double MinimumAttentionWeight = 0.05d,
    double MaximumAttentionWeight = 20d,
    double ThreatHalfLifeSeconds = 15d,
    int BasicAttackThreatValue = 8,
    float CoverBudgetMaxHealthFraction = 0.5f,
    CombatDownedOptions? Downed = null,
    CombatWaveRecoveryOptions? WaveRecovery = null,
    CombatHostileFuryOptions? HostileFury = null,
    bool CaptureCompactTelemetry = true);

public sealed class FastCombatEngine
{
    public const int TicksPerSecond = 10;
    internal const double CombatMagnitudeVariance = 0.2d;
    private const int MagnitudeRandomSeedSalt = unchecked((int)0x9E3779B9);
    private const int TargetingRandomSeedSalt = unchecked((int)0x6A09E667);
    private const int HealthRegenerationIntervalSeconds = 5;
    private const int HealthRegenerationIntervalTicks =
        TicksPerSecond * HealthRegenerationIntervalSeconds;

    private readonly IReadOnlyDictionary<string, CompiledStatus> _statusesById;
    private readonly IReadOnlyDictionary<string, CompiledSummon> _summonsById;
    private readonly IReadOnlyDictionary<string, CompiledAbility> _abilitiesById;
    private readonly Random _random;
    private readonly Random _magnitudeRandom;
    private readonly Random _targetingRandom;
    private readonly int _maxTicks;
    private readonly int _basicAttackIntervalTicks;
    private readonly bool _startActiveAbilitiesOnCooldown;
    private readonly float _markThreatBonus;
    private readonly bool _captureEventLog;
    private readonly bool _captureCompactTelemetry;
    private readonly int _overtimeStartsAtTick;
    private readonly int _overtimePowerIncreaseIntervalTicks;
    private readonly float _overtimePowerIncreasePercent;
    private readonly bool _threatAndTankingEnabled;
    private readonly double _attentionExponent;
    private readonly double _minimumAttentionWeight;
    private readonly double _maximumAttentionWeight;
    private readonly double _threatDecayPerTick;
    private readonly int _basicAttackThreatValue;
    private readonly float _coverBudgetMaxHealthFraction;
    private readonly CombatDownedOptions? _downedOptions;
    private readonly CombatWaveRecoveryOptions? _waveRecoveryOptions;
    private readonly CombatHostileFuryOptions? _hostileFuryOptions;
    private readonly Dictionary<RuntimeCombatant, float> _basicAttackProgress = [];
    private readonly Dictionary<RuntimeCombatant, float> _healthRegenerationProgress = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationPotential = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationOverhealed = [];
    private readonly Dictionary<RuntimeCombatant, int> _healthRegenerationPulses = [];
    private readonly Dictionary<MaintainedThreatSourceKey, float> _maintainedThreatRates = [];
    private readonly Dictionary<MaintainedThreatSourceKey, double> _maintainedThreatRemainders = [];
    private readonly Dictionary<RuntimeCombatant, ThreatGenerationTelemetry> _threatGeneration = [];
    private readonly List<CombatLogItem> _log = [];
    private readonly Dictionary<string, int> _balanceDamageDone = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _balanceDamageTaken = new(StringComparer.OrdinalIgnoreCase);
    private readonly CombatStatsAccumulator _balanceStats;
    private readonly Dictionary<string, RuntimeSummonGroup> _summonGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuntimeEffect> _effectTickBuffer = [];
    private readonly List<RuntimeStatus> _statusTickBuffer = [];
    private readonly List<RuntimeCondition> _conditionTickBuffer = [];
    private readonly List<RuntimeBarrierContribution> _barrierTickBuffer = [];
    private readonly List<RuntimeCover> _coverTickBuffer = [];
    private readonly List<RuntimeSummonGroup> _summonGroupTickBuffer = [];
    private readonly List<RuntimeCombatant> _summonTickBuffer = [];
    private readonly List<RuntimeCondition> _thornsBuffer = [];
    private CombatStatsAccumulator? _checkpointStats;
    private int _currentTick;
    private long _applicationOrder;
    private long _activationSequence;
    private long _summonSequence;
    private ulong _listenerMask;
    private int _eventDepth;
    private bool _forceCheckpoint;
    private int _currentWaveNumber = 1;
    private readonly Dictionary<RuntimeCombatant, int> _deathCounts = [];
    private readonly Dictionary<RuntimeCombatant, int> _revivalCounts = [];
    private readonly Dictionary<RuntimeCombatant, int> _reviveAtTicks = [];
    private readonly Dictionary<RuntimeCombatant, int> _downedTicks = [];
    private readonly Dictionary<RuntimeCombatant, int> _actionDeniedTicks = [];
    private readonly Dictionary<RuntimeCombatant, int> _staggeredTicks = [];
    private readonly Dictionary<RuntimeCombatant, int> _stunnedOrFrozenTicks = [];
    private readonly Dictionary<RuntimeCombatant, int> _silencedTicks = [];
    private readonly Dictionary<RuntimeCombatant, int> _slowedTicks = [];
    private int _peakActiveFriendlyCombatants;
    private int _peakActiveHostileCombatants;
    private int _peakActiveFriendlySummons;
    private int _peakActiveHostileSummons;
    private int _peakActiveHostileTick;
    private int? _firstAdditionalHostileTick;
    private int? _firstAdditionalHostileClearTick;
    private bool _hostileSummonWindowActive;
    private int _additionalHostileWindowCount;
    private int _clearedAdditionalHostileWindowCount;
    private int _hostileSummonActiveTicks;
    private int _lastHostileSummonUptimeTick = -1;
    private int _hostileSummonWaveCount;
    private int _hostileSummonWaveIntervalCount;
    private int _hostileSummonWaveIntervalTotalTicks;
    private int? _lastHostileSummonWaveTick;
    private int? _minimumHostileSummonWaveIntervalTicks;
    private int? _maximumHostileSummonWaveIntervalTicks;
    private int _initialFriendlyHealthDeficitSampleTicks;
    private double _initialFriendlyHealthDeficitRatioTotal;

    private enum DamageDelivery
    {
        Direct,
        Periodic,
        Reflected,
        Stored,
        Self,
        Redirected
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
        _targetingRandom = new Random(
            unchecked(resolved.RandomSeed ^ TargetingRandomSeedSalt));
        _maxTicks = resolved.MaxTicks;
        _basicAttackIntervalTicks = resolved.BasicAttackIntervalTicks;
        _startActiveAbilitiesOnCooldown = resolved.StartActiveAbilitiesOnCooldown;
        _markThreatBonus = Math.Max(0, resolved.MarkThreatBonus);
        _captureEventLog = resolved.CaptureEventLog;
        _captureCompactTelemetry = resolved.CaptureCompactTelemetry;
        _balanceStats = new CombatStatsAccumulator(_captureCompactTelemetry);
        _overtimeStartsAtTick = resolved.OvertimeStartsAtTick ?? int.MaxValue;
        _overtimePowerIncreaseIntervalTicks = Math.Max(0, resolved.OvertimePowerIncreaseIntervalTicks);
        _overtimePowerIncreasePercent = Math.Max(0, resolved.OvertimePowerIncreasePercent);
        _threatAndTankingEnabled = resolved.ThreatAndTankingEnabled;
        _attentionExponent = Math.Max(1d, resolved.AttentionExponent);
        _minimumAttentionWeight = Math.Max(0.0001d, resolved.MinimumAttentionWeight);
        _maximumAttentionWeight = Math.Max(_minimumAttentionWeight, resolved.MaximumAttentionWeight);
        _threatDecayPerTick = resolved.ThreatHalfLifeSeconds <= 0
            ? 0d
            : 1d - Math.Pow(0.5d, 1d / (resolved.ThreatHalfLifeSeconds * TicksPerSecond));
        _basicAttackThreatValue = resolved.BasicAttackThreatValue;
        _coverBudgetMaxHealthFraction = Math.Max(0, resolved.CoverBudgetMaxHealthFraction);
        _downedOptions = resolved.Downed;
        _waveRecoveryOptions = resolved.WaveRecovery;
        _hostileFuryOptions = resolved.HostileFury;
    }

    public CombatResult Run(
        IReadOnlyList<RuntimeCombatant> friendly,
        IReadOnlyList<RuntimeCombatant> hostile,
        CancellationToken cancellationToken = default,
        Action<CombatCheckpoint>? checkpointObserver = null,
        int checkpointIntervalTicks = 0,
        IReadOnlyList<IReadOnlyList<RuntimeCombatant>>? hostileReinforcementWaves = null,
        Func<int, IReadOnlyList<RuntimeCombatant>?>? hostileWaveFactory = null)
    {
        var combatants = friendly.Concat(hostile).ToList();
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
            InitializeEncounterCombatant(combatants[combatantIndex]);

        var reinforcementWaves = hostileReinforcementWaves ?? [];
        var nextReinforcementWave = 0;
        if (!HasLivingTeam(combatants, CombatTeam.Hostile))
        {
            SpawnNextHostileWave(
                combatants,
                reinforcementWaves,
                ref nextReinforcementWave,
                publishCombatStart: false,
                hostileWaveFactory);
        }

        var checkpointSequence = 0;
        var checkpointLogIndex = 0;
        var checkpointStats = checkpointObserver is not null && checkpointIntervalTicks > 0
            ? new CombatStatsAccumulator(_captureCompactTelemetry)
            : null;
        _checkpointStats = checkpointStats;
        if (_captureCompactTelemetry)
            TrackCompactTelemetry(combatants);
        Publish(new CombatEvent(AbilityTriggerEvent.OnCombatStart, null, null, null), combatants);
        if (checkpointObserver is not null && checkpointIntervalTicks > 0)
        {
            checkpointObserver(CreateCheckpoint(
                combatants,
                checkpointStats!,
                checkpointSequence++,
                checkpointLogIndex,
                false));
            checkpointLogIndex = _log.Count;
        }

        while (_currentTick < _maxTicks
               && HasLivingTeam(combatants, CombatTeam.Friendly)
               && HasLivingTeam(combatants, CombatTeam.Hostile))
        {
            TickDownedCombatants(combatants);
            if ((_currentTick & 63) == 0)
                cancellationToken.ThrowIfCancellationRequested();

            PublishIntervalEvents(combatants);
            TickStaggerStates(combatants);
            GenerateMaintainedThreat(combatants);

            var actingCombatantCount = combatants.Count;
            for (var combatantIndex = 0; combatantIndex < actingCombatantCount; combatantIndex++)
            {
                var combatant = combatants[combatantIndex];
                if (!combatant.IsAlive)
                    continue;
                if (_captureCompactTelemetry)
                    TrackControlExposure(combatant);
                if (IsActionBlocked(combatant) || !HasLivingOpponent(combatant, combatants))
                    continue;

                if (!IsActiveAbilityBlocked(combatant))
                    UseReadyActiveAbilities(combatant, combatants);

                if (combatant.CanBasicAttack && HasLivingOpponent(combatant, combatants))
                    TickBasicAttack(combatant, combatants);
            }

            TickEffects(combatants);
            TickStatuses(combatants);
            TickConditions(combatants);
            TickHealthRegeneration(combatants);
            TickBarrierContributions(combatants);
            TickCovers(combatants);

            for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
                combatants[combatantIndex].Tick();

            TickSummons(combatants);
            if (_captureCompactTelemetry)
            {
                TrackCompactTelemetry(combatants);
                TrackInitialFriendlyHealthDeficit(friendly);
            }
            _currentTick++;

            var spawnedReinforcementWave = false;
            if (HasLivingTeam(combatants, CombatTeam.Friendly)
                && !HasLivingTeam(combatants, CombatTeam.Hostile))
            {
                spawnedReinforcementWave = SpawnNextHostileWave(
                    combatants,
                    reinforcementWaves,
                    ref nextReinforcementWave,
                    publishCombatStart: true,
                    hostileWaveFactory);
                if (_captureCompactTelemetry && spawnedReinforcementWave)
                    TrackCompactTelemetry(combatants);
                if (spawnedReinforcementWave
                    && checkpointObserver is not null
                    && checkpointIntervalTicks > 0)
                {
                    checkpointObserver(CreateCheckpoint(
                        combatants,
                        checkpointStats!,
                        checkpointSequence++,
                        checkpointLogIndex,
                        false));
                    checkpointLogIndex = _log.Count;
                }
            }

            if (checkpointObserver is not null
                && checkpointIntervalTicks > 0
                && !spawnedReinforcementWave
                && (_forceCheckpoint || _currentTick % checkpointIntervalTicks == 0))
            {
                var isFinalCheckpoint = _currentTick >= _maxTicks
                    || !HasLivingTeam(combatants, CombatTeam.Friendly)
                    || !HasLivingTeam(combatants, CombatTeam.Hostile);
                checkpointObserver(CreateCheckpoint(
                    combatants,
                    checkpointStats!,
                    checkpointSequence++,
                    checkpointLogIndex,
                    isFinalCheckpoint));
                checkpointLogIndex = _log.Count;
                _forceCheckpoint = false;
            }
        }

        if (checkpointObserver is not null && checkpointIntervalTicks > 0)
        {
            if (_currentTick % checkpointIntervalTicks != 0)
            {
                checkpointObserver(CreateCheckpoint(
                    combatants,
                    checkpointStats!,
                    checkpointSequence,
                    checkpointLogIndex,
                    true));
            }
        }

        var entityStats = _captureEventLog || checkpointStats is not null
            ? CreateDetailedStats(combatants, checkpointStats)
            : CreateBalanceStats(combatants);

        return new CombatResult
        {
            EventLog = [.. _log],
            Duration = _currentTick,
            Outcome = DetermineOutcome(combatants),
            EntityStats = [.. entityStats],
            CompactTelemetry = _captureCompactTelemetry
                ? CreateCompactTelemetry(combatants)
                : new CompactCombatTelemetry()
        };
    }

    private void InitializeEncounterCombatant(RuntimeCombatant combatant)
    {
        RegisterListeners(combatant);
        _basicAttackProgress[combatant] = 0;
        _healthRegenerationProgress[combatant] = 0;
        InitializeActiveAbilityCooldowns(combatant);
    }

    private bool SpawnNextHostileWave(
        List<RuntimeCombatant> combatants,
        IReadOnlyList<IReadOnlyList<RuntimeCombatant>> reinforcementWaves,
        ref int nextWave,
        bool publishCombatStart,
        Func<int, IReadOnlyList<RuntimeCombatant>?>? hostileWaveFactory)
    {
        while (true)
        {
            IReadOnlyList<RuntimeCombatant>? wave;
            if (nextWave < reinforcementWaves.Count)
                wave = reinforcementWaves[nextWave++];
            else if (hostileWaveFactory is not null)
                wave = hostileWaveFactory(++_currentWaveNumber);
            else
                return false;

            if (wave is null)
                return false;
            if (wave.Count == 0)
                continue;

            if (publishCombatStart)
                ApplyWaveRecovery(combatants);

            for (var index = 0; index < wave.Count; index++)
            {
                var combatant = wave[index];
                if (combatant.Team != CombatTeam.Hostile)
                    throw new InvalidOperationException("Hostile reinforcement waves can contain only hostile combatants.");
                InitializeEncounterCombatant(combatant);
                combatants.Add(combatant);
            }

            if (publishCombatStart)
            {
                Publish(
                    new CombatEvent(AbilityTriggerEvent.OnCombatStart, null, null, null),
                    combatants,
                    wave);
            }

            return true;
        }
    }

    private void TickDownedCombatants(IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (_downedOptions is null)
            return;

        foreach (var combatant in combatants)
        {
            if (combatant.Team != CombatTeam.Friendly || combatant.IsAlive)
                continue;
            _downedTicks[combatant] = _downedTicks.GetValueOrDefault(combatant) + 1;
            if (!_reviveAtTicks.TryGetValue(combatant, out var reviveAt) || reviveAt > _currentTick)
                continue;
            Revive(combatant, _downedOptions.ReviveHealthPercent, combatants, "Revival");
        }
    }

    private void ApplyWaveRecovery(IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (_waveRecoveryOptions is null)
            return;

        foreach (var combatant in combatants.Where(x => x.Team == CombatTeam.Friendly && !x.IsSummoned))
        {
            if (combatant.IsAlive)
            {
                combatant.AdjustHealth(combatant.GetAttribute(AttributeType.MaxHealth)
                                       * _waveRecoveryOptions.LivingHealPercent / 100f);
                continue;
            }

            Revive(combatant, _waveRecoveryOptions.DownedReviveHealthPercent, combatants, "Level recovery");
        }
    }

    private void Revive(
        RuntimeCombatant combatant,
        float healthPercent,
        IReadOnlyList<RuntimeCombatant> combatants,
        string source)
    {
        combatant.AdjustHealth(combatant.GetAttribute(AttributeType.MaxHealth)
                               * Math.Clamp(healthPercent, 1, 100) / 100f);
        combatant.BarrierContributions.Clear();
        combatant.Covers.Clear();
        _reviveAtTicks.Remove(combatant);
        _revivalCounts[combatant] = _revivalCounts.GetValueOrDefault(combatant) + 1;
        Log(combatant, combatant, source, EventType.Revive, 0, $"{combatant.Name} returned to battle.");
        PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, combatant, combatant, null, combatants);
    }

    private CombatCheckpoint CreateCheckpoint(
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatStatsAccumulator stats,
        int sequence,
        int logIndex,
        bool isFinal)
    {
        var intervalEvents = _captureEventLog ? _log.Skip(logIndex).ToArray() : [];
        if (_captureEventLog)
        {
            var teams = combatants.ToDictionary(
                combatant => combatant.Id,
                combatant => combatant.Team.ToString(),
                StringComparer.OrdinalIgnoreCase);
            stats.AddRange(intervalEvents, teams);
        }
        var entityStats = AddLifecycleTelemetry(AddAttentionTelemetry(
            AddFinalCombatantState(
                AddThreatGenerationTelemetry(
                    AddHealthRegenerationTelemetry(
                        stats.Snapshot(),
                        combatants)),
                combatants),
            combatants), combatants);
        return new CombatCheckpoint(
            sequence,
            _currentTick,
            combatants.Where(x => x.Team == CombatTeam.Friendly).Select(ToSimpleEntity).ToArray(),
            combatants.Where(x => x.Team == CombatTeam.Hostile).Select(ToSimpleEntity).ToArray(),
            entityStats,
            intervalEvents,
            isFinal,
            new CombatCheckpointContext(
                _currentWaveNumber,
                GetFuryStacks(),
                _reviveAtTicks.Select(entry => new CombatDownedState(
                    entry.Key.Id,
                    _deathCounts.GetValueOrDefault(entry.Key),
                    entry.Value,
                    Math.Max(0, entry.Value - _currentTick))).ToArray()));
    }

    private static SimpleCombatEntity ToSimpleEntity(RuntimeCombatant combatant) => new()
    {
        Id = combatant.Id,
        Name = combatant.Name,
        Level = combatant.Level,
        ImagePath = combatant.ImagePath,
        Health = (int)combatant.Health,
        MaxHealth = (int)combatant.GetAttribute(AttributeType.MaxHealth),
        Barrier = (int)combatant.Barrier,
        Threat = combatant.Threat,
        PartyNumber = combatant.PartyNumber,
        CurrentStagger = combatant.Stagger?.Current ?? 0,
        MaxStagger = combatant.Stagger?.Max ?? 0,
        IsStaggered = combatant.Stagger?.IsStaggered == true,
        IsStaggerRecovering = combatant.Stagger?.IsRecovering == true
    };

    private IReadOnlyList<EntityStats> CreateDetailedStats(
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatStatsAccumulator? checkpointStats)
    {
        var teamsByEntityId = combatants.ToDictionary(
            combatant => combatant.Id,
            combatant => combatant.Team.ToString(),
            StringComparer.OrdinalIgnoreCase);
        return AddLifecycleTelemetry(AddAttentionTelemetry(
            AddFinalCombatantState(
                AddThreatGenerationTelemetry(
                    AddHealthRegenerationTelemetry(
                        checkpointStats?.Snapshot()
                        ?? new CombatStatsAggregator().Aggregate(
                            _log,
                            teamsByEntityId,
                            _captureCompactTelemetry),
                        combatants)),
                combatants),
            combatants), combatants);
    }

    private IReadOnlyList<EntityStats> CreateBalanceStats(IReadOnlyList<RuntimeCombatant> combatants) =>
        AddLifecycleTelemetry(AddAttentionTelemetry(
            AddFinalCombatantState(
                AddThreatGenerationTelemetry(
                    AddHealthRegenerationTelemetry(_balanceStats.Snapshot(), combatants)),
                combatants),
            combatants), combatants);

    private IReadOnlyList<EntityStats> AddLifecycleTelemetry(
        IReadOnlyList<EntityStats> aggregatedStats,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var result = aggregatedStats.ToList();
        foreach (var combatant in combatants)
        {
            var index = result.FindIndex(x => x.EntityId.Equals(combatant.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                continue;
            result[index] = result[index] with
            {
                Deaths = Math.Max(result[index].Deaths, _deathCounts.GetValueOrDefault(combatant)),
                Revivals = Math.Max(result[index].Revivals, _revivalCounts.GetValueOrDefault(combatant)),
                DownedTicks = _downedTicks.GetValueOrDefault(combatant),
                ActionDeniedTicks = _actionDeniedTicks.GetValueOrDefault(combatant),
                StaggeredTicks = _staggeredTicks.GetValueOrDefault(combatant),
                StunnedOrFrozenTicks = _stunnedOrFrozenTicks.GetValueOrDefault(combatant),
                SilencedTicks = _silencedTicks.GetValueOrDefault(combatant),
                SlowedTicks = _slowedTicks.GetValueOrDefault(combatant)
            };
        }
        return result;
    }

    private void PublishIntervalEvents(IReadOnlyList<RuntimeCombatant> combatants)
    {
        var intervalCombatantCount = combatants.Count;
        for (var combatantIndex = 0; combatantIndex < intervalCombatantCount; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            if (!combatant.IsAlive)
                continue;
            var hasAbilityListener =
                combatant.AbilityTriggersByEvent.ContainsKey(AbilityTriggerEvent.OnInterval);
            var hasStatusListener = false;
            for (var statusIndex = 0; statusIndex < combatant.Statuses.Count; statusIndex++)
            {
                if (!combatant.Statuses[statusIndex].Definition.TriggersByEvent.ContainsKey(
                        AbilityTriggerEvent.OnInterval))
                {
                    continue;
                }

                hasStatusListener = true;
                break;
            }
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
        for (var abilityIndex = 0; abilityIndex < actor.Abilities.Count; abilityIndex++)
        {
            var ability = actor.Abilities[abilityIndex];
            if (ability.Definition.Kind != AbilitySpecKind.Active || !ability.IsReady)
                continue;

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
            GenerateAbilityThreat(actor, ability.Definition);
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
        PerformBasicAttack(actor, combatants);
    }

    private void PerformBasicAttack(RuntimeCombatant actor, IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (SelectAttentionTarget(actor, combatants) is not { } target)
            return;

        if (_threatAndTankingEnabled)
            AdjustThreatAndTrack(actor, _basicAttackThreatValue, "Basic Attack");

        var baseDamage = Math.Max(
            1,
            (int)Math.Round(
                (1 + GetEffectivePower(actor) * AttributeCombatRules.BasicAttackPowerCoefficient) *
                actor.BasicAttackDamageMultiplier));
        var damage = Math.Max(1, ApplyCombatMagnitudeVariance(baseDamage));
        Log(actor, null, "Basic Attack", EventType.AbilityUse, 0, $"{actor.Name} used Basic Attack");
        PublishIfObserved(AbilityTriggerEvent.OnBasicAttack, actor, target, "basic_attack", combatants);
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

    private static bool IsActionBlocked(RuntimeCombatant combatant)
    {
        return combatant.Stagger?.IsStaggered == true
               || IsStunnedOrFrozen(combatant);
    }

    private static bool IsActiveAbilityBlocked(RuntimeCombatant combatant) =>
        combatant.HasCondition(StandardConditionType.Silence)
        || HasStatusTag(combatant, "Control.Silence");

    private static bool IsStunnedOrFrozen(RuntimeCombatant combatant) =>
        combatant.HasCondition(StandardConditionType.Stun)
        || combatant.HasCondition(StandardConditionType.Freeze)
        || HasStatusTag(combatant, "Control.Stun")
        || HasStatusTag(combatant, "Control.Freeze");

    private static bool HasStatusTag(RuntimeCombatant combatant, string tag)
    {
        for (var index = 0; index < combatant.Statuses.Count; index++)
        {
            var status = combatant.Statuses[index];
            if (status.Stacks > 0 && status.Definition.Tags.Contains(tag))
                return true;
        }
        return false;
    }

    private void TrackControlExposure(RuntimeCombatant combatant)
    {
        var staggered = combatant.Stagger?.IsStaggered == true;
        var stunnedOrFrozen = IsStunnedOrFrozen(combatant);
        if (staggered || stunnedOrFrozen)
            _actionDeniedTicks[combatant] = _actionDeniedTicks.GetValueOrDefault(combatant) + 1;
        if (staggered)
            _staggeredTicks[combatant] = _staggeredTicks.GetValueOrDefault(combatant) + 1;
        if (stunnedOrFrozen)
            _stunnedOrFrozenTicks[combatant] = _stunnedOrFrozenTicks.GetValueOrDefault(combatant) + 1;
        if (IsActiveAbilityBlocked(combatant))
            _silencedTicks[combatant] = _silencedTicks.GetValueOrDefault(combatant) + 1;
        if (combatant.HasCondition(StandardConditionType.Slow) || HasStatusTag(combatant, "Control.Slow"))
            _slowedTicks[combatant] = _slowedTicks.GetValueOrDefault(combatant) + 1;
    }

    private void TrackCompactTelemetry(IReadOnlyList<RuntimeCombatant> combatants)
    {
        var activeFriendly = 0;
        var activeHostile = 0;
        var activeFriendlySummons = 0;
        var activeHostileSummons = 0;
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (!combatant.IsAlive)
                continue;
            if (combatant.Team == CombatTeam.Friendly)
            {
                activeFriendly++;
                if (combatant.IsSummoned)
                    activeFriendlySummons++;
            }
            else if (combatant.Team == CombatTeam.Hostile)
            {
                activeHostile++;
                if (combatant.IsSummoned)
                    activeHostileSummons++;
            }
        }

        _peakActiveFriendlyCombatants = Math.Max(_peakActiveFriendlyCombatants, activeFriendly);
        _peakActiveFriendlySummons = Math.Max(_peakActiveFriendlySummons, activeFriendlySummons);
        _peakActiveHostileSummons = Math.Max(_peakActiveHostileSummons, activeHostileSummons);
        if (activeHostile > _peakActiveHostileCombatants)
        {
            _peakActiveHostileCombatants = activeHostile;
            _peakActiveHostileTick = _currentTick;
        }
        if (activeHostile > 1)
            _firstAdditionalHostileTick ??= _currentTick;
        if (activeHostileSummons > 0)
        {
            if (!_hostileSummonWindowActive)
            {
                _hostileSummonWindowActive = true;
                _additionalHostileWindowCount++;
            }
            if (_hostileSummonWaveCount == 0)
                RecordHostileSummonWave();
            if (_lastHostileSummonUptimeTick != _currentTick)
            {
                _hostileSummonActiveTicks++;
                _lastHostileSummonUptimeTick = _currentTick;
            }
        }
        else if (_hostileSummonWindowActive)
        {
            _hostileSummonWindowActive = false;
            _clearedAdditionalHostileWindowCount++;
            _firstAdditionalHostileClearTick ??= _currentTick;
        }
    }

    private void TrackInitialFriendlyHealthDeficit(IReadOnlyList<RuntimeCombatant> friendly)
    {
        if (friendly.Count == 0)
            return;

        double deficitRatioTotal = 0;
        for (var index = 0; index < friendly.Count; index++)
        {
            var combatant = friendly[index];
            var maximumHealth = Math.Max(1, combatant.GetAttribute(AttributeType.MaxHealth));
            var healthRatio = combatant.IsAlive
                ? Math.Clamp(combatant.Health / maximumHealth, 0, 1)
                : 0;
            deficitRatioTotal += 1 - healthRatio;
        }

        _initialFriendlyHealthDeficitRatioTotal += deficitRatioTotal / friendly.Count;
        _initialFriendlyHealthDeficitSampleTicks++;
    }

    private void RecordHostileSummonWave()
    {
        if (_lastHostileSummonWaveTick == _currentTick)
            return;
        if (_lastHostileSummonWaveTick.HasValue)
        {
            var interval = _currentTick - _lastHostileSummonWaveTick.Value;
            _hostileSummonWaveIntervalCount++;
            _hostileSummonWaveIntervalTotalTicks = checked(
                _hostileSummonWaveIntervalTotalTicks + interval);
            _minimumHostileSummonWaveIntervalTicks = !_minimumHostileSummonWaveIntervalTicks.HasValue
                ? interval
                : Math.Min(_minimumHostileSummonWaveIntervalTicks.Value, interval);
            _maximumHostileSummonWaveIntervalTicks = !_maximumHostileSummonWaveIntervalTicks.HasValue
                ? interval
                : Math.Max(_maximumHostileSummonWaveIntervalTicks.Value, interval);
        }
        _lastHostileSummonWaveTick = _currentTick;
        _hostileSummonWaveCount++;
    }

    private CompactCombatTelemetry CreateCompactTelemetry(IReadOnlyList<RuntimeCombatant> combatants) => new(
        _peakActiveFriendlyCombatants,
        _peakActiveHostileCombatants,
        _peakActiveFriendlySummons,
        _peakActiveHostileSummons,
        _peakActiveHostileTick,
        _firstAdditionalHostileTick,
        _firstAdditionalHostileClearTick,
        _additionalHostileWindowCount,
        _clearedAdditionalHostileWindowCount,
        _hostileSummonActiveTicks,
        _hostileSummonWaveCount,
        _hostileSummonWaveIntervalCount,
        _hostileSummonWaveIntervalTotalTicks,
        _minimumHostileSummonWaveIntervalTicks,
        _maximumHostileSummonWaveIntervalTicks,
        combatants.Count(combatant => combatant.Team == CombatTeam.Friendly),
        combatants.Count(combatant => combatant.Team == CombatTeam.Hostile),
        combatants.Count(combatant => combatant.Team == CombatTeam.Friendly && combatant.IsSummoned),
        combatants.Count(combatant => combatant.Team == CombatTeam.Hostile && combatant.IsSummoned),
        combatants.Count(combatant => combatant.Team == CombatTeam.Friendly && combatant.IsAlive),
        combatants.Count(combatant => combatant.Team == CombatTeam.Hostile && combatant.IsAlive),
        combatants.Count(combatant => combatant.Team == CombatTeam.Friendly && combatant.IsAlive && combatant.IsSummoned),
        combatants.Count(combatant => combatant.Team == CombatTeam.Hostile && combatant.IsAlive && combatant.IsSummoned),
        _initialFriendlyHealthDeficitSampleTicks,
        _initialFriendlyHealthDeficitSampleTicks == 0
            ? 0
            : _initialFriendlyHealthDeficitRatioTotal / _initialFriendlyHealthDeficitSampleTicks);

    private static bool HasLivingOpponent(RuntimeCombatant actor, IReadOnlyList<RuntimeCombatant> combatants)
    {
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (combatant.Team != actor.Team && combatant.IsAlive)
                return true;
        }
        return false;
    }

    private static bool HasLivingTeam(IReadOnlyList<RuntimeCombatant> combatants, CombatTeam team)
    {
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (combatant.Team == team && combatant.IsAlive)
                return true;
        }
        return false;
    }

    private bool CanResolveActiveAbility(
        RuntimeAbility ability,
        RuntimeCombatant actor,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!ability.Definition.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers))
            return false;

        var primaryTarget = SelectActiveAbilityPrimaryTarget(ability, actor, combatants);
        var combatEvent = new CombatEvent(
            AbilityTriggerEvent.OnAbilityUsed,
            actor,
            primaryTarget,
            ability.Definition.Id);
        for (var triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
        {
            var trigger = triggers[triggerIndex];
            if (!ConditionsPass(trigger.Conditions, actor, combatEvent, combatants))
                continue;

            for (var effectIndex = 0; effectIndex < trigger.Effects.Count; effectIndex++)
            {
                var effect = trigger.Effects[effectIndex];
                var targetBuffer = ArrayPool<RuntimeCombatant>.Shared.Rent(Math.Max(1, combatants.Count));
                try
                {
                    var targetCount = FillTargets(
                        targetBuffer,
                        actor,
                        effect.Target,
                        combatEvent,
                        combatants,
                        effect.SummonId,
                        effect.TargetCondition,
                        effect.ExcludeEventTarget,
                        effect.IgnoreTaunt,
                        effect.ExcludeSummons,
                        effect.UseHealthPercentage,
                        effect.RandomizeTies);
                    for (var targetIndex = 0; targetIndex < targetCount; targetIndex++)
                    {
                        var target = targetBuffer[targetIndex];
                        if (target.IsAlive
                            && EffectCanResolve(effect, actor, combatants)
                            && ConditionsPass(
                                effect.Conditions,
                                actor,
                                combatEvent,
                                combatants,
                                effectTarget: target))
                        {
                            return true;
                        }
                    }
                }
                finally
                {
                    ArrayPool<RuntimeCombatant>.Shared.Return(targetBuffer, clearArray: true);
                }
            }
        }

        return false;
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
            PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, actor, actor, null, combatants);

        return additionalCooldownTicks;
    }

    private void RegisterListeners(RuntimeCombatant combatant)
    {
        foreach (var eventType in combatant.AbilityTriggersByEvent.Keys)
            RegisterListener(eventType);

        for (var statusIndex = 0; statusIndex < combatant.Statuses.Count; statusIndex++)
            RegisterListeners(combatant.Statuses[statusIndex]);
    }

    private void RegisterListeners(RuntimeStatus status)
    {
        foreach (var eventType in status.Definition.TriggersByEvent.Keys)
            RegisterListener(eventType);
    }

    private void RegisterListener(AbilityTriggerEvent eventType) =>
        _listenerMask |= GetListenerBit(eventType);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasPotentialListener(AbilityTriggerEvent eventType) =>
        (_listenerMask & GetListenerBit(eventType)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GetListenerBit(AbilityTriggerEvent eventType)
    {
        var bit = (int)eventType;
        if ((uint)bit >= 64)
        {
            throw new InvalidOperationException(
                $"Ability trigger event '{eventType}' does not fit in the 64-bit listener index.");
        }

        return 1UL << bit;
    }

    private void PublishIfObserved(
        AbilityTriggerEvent eventType,
        RuntimeCombatant? source,
        RuntimeCombatant? target,
        string? abilityId,
        IReadOnlyList<RuntimeCombatant> combatants,
        int magnitude = 0,
        RuntimeCombatant? instigator = null,
        long? barrierApplicationOrder = null,
        ConditionRemovalReason? removalReason = null,
        DamageType damageType = DamageType.None,
        AttackType attackType = AttackType.None,
        bool wasCritical = false,
        bool wasDirectHit = false)
    {
        if (!HasPotentialListener(eventType))
            return;

        Publish(
            new CombatEvent(
                eventType,
                source,
                target,
                abilityId,
                magnitude,
                instigator,
                barrierApplicationOrder,
                removalReason,
                damageType,
                attackType,
                wasCritical,
                wasDirectHit),
            combatants);
    }

    private void Publish(
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        IReadOnlyList<RuntimeCombatant>? listeners = null)
    {
        if (_eventDepth >= 64)
            throw new InvalidOperationException("Combat event recursion exceeded the maximum depth of 64.");

        _eventDepth++;
        try
        {
            var listeningCombatants = listeners ?? combatants;
            var combatantCount = listeningCombatants.Count;
            for (var combatantIndex = 0; combatantIndex < combatantCount; combatantIndex++)
            {
                var combatant = listeningCombatants[combatantIndex];
                if (!combatant.IsAlive
                    && (combatEvent.Event != AbilityTriggerEvent.OnDeath
                        || !ReferenceEquals(combatant, combatEvent.Source)))
                {
                    continue;
                }

                if (combatant.AbilityTriggersByEvent.TryGetValue(combatEvent.Event, out var abilities))
                {
                    for (var abilityIndex = 0; abilityIndex < abilities.Count; abilityIndex++)
                    {
                        var ability = abilities[abilityIndex];
                        if (ability.Definition.Kind == AbilitySpecKind.Active
                            && combatEvent.Event == AbilityTriggerEvent.OnAbilityUsed
                            && !string.Equals(combatEvent.AbilityId, ability.Definition.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!IsSourceScopedTriggerRelevant(combatant, combatEvent))
                            continue;

                        var triggers = ability.Definition.TriggersByEvent[combatEvent.Event];
                        for (var triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
                        {
                            var trigger = triggers[triggerIndex];
                            if (!ability.CanUseTrigger(trigger, _currentTick)
                                || !ConditionsPass(trigger.Conditions, combatant, combatEvent, combatants))
                                continue;

                            ability.StartTriggerCooldown(trigger);
                            ability.BeginTriggerExecution(trigger);
                            try
                            {
                                var effectUsage = new EffectUsageTracker(ability);
                                if (ability.Definition.Kind == AbilitySpecKind.Passive
                                    && ability.CanGenerateThreat(trigger)
                                    && (trigger.Effects.Count == 0
                                        || trigger.Effects.Any(effect => effectUsage.CanUseEffect(effect, null))))
                                {
                                    GenerateAbilityThreat(combatant, ability.Definition, trigger.ThreatValue);
                                    ability.StartThreatCooldown(trigger);
                                }
                                ExecuteTrigger(
                                    trigger,
                                    combatant,
                                    combatEvent,
                                    combatants,
                                    effectUsage,
                                    countStatsActivation: ability.Definition.Kind == AbilitySpecKind.Passive);
                            }
                            finally
                            {
                                ability.EndTriggerExecution(trigger);
                            }
                        }
                    }
                }

                PublishStatusTriggers(combatEvent, combatants, combatant);
            }
        }
        finally
        {
            _eventDepth--;
        }
    }

    private void GenerateAbilityThreat(
        RuntimeCombatant source,
        CompiledAbility ability,
        int? threatValue = null)
    {
        var resolvedThreatValue = threatValue ?? ability.ThreatValue;
        if (!_threatAndTankingEnabled || resolvedThreatValue == 0 || ability.ThreatMultiplier <= 0)
            return;

        AdjustThreatAndTrack(
            source,
            resolvedThreatValue * ability.ThreatMultiplier,
            ability.Name);
    }

    private void GenerateMaintainedThreat(IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!_threatAndTankingEnabled)
            return;

        _maintainedThreatRates.Clear();
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var target = combatants[combatantIndex];
            for (var modifierIndex = 0; modifierIndex < target.MaintainedModifiers.Count; modifierIndex++)
            {
                var modifier = target.MaintainedModifiers[modifierIndex];
                var effect = modifier.Definition;
                if (!modifier.Source.IsAlive
                    || effect.MaintainedThreatPerSecond <= 0
                    || effect.MaintainedThreatBand is not { } band)
                {
                    continue;
                }

                var key = new MaintainedThreatSourceKey(modifier.Source, modifier.StatsSource, band);
                _maintainedThreatRates[key] = Math.Max(
                    _maintainedThreatRates.GetValueOrDefault(key),
                    effect.MaintainedThreatPerSecond);
            }
        }

        foreach (var (key, threatPerSecond) in _maintainedThreatRates)
        {
            var accumulated = _maintainedThreatRemainders.GetValueOrDefault(key)
                              + threatPerSecond / TicksPerSecond;
            var wholeThreat = (int)Math.Floor(accumulated + 1e-9d);
            _maintainedThreatRemainders[key] = accumulated - wholeThreat;
            if (wholeThreat > 0)
                AdjustThreatAndTrack(key.Source, wholeThreat, key.StatsSource);
        }
    }

    private void PublishStatusTriggers(
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        RuntimeCombatant combatant)
    {
        var statusCount = combatant.Statuses.Count;
        if (statusCount == 0)
            return;

        var statusSnapshot = ArrayPool<RuntimeStatus>.Shared.Rent(statusCount);
        for (var statusIndex = 0; statusIndex < statusCount; statusIndex++)
            statusSnapshot[statusIndex] = combatant.Statuses[statusIndex];

        try
        {
            for (var statusIndex = 0; statusIndex < statusCount; statusIndex++)
            {
                var status = statusSnapshot[statusIndex];
                if (!status.Definition.TriggersByEvent.TryGetValue(combatEvent.Event, out var triggers))
                    continue;

                if (!IsSourceScopedTriggerRelevant(status.Owner, combatEvent))
                    continue;

                for (var triggerIndex = 0; triggerIndex < triggers.Count; triggerIndex++)
                {
                    var trigger = triggers[triggerIndex];
                    if (IsStatusLifecycleEvent(combatEvent.Event)
                        && (!string.Equals(combatEvent.AbilityId, status.Definition.Id, StringComparison.OrdinalIgnoreCase)
                            || !ReferenceEquals(combatEvent.Target, status.Owner)))
                    {
                        continue;
                    }

                    if (!status.CanUseTrigger(trigger, _currentTick)
                        || !ConditionsPass(trigger.Conditions, status.Source, combatEvent, combatants))
                    {
                        continue;
                    }

                    status.StartTriggerCooldown(trigger);
                    status.BeginTriggerExecution(trigger);
                    try
                    {
                        ExecuteTrigger(
                            trigger,
                            status.Source,
                            combatEvent,
                            combatants,
                            new EffectUsageTracker(status),
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
        finally
        {
            ArrayPool<RuntimeStatus>.Shared.Return(statusSnapshot, clearArray: true);
        }
    }

    private void ExecuteTrigger(
        CompiledTrigger trigger,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        EffectUsageTracker effectUsage,
        string? statsSourceOverride = null,
        bool countStatsActivation = false,
        double durationMultiplier = 1d)
    {
        var activationCounted = false;
        var executionContext = CreateEffectExecutionContext();
        for (var effectIndex = 0; effectIndex < trigger.Effects.Count; effectIndex++)
        {
            var effect = trigger.Effects[effectIndex];
            if (!effectUsage.CanUseEffect(effect, null))
                continue;

            var repeatCount = effect.RepeatCount;
            if (!string.IsNullOrWhiteSpace(effect.RepeatPerOwnedSummonId))
            {
                repeatCount *= CountLivingOwnedSummons(
                    source,
                    effect.RepeatPerOwnedSummonId,
                    combatants);
            }

            if (repeatCount <= 0)
                continue;

            // Effects such as Summon can append combatants while resolving this target set.
            // Snapshot it so every repetition keeps the cast's originally selected targets.
            var targets = ArrayPool<RuntimeCombatant>.Shared.Rent(Math.Max(1, combatants.Count));
            try
            {
                var targetCount = FillTargets(
                    targets,
                    source,
                    effect.Target,
                    combatEvent,
                    combatants,
                    effect.SummonId,
                    effect.TargetCondition,
                    effect.ExcludeEventTarget,
                    effect.IgnoreTaunt,
                    effect.ExcludeSummons,
                    effect.UseHealthPercentage,
                    effect.RandomizeTies);
                for (var repetition = 0; repetition < repeatCount; repetition++)
                {
                    for (var targetIndex = 0; targetIndex < targetCount; targetIndex++)
                    {
                        var target = targets[targetIndex];
                        if (!effectUsage.CanUseEffect(effect, target))
                            continue;

                        if (!target.IsAlive || !CanAbilityAffectTarget(source, target))
                            continue;

                        var effectConditionsPass = ConditionsPass(
                            effect.Conditions,
                            source,
                            combatEvent,
                            combatants,
                            effectTarget: target);
                        if (effect.MaintainWhileConditionsMet)
                        {
                            var countMaintainedActivation = countStatsActivation && !activationCounted;
                            if (SynchronizeMaintainedModifier(
                                    effect,
                                    source,
                                    target,
                                    combatants,
                                    combatEvent,
                                    statsSourceOverride,
                                    effectConditionsPass,
                                    countMaintainedActivation,
                                    targetIndex)
                                && countMaintainedActivation)
                            {
                                activationCounted = true;
                            }

                            continue;
                        }

                        if (!effectConditionsPass)
                            continue;

                        if (effect.Operation != AbilityEffectOperation.ApplyRandomCondition
                            && !IsPeriodicEffect(effect)
                            && effect.ChancePercent < 100
                            && _random.Next(1, 101) > effect.ChancePercent)
                            continue;

                        var countThisActivation = countStatsActivation && !activationCounted;
                        effectUsage.MarkEffectUsed(effect, target);
                        ExecuteEffect(
                            effect,
                            source,
                            target,
                            combatants,
                            combatEvent,
                            statsSourceOverride,
                            countThisActivation,
                            durationMultiplier,
                            executionContext,
                            targetIndex);
                        if (countThisActivation)
                            activationCounted = true;
                    }
                }
            }
            finally
            {
                ArrayPool<RuntimeCombatant>.Shared.Return(targets, clearArray: true);
            }
        }
    }

    private readonly struct EffectUsageTracker
    {
        private readonly RuntimeAbility? _ability;
        private readonly RuntimeStatus? _status;

        public EffectUsageTracker(RuntimeAbility ability)
        {
            _ability = ability;
            _status = null;
        }

        public EffectUsageTracker(RuntimeStatus status)
        {
            _ability = null;
            _status = status;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanUseEffect(CompiledEffect effect, RuntimeCombatant? target) =>
            _ability is not null
                ? _ability.CanUseEffect(effect, target)
                : _status!.CanUseEffect(effect, target);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkEffectUsed(CompiledEffect effect, RuntimeCombatant target)
        {
            if (_ability is not null)
                _ability.MarkEffectUsed(effect, target);
            else
                _status!.MarkEffectUsed(effect, target);
        }
    }

    private EffectExecutionContext CreateEffectExecutionContext() =>
        new(++_activationSequence);

    private bool SynchronizeMaintainedModifier(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatEvent combatEvent,
        string? statsSourceOverride,
        bool shouldBeActive,
        bool countStatsActivation,
        int targetIndex)
    {
        var existing = target.MaintainedModifiers.FirstOrDefault(modifier =>
            ReferenceEquals(modifier.Definition, effect)
            && ReferenceEquals(modifier.Source, source));
        if (shouldBeActive)
        {
            if (existing is not null)
                return false;

            var statsSource = statsSourceOverride ?? effect.StatsSource;
            var appliedValue = CalculateValue(
                effect,
                source,
                target,
                combatants,
                combatEvent,
                targetIndex);
            ApplyEffectOnce(
                effect,
                source,
                target,
                combatants,
                combatEvent,
                statsSource,
                countStatsActivation,
                precomputedValue: appliedValue,
                targetIndex: targetIndex);
            target.MaintainedModifiers.Add(new RuntimeMaintainedModifier(
                effect,
                source,
                target,
                statsSource,
                appliedValue));
            return true;
        }

        if (existing is null)
            return false;

        RemoveModifierValue(existing.Definition, existing.Target, existing.AppliedModifierValue);
        target.MaintainedModifiers.Remove(existing);
        Log(
            existing.Source,
            existing.Target,
            existing.Definition.Id,
            EventType.BuffExpired,
            -existing.AppliedModifierValue,
            $"{existing.Target.Name}'s conditional modifier returned to normal.",
            existing.StatsSource);
        return false;
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
        EffectExecutionContext? executionContext = null,
        int targetIndex = 0)
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
                PublishIfObserved(AbilityTriggerEvent.OnHeal, source, target, effect.Id, combatants);
            }

            return;
        }

        if (effect.RefreshDuration
            && effect.DurationTicks > 0
            && IsTimedModifierOperation(effect.Operation))
        {
            RemoveExistingTimedModifier(effect, source, target);
        }

        var appliedModifierValue = effect.DurationTicks > 0 && IsTimedModifierOperation(effect.Operation)
            ? (int?)CalculateValue(effect, source, target, combatants, combatEvent, targetIndex)
            : null;

        ApplyEffectOnce(
            effect,
            source,
            target,
            combatants,
            combatEvent,
            statsSource,
            countStatsActivation,
            executionContext,
            appliedModifierValue,
            targetIndex);

        if (effect.DurationTicks > 0 && IsTimedModifierOperation(effect.Operation))
        {
            target.ActiveEffects.Add(new RuntimeEffect(
                effect,
                source,
                target,
                statsSource,
                durationMultiplier,
                appliedModifierValue: appliedModifierValue));
        }
    }

    private void RemoveExistingTimedModifier(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target)
    {
        for (var index = target.ActiveEffects.Count - 1; index >= 0; index--)
        {
            var activeEffect = target.ActiveEffects[index];
            if (!ReferenceEquals(activeEffect.Definition, effect)
                || !ReferenceEquals(activeEffect.Source, source))
            {
                continue;
            }

            if (activeEffect.AppliedModifierValue is { } appliedModifierValue)
                RemoveModifierValue(effect, target, appliedModifierValue);

            target.ActiveEffects.RemoveAt(index);
        }
    }

    private void ApplyEffectOnce(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatEvent? combatEvent = null,
        string? statsSourceOverride = null,
        bool countStatsActivation = false,
        EffectExecutionContext? executionContext = null,
        int? precomputedValue = null,
        int targetIndex = 0)
    {
        var value = precomputedValue ?? CalculateValue(effect, source, target, combatants, combatEvent, targetIndex);
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
                var damageType = effect.InheritEventDamageType
                    ? combatEvent?.DamageType ?? effect.DamageType
                    : effect.DamageType;
                var healthDamage = ApplyDamage(
                    source,
                    target,
                    value,
                    effect.AttackType,
                    damageType,
                    effect,
                    combatants,
                    effect.Id,
                    statsSource,
                    countStatsActivation,
                    delivery);
                if (delivery == DamageDelivery.Direct)
                    ApplyLifeSteal(effect, source, target, healthDamage, combatants, statsSource);
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
            case AbilityEffectOperation.GrantCover:
                GrantCover(
                    source,
                    target,
                    value,
                    effect.DurationTicks,
                    statsSource ?? effect.StatsSource,
                    effect.Id,
                    countStatsActivation);
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
            case AbilityEffectOperation.ResetAbilityCooldown:
                var resetCooldownTicks = target.ResetAbilityCooldown(effect.AbilityId!);
                Log(
                    source,
                    target,
                    effect.Id,
                    EventType.Buff,
                    resetCooldownTicks,
                    $"{source.Name} reset {effect.AbilityId}'s cooldown to {resetCooldownTicks} ticks on {target.Name}.",
                    statsSource,
                    countStatsActivation);
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
                    countStatsActivation,
                    effect.GuaranteedConditionApplication,
                    effect.StaggerPower);
                break;
            case AbilityEffectOperation.ModifyStatusStacks:
                ModifyStatusStacks(source, target, effect.StatusId!, value, combatants);
                break;
            case AbilityEffectOperation.ScaleStatusStacks:
                var currentStacks = target.GetStatusStacks(effect.StatusId!);
                var retainedStacks = (int)Math.Floor(currentStacks * value / 100d);
                ModifyStatusStacks(
                    source,
                    target,
                    effect.StatusId!,
                    retainedStacks - currentStacks,
                    combatants);
                break;
            case AbilityEffectOperation.RemoveStatus:
                if (RemoveStatus(source, target, effect.StatusId!, combatants))
                {
                    Log(source, target, effect.Id, EventType.StatusEffectRemoved, 0, $"{target.Name} lost {effect.StatusId}.", statsSource, countStatsActivation);
                }
                break;
            case AbilityEffectOperation.ToggleStatus:
                ToggleStatus(
                    source,
                    target,
                    effect.StatusId!,
                    effect.AlternativeStatusId!,
                    combatants,
                    statsSource,
                    countStatsActivation);
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
                    countStatsActivation,
                    effect.GuaranteedConditionApplication,
                    effect.StaggerPower);
                break;
            case AbilityEffectOperation.Cleanse:
                var cleansed = CleanseStatuses(source, target, combatants)
                               + CleanseConditions(source, target, effect.Condition, combatants);
                Log(source, target, effect.Id, EventType.StatusEffectCleansed, cleansed, $"{target.Name} was cleansed.", statsSource, countStatsActivation);
                break;
            case AbilityEffectOperation.Dispel:
                var dispelled = DispelConditions(source, target, effect.Condition, effect.BaseValue, combatants);
                Log(source, target, effect.Id, EventType.StatusEffectDispelled, dispelled, $"{target.Name} was dispelled.", statsSource, countStatsActivation);
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
                    executionContext ?? CreateEffectExecutionContext());
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
                AdjustThreatAndTrack(target, value, statsSource ?? effect.Id);
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
            case AbilityEffectOperation.ModifyDamageDealtToLowHealth:
                target.AdjustDamageDealtToLowHealth(effect.HealthStepPercent, value);
                Log(source, target, effect.Id, value >= 0 ? EventType.Buff : EventType.Debuff, value, $"{target.Name}'s damage against wounded targets changed by {value}%.", statsSource, countStatsActivation);
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
                    executionContext ?? CreateEffectExecutionContext());
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
            case AbilityEffectOperation.SynchronizeAttributePerLivingNonSummonedAlly:
                SynchronizeAttributePerLivingNonSummonedAlly(
                    effect,
                    source,
                    target,
                    combatants,
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.ModifyCriticalDamageAgainstCondition:
                target.AdjustCriticalDamageAgainstCondition(effect.Condition!.Value, value);
                Log(
                    source,
                    target,
                    effect.Id,
                    value >= 0 ? EventType.Buff : EventType.Debuff,
                    value,
                    $"{target.Name}'s Critical Damage against {effect.Condition} targets changed by {value}%.",
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.PerformBasicAttack:
                if (target.CanBasicAttack && !IsActionBlocked(target))
                    PerformBasicAttack(target, combatants);
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
            case AbilityEffectOperation.SynchronizeAttributePerMissingHealthStep:
                SynchronizeAttributePerMissingHealthStep(
                    effect,
                    source,
                    target,
                    statsSource,
                    countStatsActivation);
                break;
            case AbilityEffectOperation.SwapHealth:
                SwapHealth(
                    source,
                    target,
                    combatants,
                    effect.Id,
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
        float armorPenetrationBonus = 0,
        bool skipSourceDamageModifier = false,
        RuntimeCombatant? redirectedFrom = null)
    {
        if (!target.IsAlive || damage <= 0)
            return 0;

        var redirectedIncomingDamage = delivery == DamageDelivery.Redirected ? damage : 0;

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
                    avoidedDamage: damage,
                    countsAsTargetedAttack: true);
                PublishIfObserved(AbilityTriggerEvent.OnDodge, target, source, null, combatants);
                return 0;
            }
        }

        var damageModifier = (skipSourceDamageModifier
                                 ? 0
                                 : source.GetDamageDealtPercent(damageType)
                                   + source.GetDamageDealtToLowHealthPercent(target))
                             + target.GetDamageTakenPercent(damageType, source);
        damage = Math.Max(0, (int)Math.Round(damage * Math.Max(0, 1 + damageModifier / 100f)));
        var isCritical = delivery == DamageDelivery.Direct
                         && CanCrit(effect, AbilityEffectOperation.Damage)
                         && RollCriticalStrike(source, effect?.CritChanceBonus ?? 0);
        var criticalDamage = isCritical
            ? ApplyCriticalMultiplier(source, damage, target)
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
        var damageRedirectedAway = 0;
        if (_threatAndTankingEnabled
            && delivery != DamageDelivery.Redirected
            && guardedDamage > 0
            && TryGetActiveCover(target) is { } cover)
        {
            damageRedirectedAway = Math.Max(
                0,
                (int)Math.Round(
                    cover.ConsumeBudget(guardedDamage * cover.Percent / 100f),
                    MidpointRounding.AwayFromZero));
            guardedDamage = Math.Max(0, guardedDamage - damageRedirectedAway);
            if (damageRedirectedAway > 0)
            {
                ApplyDamage(
                    source,
                    cover.Guardian,
                    damageRedirectedAway,
                    attackType,
                    damageType,
                    effect,
                    combatants,
                    sourceName,
                    statsSource,
                    delivery: DamageDelivery.Redirected,
                    armorPenetrationBonus: armorPenetrationBonus,
                    skipSourceDamageModifier: true,
                    redirectedFrom: target);
            }
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
        TrackBalanceDamage(source, target, healthDamage);

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
            $"{source.Name} dealt {healthDamage} {damageType} damage to {target.Name}{(isCritical ? " (critical)" : string.Empty)}{(blocked ? " (blocked)" : string.Empty)}{(redirectedFrom is null ? string.Empty : $" redirected from {redirectedFrom.Name}")}.",
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
            pendingHealthDamage,
            damageType: damageType,
            damageRedirectedTo: redirectedIncomingDamage,
            damageRedirectedAway: damageRedirectedAway,
            countsAsTargetedAttack: delivery == DamageDelivery.Direct);
        var abilityId = sourceName.Equals("Basic Attack", StringComparison.Ordinal)
            ? "basic_attack"
            : effect?.Id;
        if (healthDamage > 0)
        {
            PublishIfObserved(
                AbilityTriggerEvent.OnDamageDealt,
                source,
                target,
                abilityId,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: delivery == DamageDelivery.Direct);
        }
        if (delivery == DamageDelivery.Direct)
        {
            PublishIfObserved(
                AbilityTriggerEvent.OnHit,
                source,
                target,
                abilityId,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: true);
            PublishAttackTypeEvents(
                source,
                target,
                abilityId,
                healthDamage,
                damageType,
                attackType,
                isCritical,
                combatants);
            PublishIfObserved(
                AbilityTriggerEvent.OnDamaged,
                target,
                source,
                abilityId,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: true);
            PublishIfObserved(
                AbilityTriggerEvent.OnAttacked,
                target,
                source,
                abilityId,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: true);
        }
        if (healthDamage > 0)
            PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, target, source, null, combatants);

        if (delivery == DamageDelivery.Direct
            && guardedDamage > 0
            && !ReferenceEquals(source, target)
            && source.IsAlive)
        {
            ResolveThorns(target, source, guardedDamage, combatants);
        }

        if (!target.IsAlive)
        {
            if (_downedOptions is not null
                && target.Team == CombatTeam.Friendly
                && !target.IsSummoned)
            {
                var deaths = _deathCounts.GetValueOrDefault(target) + 1;
                _deathCounts[target] = deaths;
                var delay = Math.Min(
                    _downedOptions.MaximumDelayTicks,
                    checked(_downedOptions.BaseDelayTicks
                            + (deaths - 1) * _downedOptions.AdditionalDelayTicksPerDeath));
                _reviveAtTicks[target] = checked(_currentTick + Math.Max(1, delay));
            }
            Log(source, target, sourceName, EventType.Death, 0, $"{target.Name} was killed by {source.Name}.", statsSource);
            PublishIfObserved(
                AbilityTriggerEvent.OnKill,
                source,
                target,
                null,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: delivery == DamageDelivery.Direct);
            PublishIfObserved(
                AbilityTriggerEvent.OnDeath,
                target,
                source,
                null,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: delivery == DamageDelivery.Direct);
            PublishIfObserved(
                AbilityTriggerEvent.OnEnemyDeath,
                target,
                source,
                null,
                combatants,
                healthDamage,
                damageType: damageType,
                attackType: attackType,
                wasCritical: isCritical,
                wasDirectHit: delivery == DamageDelivery.Direct);
            NotifySummonChanged(target, combatants);
            ExpireOwnedSummons(target, combatants, "owner death");
        }

        return healthDamage;
    }

    private static RuntimeCover? TryGetActiveCover(RuntimeCombatant target)
    {
        RuntimeCover? selected = null;
        for (var index = target.Covers.Count - 1; index >= 0; index--)
        {
            var cover = target.Covers[index];
            if (!cover.IsActive || ReferenceEquals(cover.Guardian, target))
            {
                target.Covers.RemoveAt(index);
                continue;
            }

            if (selected is null
                || cover.Percent > selected.Percent
                || cover.Percent == selected.Percent
                && cover.ApplicationOrder > selected.ApplicationOrder)
            {
                selected = cover;
            }
        }

        return selected;
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
        int receivedDamage,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        _thornsBuffer.Clear();
        for (var index = 0; index < defender.Conditions.Count; index++)
        {
            var condition = defender.Conditions[index];
            if (condition.Type == StandardConditionType.Thorns && condition.Value > 0)
                _thornsBuffer.Add(condition);
        }

        _thornsBuffer.Sort(static (left, right) =>
            left.ApplicationOrder.CompareTo(right.ApplicationOrder));

        for (var index = 0; index < _thornsBuffer.Count; index++)
        {
            var condition = _thornsBuffer[index];
            var reflectedDamage = Math.Max(
                0,
                (int)Math.Min(
                    int.MaxValue,
                    Math.Round(
                        receivedDamage * condition.Value / 100d,
                        MidpointRounding.AwayFromZero)));
            if (reflectedDamage <= 0)
                continue;

            ApplyDamage(
                condition.Source,
                attacker,
                reflectedDamage,
                AttackType.None,
                DamageType.None,
                null,
                combatants,
                GetConditionId(StandardConditionType.Thorns),
                condition.StatsSource,
                delivery: DamageDelivery.Reflected);
        }
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

    private void GrantCover(
        RuntimeCombatant guardian,
        RuntimeCombatant target,
        int percent,
        int durationTicks,
        string statsSource,
        string effectId,
        bool countStatsActivation)
    {
        if (!_threatAndTankingEnabled
            || ReferenceEquals(guardian, target)
            || guardian.Team != target.Team
            || percent <= 0)
        {
            return;
        }

        var normalizedPercent = Math.Clamp(percent, 1, 100);
        var strongestExisting = target.Covers
            .Where(cover => cover.IsActive)
            .OrderByDescending(cover => cover.Percent)
            .ThenByDescending(cover => cover.ApplicationOrder)
            .FirstOrDefault();
        if (strongestExisting is not null && strongestExisting.Percent > normalizedPercent)
            return;

        target.Covers.Clear();
        var budget = guardian.GetAttribute(AttributeType.MaxHealth) * _coverBudgetMaxHealthFraction;
        if (budget <= 0)
            return;

        target.Covers.Add(new RuntimeCover(
            guardian,
            normalizedPercent,
            budget,
            durationTicks,
            ++_applicationOrder,
            statsSource));
        Log(
            guardian,
            target,
            effectId,
            EventType.Buff,
            normalizedPercent,
            $"{guardian.Name} covered {target.Name} for {normalizedPercent}% of incoming damage.",
            statsSource,
            countStatsActivation);
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

    private static int ApplyCriticalMultiplier(
        RuntimeCombatant source,
        int value,
        RuntimeCombatant? hostileTarget = null)
    {
        var criticalDamage = source.GetAttribute(AttributeType.CritDamage)
                             + (hostileTarget is null
                                 ? 0
                                 : source.GetCriticalDamageAgainstConditionPercent(hostileTarget));
        var multiplier = 1 + Math.Max(0, criticalDamage) / 100f;
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
        string? abilityId,
        int magnitude,
        DamageType damageType,
        AttackType attackType,
        bool wasCritical,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        switch (attackType)
        {
            case AttackType.Melee:
                PublishIfObserved(
                    AbilityTriggerEvent.OnMeleeAttack,
                    source,
                    target,
                    abilityId,
                    combatants,
                    magnitude,
                    damageType: damageType,
                    attackType: attackType,
                    wasCritical: wasCritical,
                    wasDirectHit: true);
                PublishIfObserved(
                    AbilityTriggerEvent.OnMeleeAttacked,
                    target,
                    source,
                    abilityId,
                    combatants,
                    magnitude,
                    damageType: damageType,
                    attackType: attackType,
                    wasCritical: wasCritical,
                    wasDirectHit: true);
                break;
            case AttackType.Ranged:
                PublishIfObserved(
                    AbilityTriggerEvent.OnRangedAttack,
                    source,
                    target,
                    abilityId,
                    combatants,
                    magnitude,
                    damageType: damageType,
                    attackType: attackType,
                    wasCritical: wasCritical,
                    wasDirectHit: true);
                PublishIfObserved(
                    AbilityTriggerEvent.OnRangedAttacked,
                    target,
                    source,
                    abilityId,
                    combatants,
                    magnitude,
                    damageType: damageType,
                    attackType: attackType,
                    wasCritical: wasCritical,
                    wasDirectHit: true);
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
            PublishIfObserved(AbilityTriggerEvent.OnHeal, source, target, null, combatants, restored);
        PublishIfObserved(AbilityTriggerEvent.OnHealed, target, source, null, combatants, restored);
        PublishIfObserved(AbilityTriggerEvent.OnEnemyHealed, source, target, null, combatants, restored);
        PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, target, source, null, combatants, restored);

        if (isLifeSteal)
            PublishIfObserved(AbilityTriggerEvent.OnLifestealHeal, source, target, null, combatants, restored);
    }

    private void ApplyLifeSteal(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        int healthDamage,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource)
    {
        if (effect.LifeStealTargetCondition is { } condition && !target.HasCondition(condition))
            return;

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
        var lifeStealPercentage = Math.Clamp(
            source.GetAttribute(AttributeType.LifeSteal) + effectPercentage,
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
            var status = new RuntimeStatus(
                statusDefinition,
                source,
                target,
                stacks,
                statsSource,
                CalculateStatusDuration(statusDefinition, target));
            RegisterListeners(status);
            target.Statuses.Add(status);
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
        bool countStatsActivation,
        bool guaranteedApplication,
        int staggerPower)
    {
        if (!guaranteedApplication
            && IsControlCondition(type)
            && target.HasCondition(StandardConditionType.Unstoppable))
            return;

        if (!guaranteedApplication
            && type is StandardConditionType.Freeze or StandardConditionType.Stun
            && _random.Next(1, 101) > 80)
        {
            return;
        }

        if (!guaranteedApplication
            && IsHarmfulCondition(type)
            && TryConsumeConditionCharge(target, StandardConditionType.Ward, source, combatants))
        {
            return;
        }

        if (type is StandardConditionType.Freeze or StandardConditionType.Stun
            && target.Stagger is not null)
        {
            ApplyStagger(
                source,
                target,
                staggerPower,
                statsSource,
                countStatsActivation,
                combatants);
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
            case StandardConditionType.Silence:
            case StandardConditionType.Taunt:
            case StandardConditionType.Mark:
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
            case StandardConditionType.Soaked:
                ApplyOrStackSharedCondition(source, target, type, normalizedValue, 10, 0, statsSource);
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

    private void ApplyStagger(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int staggerPower,
        string? statsSource,
        bool countStatsActivation,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var stagger = target.Stagger;
        if (stagger is null || staggerPower <= 0)
            return;

        var creditedSource = source.IsSummoned && source.SummonOwner is not null
            ? source.SummonOwner
            : source;
        var applied = stagger.Apply(staggerPower, out var broke);
        if (applied <= 0)
            return;

        Log(
            creditedSource,
            target,
            statsSource ?? "Stagger",
            EventType.StaggerApplied,
            applied,
            $"{source.Name} applied {applied} Stagger to {target.Name}.",
            statsSource,
            countStatsActivation);
        if (!broke)
            return;

        Log(
            creditedSource,
            target,
            statsSource ?? "Stagger",
            EventType.StaggerBroken,
            1,
            $"{target.Name} was Staggered.",
            statsSource,
            false);
        Publish(
            new CombatEvent(
                AbilityTriggerEvent.OnStaggerBroken,
                creditedSource,
                target,
                statsSource ?? "Stagger",
                1),
            combatants);
        _forceCheckpoint = true;
    }

    private void TickStaggerStates(IReadOnlyList<RuntimeCombatant> combatants)
    {
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (combatant.Stagger?.Tick() != RuntimeStaggerTransition.Recovered)
                continue;

            Log(
                combatant,
                combatant,
                "Stagger",
                EventType.StaggerRecovered,
                0,
                $"{combatant.Name} recovered from Stagger.");
            _forceCheckpoint = true;
        }
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
            or StandardConditionType.Doom
            or StandardConditionType.Mark
            or StandardConditionType.Silence
            or StandardConditionType.Soaked;

    private static bool IsBeneficialCondition(StandardConditionType type) =>
        !IsHarmfulCondition(type);

    private static string GetConditionId(StandardConditionType type) =>
        type switch
        {
            StandardConditionType.Haste => "condition.haste",
            StandardConditionType.Slow => "condition.slow",
            StandardConditionType.Empower => "condition.empower",
            StandardConditionType.Weaken => "condition.weaken",
            StandardConditionType.Vulnerable => "condition.vulnerability",
            StandardConditionType.Wound => "condition.wound",
            StandardConditionType.Recovery => "condition.recovery",
            StandardConditionType.Decay => "condition.decay",
            StandardConditionType.Renewal => "condition.renewal",
            StandardConditionType.Guard => "condition.guard",
            StandardConditionType.Ward => "condition.ward",
            StandardConditionType.Unstoppable => "condition.unstoppable",
            StandardConditionType.Poison => "condition.poison",
            StandardConditionType.Burn => "condition.burn",
            StandardConditionType.Bleed => "condition.bleed",
            StandardConditionType.Stun => "condition.stun",
            StandardConditionType.Taunt => "condition.taunt",
            StandardConditionType.Mark => "condition.mark",
            StandardConditionType.Cover => "condition.cover",
            StandardConditionType.Stealth => "condition.stealth",
            StandardConditionType.Chill => "condition.chill",
            StandardConditionType.Freeze => "condition.freeze",
            StandardConditionType.Corrosion => "condition.corrosion",
            StandardConditionType.Doom => "condition.doom",
            StandardConditionType.Thorns => "condition.thorns",
            StandardConditionType.Silence => "condition.silence",
            StandardConditionType.Soaked => "condition.soaked",
            _ => $"condition.{type.ToString().ToLowerInvariant()}"
        };

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

        return AttributeCombatRules.CalculateStatusDurationTicks(
            statusDefinition.DurationTicks,
            resistance);
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
        {
            HealFromSummonOverflow(
                source,
                effect,
                combatants,
                statsSource,
                countStatsActivation);
            return;
        }

        var groupInstanceId = string.IsNullOrWhiteSpace(effect.SummonGroupId)
            ? null
            : executionContext.GetSummonGroupInstanceId(effect.SummonGroupId);
        var summon = CreateSummonedCombatant(
            source,
            effect,
            summonDefinition,
            _abilitiesById,
            groupInstanceId);
        RegisterListeners(summon);
        mutableCombatants.Add(summon);
        if (_captureCompactTelemetry && summon.Team == CombatTeam.Hostile)
            RecordHostileSummonWave();
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

    private void HealFromSummonOverflow(
        RuntimeCombatant source,
        CompiledEffect effect,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        if (effect.HealingScalingAttribute is not { } attribute
            || effect.HealingScalingCoefficient <= 0)
        {
            return;
        }

        var healing = Math.Max(
            0,
            (int)Math.Round(GetEffectiveAttribute(source, attribute) * effect.HealingScalingCoefficient));
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
            && (effect.CountAllOwnedSummons || combatant.Tags.Contains(summonTag)));
        if (effect.CountAllOwnedSummons)
            livingSummons = Math.Min(livingSummons, effect.RepeatCount);

        var amountPerSummon = Math.Abs(effect.ScalingCoefficient) > float.Epsilon
            ? target.GetInitialAttribute(effect.Attribute!.Value) * effect.ScalingCoefficient
            : effect.BaseValue;
        var desiredAmount = livingSummons * amountPerSummon;
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

    private void SynchronizeAttributePerLivingNonSummonedAlly(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        var livingAllies = combatants.Count(combatant =>
            combatant.IsAlive
            && !combatant.IsSummoned
            && !ReferenceEquals(combatant, source)
            && AreAbilityAllies(source, combatant));
        livingAllies = Math.Min(livingAllies, effect.MaximumCount);

        var amountPerAlly = Math.Abs(effect.ScalingCoefficient) > float.Epsilon
            ? target.GetInitialAttribute(effect.Attribute!.Value) * effect.ScalingCoefficient
            : effect.BaseValue;
        var desiredAmount = livingAllies * amountPerAlly;
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
            $"{target.Name}'s {effect.Attribute} changed by {roundedDelta} from {livingAllies} living ally/allies.",
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

    private void SynchronizeAttributePerMissingHealthStep(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        string? statsSource,
        bool countStatsActivation)
    {
        var maxHealth = Math.Max(1d, target.GetAttribute(AttributeType.MaxHealth));
        var missingHealthPercent = Math.Clamp((maxHealth - target.Health) / maxHealth * 100d, 0d, 100d);
        var steps = (int)Math.Floor(missingHealthPercent / effect.HealthStepPercent + 1e-9d);
        var desiredAmount = steps * effect.BaseValue;
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
            $"{target.Name}'s {effect.Attribute} changed by {roundedDelta} from {steps} missing-Health step(s).",
            statsSource,
            countStatsActivation);
    }

    private void SwapHealth(
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        string effectId,
        string? statsSource,
        bool countStatsActivation)
    {
        var sourceHealth = source.Health;
        var targetHealth = target.Health;
        source.SetHealth(targetHealth);
        target.SetHealth(sourceHealth);
        var sourceChange = (int)Math.Round(source.Health - sourceHealth);
        Log(
            source,
            target,
            effectId,
            EventType.Buff,
            sourceChange,
            $"{source.Name} swapped Health with {target.Name}.",
            statsSource,
            countStatsActivation);
        PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, source, target, effectId, combatants);
        PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, target, source, effectId, combatants);
    }

    private int GetBasicAttackChargeThreshold() => Math.Max(1, _basicAttackIntervalTicks);

    private float GetBasicAttackRate(RuntimeCombatant actor)
    {
        var furyAttackSpeed = actor.Team == CombatTeam.Hostile
                              && !actor.IsSummoned
                              && _hostileFuryOptions is { IntervalTicks: > 0 }
            ? GetFuryStacks() * _hostileFuryOptions.AttackSpeedPercentPerStack
            : 0;
        var baseRate =
            (1d + (actor.GetAttribute(AttributeType.AttackSpeed) + furyAttackSpeed) / 100d)
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
                     x.Definition.Kind == AbilitySpecKind.Active))
            ability.StartInitialCooldown(combatant.GetAttribute(AttributeType.Cooldown));
    }

    private RuntimeCombatant CreateSummonedCombatant(
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
            id: string.Create(
                CultureInfo.InvariantCulture,
                $"{source.Id}:summon:{summonId}:{++_summonSequence}"),
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
            summonGroupInstanceId: summonGroupInstanceId,
            threatMultiplier: _threatAndTankingEnabled ? summonDefinition.ThreatMultiplier : 1f,
            partyNumber: source.PartyNumber);
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
                        ? GetEffectiveAttributeWithoutOvertime(source, scalingAttribute) * attribute.ScalingCoefficient
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

    private void ToggleStatus(
        RuntimeCombatant source,
        RuntimeCombatant target,
        string statusId,
        string alternativeStatusId,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? statsSource,
        bool countStatsActivation)
    {
        var nextStatusId = target.GetStatusStacks(statusId) > 0
            ? alternativeStatusId
            : statusId;
        var previousStatusId = nextStatusId.Equals(statusId, StringComparison.OrdinalIgnoreCase)
            ? alternativeStatusId
            : statusId;

        RemoveStatus(source, target, previousStatusId, combatants);
        ApplyStatus(
            source,
            target,
            nextStatusId,
            1,
            combatants,
            statsSource,
            countStatsActivation);
    }

    private int CleanseStatuses(
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var before = target.Statuses.Count;
        foreach (var status in target.Statuses.ToList())
            RemoveStatusInstance(source, target, status, ConditionRemovalReason.Cleansed, combatants);
        return before - target.Statuses.Count;
    }

    private int CleanseConditions(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType? selectedType,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var before = target.Conditions.Count;
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
        return before - target.Conditions.Count;
    }

    private int DispelConditions(
        RuntimeCombatant source,
        RuntimeCombatant target,
        StandardConditionType? selectedType,
        int maximumRemovals,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var before = target.Conditions.Count;
        if (maximumRemovals > 0)
        {
            foreach (var condition in target.Conditions
                         .Where(condition => IsBeneficialCondition(condition.Type)
                                             && condition.Type is not (StandardConditionType.Guard
                                                 or StandardConditionType.Ward
                                                 or StandardConditionType.Taunt)
                                             && (selectedType is null || condition.Type == selectedType))
                         .OrderBy(condition => condition.ApplicationOrder)
                         .Take(maximumRemovals)
                         .ToList())
            {
                RemoveCondition(
                    source,
                    target,
                    condition,
                    ConditionRemovalReason.Dispelled,
                    combatants);
            }

            return before - target.Conditions.Count;
        }

        var types = selectedType is { } selected
            ? [selected]
            : target.Conditions
                .Where(condition => IsBeneficialCondition(condition.Type))
                .Select(condition => condition.Type)
                .Distinct()
                .ToArray();

        foreach (var type in types.Where(IsBeneficialCondition))
        {
            if (type is StandardConditionType.Guard or StandardConditionType.Ward or StandardConditionType.Taunt)
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
        return before - target.Conditions.Count;
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
        ApplyLifeSteal(effect, source, target, healthDamage, combatants, statsSource);

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
        RuntimeCondition? condition = null;
        for (var index = 0; index < target.Conditions.Count; index++)
        {
            var candidate = target.Conditions[index];
            if (candidate.Type != type || candidate.Value <= 0)
                continue;

            if (condition is null || candidate.ApplicationOrder < condition.ApplicationOrder)
                condition = candidate;
        }

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
                Instigator: eventSource,
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
                Instigator: source,
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
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            _effectTickBuffer.Clear();
            for (var effectIndex = 0; effectIndex < combatant.ActiveEffects.Count; effectIndex++)
                _effectTickBuffer.Add(combatant.ActiveEffects[effectIndex]);

            for (var effectIndex = 0; effectIndex < _effectTickBuffer.Count; effectIndex++)
            {
                var effect = _effectTickBuffer[effectIndex];
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
                        var value = effect.AppliedModifierValue
                                    ?? CalculateValue(effect.Definition, effect.Source, effect.Target, combatants);
                        RemoveModifierValue(effect.Definition, effect.Target, value);
                        Log(effect.Source, effect.Target, effect.Definition.Id, EventType.BuffExpired, -value, $"{effect.Target.Name}'s modifier returned to normal.", effect.StatsSource);
                    }

                    combatant.ActiveEffects.Remove(effect);
                }
            }
        }
    }

    private void RemoveModifierValue(
        CompiledEffect effect,
        RuntimeCombatant target,
        int value)
    {
        switch (effect.Operation)
        {
            case AbilityEffectOperation.ModifyAttribute:
                target.AdjustAttribute(effect.Attribute!.Value, -value);
                break;
            case AbilityEffectOperation.ModifyThreat:
                target.AdjustThreat(-value, _currentTick, _threatDecayPerTick);
                break;
            case AbilityEffectOperation.ModifyRegenerationRate:
                target.AdjustRegenerationRate(-value);
                break;
            case AbilityEffectOperation.ModifyRegenerationInterval:
                target.AdjustRegenerationInterval(-value);
                break;
            case AbilityEffectOperation.ModifyHealingReceived:
                target.AdjustHealingReceived(-value);
                break;
            case AbilityEffectOperation.ModifyDamageDealt:
                target.AdjustDamageDealt(effect.DamageType, -value);
                break;
            case AbilityEffectOperation.ModifyDamageDealtToLowHealth:
                target.AdjustDamageDealtToLowHealth(effect.HealthStepPercent, -value);
                break;
            case AbilityEffectOperation.ModifyDamageTaken:
                target.AdjustDamageTaken(effect.DamageType, -value);
                break;
            case AbilityEffectOperation.ModifyDamageTakenFromCondition:
                target.AdjustDamageTakenFromCondition(effect.Condition!.Value, -value);
                break;
        }
    }

    private void TickStatuses(IReadOnlyList<RuntimeCombatant> combatants)
    {
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            _statusTickBuffer.Clear();
            for (var statusIndex = 0; statusIndex < combatant.Statuses.Count; statusIndex++)
                _statusTickBuffer.Add(combatant.Statuses[statusIndex]);

            for (var statusIndex = 0; statusIndex < _statusTickBuffer.Count; statusIndex++)
            {
                var status = _statusTickBuffer[statusIndex];
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
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            if (!combatant.IsAlive)
                continue;

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
            PublishIfObserved(AbilityTriggerEvent.OnHeal, combatant, combatant, null, combatants, restored);
            PublishIfObserved(AbilityTriggerEvent.OnHealed, combatant, combatant, null, combatants, restored);
            PublishIfObserved(AbilityTriggerEvent.OnEnemyHealed, combatant, combatant, null, combatants, restored);
            PublishIfObserved(AbilityTriggerEvent.OnHealthChanged, combatant, combatant, null, combatants, restored);
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
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            _conditionTickBuffer.Clear();
            for (var conditionIndex = 0; conditionIndex < combatant.Conditions.Count; conditionIndex++)
                _conditionTickBuffer.Add(combatant.Conditions[conditionIndex]);

            for (var conditionIndex = 0; conditionIndex < _conditionTickBuffer.Count; conditionIndex++)
            {
                var condition = _conditionTickBuffer[conditionIndex];
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

    private void AdjustThreatAndTrack(
        RuntimeCombatant combatant,
        float amount,
        string statsSource)
    {
        combatant.AdjustThreat(amount, _currentTick, _threatDecayPerTick);
        var generated = (int)Math.Round(
            amount * combatant.ThreatMultiplier,
            MidpointRounding.AwayFromZero);
        if (generated == 0)
            return;

        if (!_threatGeneration.TryGetValue(combatant, out var telemetry))
        {
            telemetry = new ThreatGenerationTelemetry();
            _threatGeneration[combatant] = telemetry;
        }

        telemetry.Total += generated;
        if (!string.IsNullOrWhiteSpace(statsSource))
        {
            telemetry.ByAbility[statsSource] =
                telemetry.ByAbility.GetValueOrDefault(statsSource) + generated;
        }
    }

    private IReadOnlyList<EntityStats> AddThreatGenerationTelemetry(
        IReadOnlyList<EntityStats> aggregatedStats)
    {
        var result = aggregatedStats.ToList();

        foreach (var (combatant, telemetry) in _threatGeneration)
        {
            var index = result.FindIndex(stats =>
                stats.EntityId.Equals(combatant.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                result.Add(new EntityStats(
                    combatant.Id,
                    combatant.Name,
                    telemetry.ByAbility
                        .Select(entry => new AbilityStats(entry.Key, TotalThreat: entry.Value))
                        .ToList(),
                    Team: combatant.Team.ToString(),
                    ThreatGenerated: telemetry.Total));
                continue;
            }

            var stats = result[index];
            var abilities = stats.Abilities.ToList();
            foreach (var (abilityName, totalThreat) in telemetry.ByAbility)
            {
                var abilityIndex = abilities.FindIndex(ability =>
                    ability.Name.Equals(abilityName, StringComparison.OrdinalIgnoreCase));
                if (abilityIndex >= 0)
                {
                    abilities[abilityIndex] = abilities[abilityIndex] with
                    {
                        TotalThreat = totalThreat
                    };
                }
                else
                {
                    abilities.Add(new AbilityStats(abilityName, TotalThreat: totalThreat));
                }
            }

            result[index] = stats with
            {
                Abilities = abilities,
                ThreatGenerated = telemetry.Total
            };
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
            {
                result.Add(new EntityStats(
                    combatant.Id,
                    combatant.Name,
                    [],
                    Team: combatant.Team.ToString()));
                index = result.Count - 1;
            }

            result[index] = result[index] with
            {
                Health = (int)combatant.Health,
                MaxHealth = (int)combatant.GetAttribute(AttributeType.MaxHealth),
                Barrier = (int)combatant.Barrier
            };
        }

        return result;
    }

    private static IReadOnlyList<EntityStats> AddAttentionTelemetry(
        IReadOnlyList<EntityStats> aggregatedStats,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var result = aggregatedStats.ToList();
        var totalsByTeam = result
            .GroupBy(stats => stats.Team, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(stats => stats.TargetedAttacks),
                StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < result.Count; index++)
        {
            var stats = result[index];
            var total = totalsByTeam.GetValueOrDefault(stats.Team);
            result[index] = stats with
            {
                AttentionSharePercent = total <= 0
                    ? 0
                    : Math.Round(stats.TargetedAttacks * 100d / total, 2)
            };
        }

        return result;
    }

    private void TickBarrierContributions(IReadOnlyList<RuntimeCombatant> combatants)
    {
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var target = combatants[combatantIndex];
            _barrierTickBuffer.Clear();
            for (var contributionIndex = 0;
                 contributionIndex < target.BarrierContributions.Count;
                 contributionIndex++)
            {
                _barrierTickBuffer.Add(target.BarrierContributions[contributionIndex]);
            }

            for (var contributionIndex = 0;
                 contributionIndex < _barrierTickBuffer.Count;
                 contributionIndex++)
            {
                var contribution = _barrierTickBuffer[contributionIndex];
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

    private void TickCovers(IReadOnlyList<RuntimeCombatant> combatants)
    {
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var target = combatants[combatantIndex];
            _coverTickBuffer.Clear();
            _coverTickBuffer.AddRange(target.Covers);
            for (var coverIndex = 0; coverIndex < _coverTickBuffer.Count; coverIndex++)
            {
                var cover = _coverTickBuffer[coverIndex];
                cover.Tick();
                if (!cover.IsActive)
                    target.Covers.Remove(cover);
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

        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            combatant.ActiveEffects.RemoveAll(effect =>
                effect.ActivationId == activationId
                && effect.Definition.Id.Equals(linkedEffectId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void TickSummons(IReadOnlyList<RuntimeCombatant> combatants)
    {
        _summonGroupTickBuffer.Clear();
        foreach (var group in _summonGroups.Values)
        {
            if (group.ExpiresAtTick <= _currentTick)
                _summonGroupTickBuffer.Add(group);
        }

        for (var groupIndex = 0; groupIndex < _summonGroupTickBuffer.Count; groupIndex++)
        {
            var group = _summonGroupTickBuffer[groupIndex];
            _summonGroups.Remove(group.InstanceId);
            var survivingCount = 0;
            for (var memberIndex = 0; memberIndex < group.Members.Count; memberIndex++)
            {
                if (group.Members[memberIndex].IsAlive)
                    survivingCount++;
            }

            for (var memberIndex = 0; memberIndex < group.Members.Count; memberIndex++)
            {
                var member = group.Members[memberIndex];
                if (!member.IsAlive)
                    continue;

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

        _summonTickBuffer.Clear();
        for (var combatantIndex = 0; combatantIndex < combatants.Count; combatantIndex++)
        {
            var combatant = combatants[combatantIndex];
            if (combatant.IsSummoned
                && combatant.IsAlive
                && string.IsNullOrWhiteSpace(combatant.SummonGroupInstanceId))
            {
                _summonTickBuffer.Add(combatant);
            }
        }

        for (var summonIndex = 0; summonIndex < _summonTickBuffer.Count; summonIndex++)
        {
            var summon = _summonTickBuffer[summonIndex];
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
                or AbilityTriggerEvent.OnDamageDealt
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
                || ReferenceEquals(combatEvent.Target, listener)
                || ReferenceEquals(combatEvent.Instigator, listener),
            AbilityTriggerEvent.OnBarrierApplied
                or AbilityTriggerEvent.OnBarrierAbsorbed
                or AbilityTriggerEvent.OnBarrierBroken
                or AbilityTriggerEvent.OnBarrierContributionBroken
                or AbilityTriggerEvent.OnBarrierExpired =>
                ReferenceEquals(combatEvent.Source, listener)
                || ReferenceEquals(combatEvent.Target, listener),
            AbilityTriggerEvent.OnStaggerBroken =>
                ReferenceEquals(combatEvent.Source, listener)
                || ReferenceEquals(combatEvent.Target, listener),
            AbilityTriggerEvent.OnEnemyDeath =>
                combatEvent.Source is { } deadCombatant
                && deadCombatant.Team != listener.Team,
            AbilityTriggerEvent.OnEnemyHealed =>
                combatEvent.Target is { } healedCombatant
                && healedCombatant.Team != listener.Team,
            _ => true
        };

    private static bool AreAbilityAllies(RuntimeCombatant source, RuntimeCombatant candidate) =>
        candidate.Team == source.Team
        && (!source.PartyNumber.HasValue || candidate.PartyNumber == source.PartyNumber);

    private static bool CanAbilityAffectTarget(RuntimeCombatant source, RuntimeCombatant target) =>
        target.Team != source.Team || AreAbilityAllies(source, target);

    private int FillTargets(
        RuntimeCombatant[] targets,
        RuntimeCombatant source,
        AbilityTargetSelector targetSelector,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? summonId = null,
        StandardConditionType? targetCondition = null,
        bool excludeEventTarget = false,
        bool ignoreTaunt = false,
        bool excludeSummons = false,
        bool useHealthPercentage = false,
        bool randomizeTies = false)
    {
        RuntimeCombatant? target;
        switch (targetSelector)
        {
            case AbilityTargetSelector.Self:
            case AbilityTargetSelector.Source:
                targets[0] = source;
                return 1;
            case AbilityTargetSelector.EventSource:
                return AddSingleTarget(targets, combatEvent.Source);
            case AbilityTargetSelector.EventTarget:
                return AddSingleTarget(targets, combatEvent.Target);
            case AbilityTargetSelector.CurrentTarget:
                target = SelectLockedEnemy(source, combatEvent) ?? SelectAttentionTarget(source, combatants);
                return AddSingleTarget(targets, target);
            case AbilityTargetSelector.RandomEnemy:
                target = SelectLockedEnemy(source, combatEvent) ?? SelectRandomEnemy(source, combatants);
                return AddSingleTarget(targets, target);
            case AbilityTargetSelector.HighestConditionStacksEnemy:
                target = SelectHighestConditionStacksEnemy(source, combatants, targetCondition!.Value);
                return AddSingleTarget(targets, target);
            case AbilityTargetSelector.LowestHealthAlly:
            case AbilityTargetSelector.HighestMaxHealthAlly:
            case AbilityTargetSelector.LowestHealthEnemy:
            case AbilityTargetSelector.HighestHealthEnemy:
            case AbilityTargetSelector.LowestCurrentHealthEnemy:
            case AbilityTargetSelector.HighestMaxHealthEnemy:
            case AbilityTargetSelector.HighestCurrentHealthOwnedSummon:
                if (targetSelector is AbilityTargetSelector.LowestHealthEnemy
                        or AbilityTargetSelector.HighestHealthEnemy
                        or AbilityTargetSelector.LowestCurrentHealthEnemy
                        or AbilityTargetSelector.HighestMaxHealthEnemy
                    && !ignoreTaunt
                    && SelectForcedTaunter(source, combatants) is { } forcedTarget)
                {
                    targets[0] = forcedTarget;
                    return 1;
                }

                if (targetSelector == AbilityTargetSelector.LowestHealthEnemy
                    && SelectLockedEnemy(source, combatEvent) is { } lockedTarget)
                {
                    targets[0] = lockedTarget;
                    return 1;
                }

                return AddSingleTarget(
                    targets,
                    SelectExtremumTarget(
                        source,
                        targetSelector,
                        combatants,
                        summonId,
                        excludeSummons,
                        useHealthPercentage,
                        randomizeTies));
            case AbilityTargetSelector.RandomAlly:
                return FillRandomTargets(targets, source, combatants, allies: true, count: 1);
            case AbilityTargetSelector.TwoRandomEnemies:
                return FillRandomTargets(
                    targets,
                    source,
                    combatants,
                    allies: false,
                    count: 2,
                    excludedTarget: excludeEventTarget ? combatEvent.Target : null);
            case AbilityTargetSelector.ThreeRandomEnemies:
                return FillRandomTargets(
                    targets,
                    source,
                    combatants,
                    allies: false,
                    count: 3,
                    excludedTarget: excludeEventTarget ? combatEvent.Target : null);
            case AbilityTargetSelector.TwoEnemies:
                return _threatAndTankingEnabled
                    ? FillAttentionTargets(targets, source, combatants, 2)
                    : FillFilteredTargets(targets, source, targetSelector, combatants, summonId);
            case AbilityTargetSelector.ThreeEnemies:
                return _threatAndTankingEnabled
                    ? FillAttentionTargets(targets, source, combatants, 3)
                    : FillFilteredTargets(targets, source, targetSelector, combatants, summonId);
            case AbilityTargetSelector.AllEnemies:
            case AbilityTargetSelector.AllAllies:
            case AbilityTargetSelector.EveryoneButSelf:
            case AbilityTargetSelector.TwoAllies:
            case AbilityTargetSelector.SummonedAllies:
            case AbilityTargetSelector.NonSummonedAllies:
            case AbilityTargetSelector.SummonedEnemies:
            case AbilityTargetSelector.OwnedSummons:
                return FillFilteredTargets(targets, source, targetSelector, combatants, summonId);
            default:
                return 0;
        }
    }

    private int FillAttentionTargets(
        RuntimeCombatant[] targets,
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants,
        int maximumTargets)
    {
        var targetCount = 0;
        while (targetCount < maximumTargets
               && SelectAttentionTarget(source, combatants, targets, targetCount) is { } selected)
        {
            targets[targetCount++] = selected;
        }

        return targetCount;
    }

    private static int AddSingleTarget(RuntimeCombatant[] targets, RuntimeCombatant? target)
    {
        if (target is null)
            return 0;

        targets[0] = target;
        return 1;
    }

    private static int FillFilteredTargets(
        RuntimeCombatant[] targets,
        RuntimeCombatant source,
        AbilityTargetSelector targetSelector,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? summonId)
    {
        var targetCount = 0;
        var maximumTargets = targetSelector switch
        {
            AbilityTargetSelector.TwoEnemies or AbilityTargetSelector.TwoAllies => 2,
            AbilityTargetSelector.ThreeEnemies => 3,
            _ => int.MaxValue
        };
        var summonTag = targetSelector == AbilityTargetSelector.OwnedSummons
            ? $"Summon.{summonId}"
            : null;

        for (var index = 0; index < combatants.Count && targetCount < maximumTargets; index++)
        {
            var candidate = combatants[index];
            if (!candidate.IsAlive)
                continue;

            var matches = targetSelector switch
            {
                AbilityTargetSelector.AllEnemies
                    or AbilityTargetSelector.TwoEnemies
                    or AbilityTargetSelector.ThreeEnemies => candidate.Team != source.Team,
                AbilityTargetSelector.AllAllies
                    or AbilityTargetSelector.TwoAllies => AreAbilityAllies(source, candidate),
                AbilityTargetSelector.EveryoneButSelf => candidate.Id != source.Id
                    && (candidate.Team != source.Team || AreAbilityAllies(source, candidate)),
                AbilityTargetSelector.SummonedAllies => AreAbilityAllies(source, candidate) && candidate.IsSummoned,
                AbilityTargetSelector.NonSummonedAllies => AreAbilityAllies(source, candidate) && !candidate.IsSummoned,
                AbilityTargetSelector.SummonedEnemies => candidate.Team != source.Team && candidate.IsSummoned,
                AbilityTargetSelector.OwnedSummons => candidate.IsSummoned
                    && ReferenceEquals(candidate.SummonOwner, source)
                    && candidate.Tags.Contains(summonTag!),
                _ => false
            };

            if (matches)
                targets[targetCount++] = candidate;
        }

        return targetCount;
    }

    private int FillRandomTargets(
        RuntimeCombatant[] targets,
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants,
        bool allies,
        int count,
        RuntimeCombatant? excludedTarget = null)
    {
        Span<int> selectedOrders = stackalloc int[3];
        var selectedCount = 0;

        for (var index = 0; index < combatants.Count; index++)
        {
            var candidate = combatants[index];
            if (!candidate.IsAlive
                || ReferenceEquals(candidate, excludedTarget)
                || (allies
                    ? !AreAbilityAllies(source, candidate)
                    : candidate.Team == source.Team))
                continue;

            var order = _targetingRandom.Next();
            var insertionIndex = selectedCount;
            while (insertionIndex > 0 && order < selectedOrders[insertionIndex - 1])
                insertionIndex--;

            if (insertionIndex >= count)
                continue;

            var newCount = Math.Min(count, selectedCount + 1);
            for (var shiftIndex = newCount - 1; shiftIndex > insertionIndex; shiftIndex--)
            {
                targets[shiftIndex] = targets[shiftIndex - 1];
                selectedOrders[shiftIndex] = selectedOrders[shiftIndex - 1];
            }

            targets[insertionIndex] = candidate;
            selectedOrders[insertionIndex] = order;
            selectedCount = newCount;
        }

        return selectedCount;
    }

    private RuntimeCombatant? SelectHighestConditionStacksEnemy(
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants,
        StandardConditionType condition)
    {
        RuntimeCombatant? selected = null;
        var highestStacks = -1;
        var tieCount = 0;

        for (var index = 0; index < combatants.Count; index++)
        {
            var candidate = combatants[index];
            if (!candidate.IsAlive || candidate.Team == source.Team)
                continue;

            var stacks = candidate.GetConditionStacks(condition);
            if (stacks > highestStacks)
            {
                selected = candidate;
                highestStacks = stacks;
                tieCount = 1;
                continue;
            }

            if (stacks != highestStacks)
                continue;

            tieCount++;
            if (_targetingRandom.Next(tieCount) == 0)
                selected = candidate;
        }

        return selected;
    }

    private RuntimeCombatant? SelectExtremumTarget(
        RuntimeCombatant source,
        AbilityTargetSelector targetSelector,
        IReadOnlyList<RuntimeCombatant> combatants,
        string? summonId,
        bool excludeSummons = false,
        bool useHealthPercentage = false,
        bool randomizeTies = false)
    {
        RuntimeCombatant? selected = null;
        var selectedValue = 0f;
        var tieCount = 0;
        var summonTag = targetSelector == AbilityTargetSelector.HighestCurrentHealthOwnedSummon
                        && !string.IsNullOrWhiteSpace(summonId)
            ? $"Summon.{summonId}"
            : null;

        for (var index = 0; index < combatants.Count; index++)
        {
            var candidate = combatants[index];
            if (!candidate.IsAlive || excludeSummons && candidate.IsSummoned)
                continue;

            var isCandidate = targetSelector switch
            {
                AbilityTargetSelector.LowestHealthAlly
                    or AbilityTargetSelector.HighestMaxHealthAlly => AreAbilityAllies(source, candidate),
                AbilityTargetSelector.LowestHealthEnemy
                    or AbilityTargetSelector.HighestHealthEnemy
                    or AbilityTargetSelector.LowestCurrentHealthEnemy
                    or AbilityTargetSelector.HighestMaxHealthEnemy => candidate.Team != source.Team,
                AbilityTargetSelector.HighestCurrentHealthOwnedSummon => candidate.IsSummoned
                    && ReferenceEquals(candidate.SummonOwner, source)
                    && candidate.Health > source.Health
                    && (summonTag is null || candidate.Tags.Contains(summonTag)),
                _ => false
            };
            if (!isCandidate)
                continue;

            var value = useHealthPercentage
                        && targetSelector is AbilityTargetSelector.LowestHealthEnemy
                            or AbilityTargetSelector.HighestHealthEnemy
                ? candidate.Health / Math.Max(1, candidate.GetAttribute(AttributeType.MaxHealth))
                : targetSelector switch
            {
                AbilityTargetSelector.HighestMaxHealthAlly
                    or AbilityTargetSelector.HighestMaxHealthEnemy => candidate.GetAttribute(AttributeType.MaxHealth),
                AbilityTargetSelector.LowestHealthEnemy =>
                    candidate.Health / Math.Max(1, candidate.GetAttribute(AttributeType.MaxHealth)),
                _ => candidate.Health
            };
            var selectCandidate = selected is null || targetSelector switch
            {
                AbilityTargetSelector.LowestHealthAlly
                    or AbilityTargetSelector.LowestHealthEnemy
                    or AbilityTargetSelector.LowestCurrentHealthEnemy => value < selectedValue,
                _ => value > selectedValue
            };
            if (selectCandidate)
            {
                selected = candidate;
                selectedValue = value;
                tieCount = 1;
                continue;
            }

            if (!randomizeTies || Math.Abs(value - selectedValue) > float.Epsilon)
                continue;

            tieCount++;
            if (_targetingRandom.Next(tieCount) == 0)
                selected = candidate;
        }

        return selected;
    }

    private static int CountLivingOwnedSummons(
        RuntimeCombatant source,
        string summonId,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var count = 0;
        var summonTag = $"Summon.{summonId}";
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (combatant.IsAlive
                && combatant.IsSummoned
                && ReferenceEquals(combatant.SummonOwner, source)
                && combatant.Tags.Contains(summonTag))
            {
                count++;
            }
        }

        return count;
    }

    private RuntimeCombatant? SelectActiveAbilityPrimaryTarget(
        RuntimeAbility ability,
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!ability.Definition.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers))
            return null;

        CompiledEffect? primaryEffect = null;
        for (var triggerIndex = 0; triggerIndex < triggers.Count && primaryEffect is null; triggerIndex++)
        {
            var effects = triggers[triggerIndex].Effects;
            for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                var candidate = effects[effectIndex].Target;
                if (candidate is not (AbilityTargetSelector.CurrentTarget
                    or AbilityTargetSelector.RandomEnemy
                    or AbilityTargetSelector.LowestHealthEnemy
                    or AbilityTargetSelector.HighestHealthEnemy
                    or AbilityTargetSelector.LowestCurrentHealthEnemy
                    or AbilityTargetSelector.HighestMaxHealthEnemy))
                {
                    continue;
                }

                primaryEffect = effects[effectIndex];
                break;
            }
        }

        return primaryEffect?.Target switch
        {
            AbilityTargetSelector.CurrentTarget => SelectAttentionTarget(source, combatants),
            AbilityTargetSelector.RandomEnemy => SelectRandomEnemy(source, combatants),
            AbilityTargetSelector.LowestHealthEnemy
                or AbilityTargetSelector.HighestHealthEnemy
                or AbilityTargetSelector.LowestCurrentHealthEnemy
                or AbilityTargetSelector.HighestMaxHealthEnemy =>
                SelectExtremumTarget(
                    source,
                    primaryEffect.Target,
                    combatants,
                    summonId: null,
                    primaryEffect.ExcludeSummons,
                    primaryEffect.UseHealthPercentage,
                    primaryEffect.RandomizeTies),
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
            if (string.IsNullOrWhiteSpace(effect.SummonId))
                return false;

            var summonTag = $"Summon.{effect.SummonId}";
            for (var index = 0; index < combatants.Count; index++)
            {
                var combatant = combatants[index];
                if (combatant.IsAlive
                    && combatant.IsSummoned
                    && ReferenceEquals(combatant.SummonOwner, source)
                    && combatant.Tags.Contains(summonTag))
                {
                    return true;
                }
            }

            return false;
        }

        if (effect.Operation != AbilityEffectOperation.Summon || string.IsNullOrWhiteSpace(effect.SummonId))
            return true;

        if (!_summonsById.TryGetValue(effect.SummonId, out var summonDefinition))
            return false;

        if (!HasReachedSummonCap(source, summonDefinition, combatants))
            return true;

        return effect.HealingScalingAttribute is not null
               && effect.HealingScalingCoefficient > 0
               && source.Health < source.GetAttribute(AttributeType.MaxHealth);
    }

    private static bool HasReachedSummonCap(
        RuntimeCombatant source,
        CompiledSummon summonDefinition,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (summonDefinition.MaxActive <= 0)
            return false;

        var count = 0;
        var summonTag = $"Summon.{summonDefinition.Id}";
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (!combatant.IsAlive
                || !combatant.IsSummoned
                || !ReferenceEquals(combatant.SummonOwner, source)
                || !combatant.Tags.Contains(summonTag))
            {
                continue;
            }

            count++;
            if (count >= summonDefinition.MaxActive)
                return true;
        }

        return false;
    }

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

    private RuntimeCombatant? SelectAttentionTarget(
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants,
        RuntimeCombatant[]? excludedTargets = null,
        int excludedCount = 0)
    {
        var threatBuffer = ArrayPool<double>.Shared.Rent(Math.Max(1, combatants.Count));
        try
        {
            var threatCount = 0;
            var hasTaunter = false;
            for (var index = 0; index < combatants.Count; index++)
            {
                var candidate = combatants[index];
                if (candidate.Team == source.Team || !candidate.IsAlive)
                    continue;

                threatBuffer[threatCount++] = GetEffectiveThreat(candidate);
                if (_threatAndTankingEnabled
                    && candidate.HasCondition(StandardConditionType.Taunt)
                    && !candidate.HasCondition(StandardConditionType.Stealth)
                    && !IsExcluded(candidate, excludedTargets, excludedCount))
                {
                    hasTaunter = true;
                }
            }

            if (threatCount == 0)
                return null;

            Array.Sort(threatBuffer, 0, threatCount);
            var median = threatCount % 2 == 0
                ? (threatBuffer[threatCount / 2 - 1] + threatBuffer[threatCount / 2]) / 2d
                : threatBuffer[threatCount / 2];

            RuntimeCombatant? firstCandidate = null;
            RuntimeCombatant? lastCandidate = null;
            var totalWeight = 0d;
            for (var index = 0; index < combatants.Count; index++)
            {
                var candidate = combatants[index];
                if (!IsAttentionCandidate(
                        candidate,
                        source,
                        hasTaunter,
                        excludedTargets,
                        excludedCount))
                {
                    continue;
                }

                firstCandidate ??= candidate;
                lastCandidate = candidate;
                totalWeight += GetAttentionWeight(candidate, median);
            }

            if (firstCandidate is null || totalWeight <= 0)
                return firstCandidate;

            var roll = _targetingRandom.NextDouble() * totalWeight;
            for (var index = 0; index < combatants.Count; index++)
            {
                var candidate = combatants[index];
                if (!IsAttentionCandidate(
                        candidate,
                        source,
                        hasTaunter,
                        excludedTargets,
                        excludedCount))
                {
                    continue;
                }

                roll -= GetAttentionWeight(candidate, median);
                if (roll < 0)
                    return candidate;
            }

            return lastCandidate;
        }
        finally
        {
            ArrayPool<double>.Shared.Return(threatBuffer, clearArray: true);
        }
    }

    private bool IsAttentionCandidate(
        RuntimeCombatant candidate,
        RuntimeCombatant source,
        bool hasTaunter,
        RuntimeCombatant[]? excludedTargets,
        int excludedCount) =>
        candidate.Team != source.Team
        && candidate.IsAlive
        && !IsExcluded(candidate, excludedTargets, excludedCount)
        && (!hasTaunter
            || candidate.HasCondition(StandardConditionType.Taunt)
            && !candidate.HasCondition(StandardConditionType.Stealth));

    private static bool IsExcluded(
        RuntimeCombatant candidate,
        RuntimeCombatant[]? excludedTargets,
        int excludedCount)
    {
        for (var index = 0; index < excludedCount; index++)
        {
            if (ReferenceEquals(candidate, excludedTargets![index]))
                return true;
        }

        return false;
    }

    private RuntimeCombatant? SelectRandomEnemy(RuntimeCombatant source, IReadOnlyList<RuntimeCombatant> combatants)
    {
        var enemyCount = 0;
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (combatant.Team != source.Team && combatant.IsAlive)
                enemyCount++;
        }

        if (enemyCount == 0)
            return null;

        var selectedIndex = _targetingRandom.Next(enemyCount);
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            if (combatant.Team == source.Team || !combatant.IsAlive)
                continue;

            if (selectedIndex-- == 0)
                return combatant;
        }

        return null;
    }

    private RuntimeCombatant? SelectForcedTaunter(
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!_threatAndTankingEnabled)
            return null;

        var count = 0;
        for (var index = 0; index < combatants.Count; index++)
        {
            var candidate = combatants[index];
            if (candidate.Team != source.Team
                && candidate.IsAlive
                && candidate.HasCondition(StandardConditionType.Taunt)
                && !candidate.HasCondition(StandardConditionType.Stealth))
            {
                count++;
            }
        }

        if (count == 0)
            return null;

        var selectedIndex = _targetingRandom.Next(count);
        for (var index = 0; index < combatants.Count; index++)
        {
            var candidate = combatants[index];
            if (candidate.Team == source.Team
                || !candidate.IsAlive
                || !candidate.HasCondition(StandardConditionType.Taunt)
                || candidate.HasCondition(StandardConditionType.Stealth))
            {
                continue;
            }

            if (selectedIndex-- == 0)
                return candidate;
        }

        return null;
    }

    private double GetEffectiveThreat(RuntimeCombatant combatant)
    {
        if (combatant.HasCondition(StandardConditionType.Stealth))
            return 1d;

        var threat = combatant.GetThreat(_currentTick, _threatDecayPerTick);
        if (combatant.HasCondition(StandardConditionType.Mark)
            || !_threatAndTankingEnabled && combatant.HasCondition(StandardConditionType.Taunt))
            threat += _markThreatBonus;

        return threat;
    }

    private double GetAttentionWeight(RuntimeCombatant combatant, double medianThreat)
    {
        var threat = GetEffectiveThreat(combatant);
        if (!_threatAndTankingEnabled)
            return threat;

        var ratio = threat / Math.Max(1d, medianThreat);
        return Math.Clamp(
            Math.Pow(Math.Max(0, ratio), _attentionExponent),
            _minimumAttentionWeight,
            _maximumAttentionWeight);
    }

    private bool ConditionsPass(
        IReadOnlyList<CompiledCondition> conditions,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        RuntimeCombatant? effectTarget = null)
    {
        for (var index = 0; index < conditions.Count; index++)
        {
            if (!ConditionPass(conditions[index], source, combatEvent, combatants, effectTarget))
                return false;
        }

        return true;
    }

    private bool ConditionPass(
        CompiledCondition condition,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        IReadOnlyList<RuntimeCombatant> combatants,
        RuntimeCombatant? effectTarget)
    {
        if (condition.Type is AbilityConditionType.NonSummonedEnemyHealthSpreadAtMostPercent
                or AbilityConditionType.NonSummonedEnemyHealthSpreadAbovePercent)
        {
            var spreadIsAtMost = IsNonSummonedEnemyHealthSpreadAtMost(
                source,
                combatants,
                condition.Value);
            return condition.Type == AbilityConditionType.NonSummonedEnemyHealthSpreadAtMostPercent
                ? spreadIsAtMost
                : !spreadIsAtMost;
        }

        if (condition.Type == AbilityConditionType.OutnumbersEnemies)
        {
            var livingAllies = combatants.Count(combatant =>
                combatant.IsAlive
                && !combatant.IsSummoned
                && AreAbilityAllies(source, combatant));
            var livingEnemies = combatants.Count(combatant =>
                combatant.IsAlive
                && !combatant.IsSummoned
                && combatant.Team != source.Team);
            return livingAllies > livingEnemies;
        }

        if (condition.Type == AbilityConditionType.AnyEnemyHealthBelowPercent)
        {
            for (var index = 0; index < combatants.Count; index++)
            {
                var combatant = combatants[index];
                if (combatant.Team != source.Team
                    && combatant.IsAlive
                    && IsHealthBelowPercent(combatant, condition.Value))
                {
                    return true;
                }
            }

            return false;
        }

        if (condition.Type == AbilityConditionType.NoEnemyHealthBelowPercent)
        {
            for (var index = 0; index < combatants.Count; index++)
            {
                var combatant = combatants[index];
                if (combatant.Team != source.Team
                    && combatant.IsAlive
                    && IsHealthBelowPercent(combatant, condition.Value))
                {
                    return false;
                }
            }

            return true;
        }

        if (condition.Type is AbilityConditionType.AnyEnemyHasCondition
                or AbilityConditionType.NoEnemyHasCondition)
        {
            var anyEnemyHasCondition = combatants.Any(combatant =>
                combatant.Team != source.Team
                && combatant.IsAlive
                && combatant.HasCondition(condition.Condition!.Value));
            return condition.Type == AbilityConditionType.AnyEnemyHasCondition
                ? anyEnemyHasCondition
                : !anyEnemyHasCondition;
        }

        if (condition.Type == AbilityConditionType.EventSourceIsEnemy)
            return combatEvent.Source is { } eventSource && eventSource.Team != source.Team;

        if (condition.Type == AbilityConditionType.EventSourceIsAlly)
            return combatEvent.Source is { } ally
                   && AreAbilityAllies(source, ally)
                   && !ReferenceEquals(ally, source);

        if (condition.Type == AbilityConditionType.EventTargetIsAlly)
            return combatEvent.Target is { } allyTarget
                   && AreAbilityAllies(source, allyTarget)
                   && !ReferenceEquals(allyTarget, source);

        if (condition.Type == AbilityConditionType.EventMagnitudeAtLeast)
            return combatEvent.Magnitude >= condition.Value;

        if (condition.Type == AbilityConditionType.EventMagnitudeAtMost)
            return combatEvent.Magnitude <= condition.Value;

        var subject = ResolveSubject(condition.Subject, source, combatEvent, effectTarget);
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
            AbilityConditionType.EventIdIsNot => !string.Equals(
                combatEvent.AbilityId,
                condition.StatusId,
                StringComparison.OrdinalIgnoreCase),
            AbilityConditionType.EventSourceIsSelf => ReferenceEquals(combatEvent.Source, source),
            AbilityConditionType.EventInstigatorIsSelf => ReferenceEquals(combatEvent.Instigator, source),
            AbilityConditionType.HasTag => subject.Tags.Contains(condition.Tag!),
            AbilityConditionType.ChancePercent => _random.Next(1, 101) <= condition.Value,
            AbilityConditionType.HasBarrier => subject.Barrier > 0,
            _ => false
        };
    }

    private static bool IsHealthBelowPercent(RuntimeCombatant combatant, int percent) =>
        combatant.GetAttribute(AttributeType.MaxHealth) > 0
        && combatant.Health / combatant.GetAttribute(AttributeType.MaxHealth) * 100 < percent;

    private static bool IsNonSummonedEnemyHealthSpreadAtMost(
        RuntimeCombatant source,
        IReadOnlyList<RuntimeCombatant> combatants,
        int maximumSpreadPercent)
    {
        var eligibleCount = 0;
        var lowestHealthPercent = float.MaxValue;
        var highestHealthPercent = float.MinValue;
        for (var index = 0; index < combatants.Count; index++)
        {
            var combatant = combatants[index];
            var maxHealth = combatant.GetAttribute(AttributeType.MaxHealth);
            if (!combatant.IsAlive
                || combatant.IsSummoned
                || combatant.Team == source.Team
                || maxHealth <= 0)
            {
                continue;
            }

            var healthPercent = combatant.Health / maxHealth * 100;
            lowestHealthPercent = Math.Min(lowestHealthPercent, healthPercent);
            highestHealthPercent = Math.Max(highestHealthPercent, healthPercent);
            eligibleCount++;
        }

        return eligibleCount >= 2
               && highestHealthPercent - lowestHealthPercent <= maximumSpreadPercent + float.Epsilon;
    }

    private static RuntimeCombatant? ResolveSubject(
        AbilityConditionSubject subject,
        RuntimeCombatant source,
        CombatEvent combatEvent,
        RuntimeCombatant? effectTarget) =>
        subject switch
        {
            AbilityConditionSubject.Source => source,
            AbilityConditionSubject.Target => effectTarget ?? combatEvent.Target,
            AbilityConditionSubject.EventSource => combatEvent.Source,
            AbilityConditionSubject.EventTarget => combatEvent.Target,
            _ => null
        };

    private int CalculateValue(
        CompiledEffect effect,
        RuntimeCombatant source,
        RuntimeCombatant target,
        IReadOnlyList<RuntimeCombatant> combatants,
        CombatEvent? combatEvent = null,
        int targetIndex = 0)
    {
        var scalingCoefficient = effect.ScalingCoefficient;
        if (effect.MaximumScalingCoefficient > effect.ScalingCoefficient)
        {
            scalingCoefficient += (float)_random.NextDouble()
                                  * (effect.MaximumScalingCoefficient - effect.ScalingCoefficient);
        }

        var value = effect.BaseValue
                    + (effect.ScalingAttribute is { } attribute
                        ? GetEffectiveAttribute(
                              ResolveScalingSubject(
                                  effect.ScalingAttributeSubject,
                                  source,
                                  target,
                                  combatEvent),
                              attribute)
                          * scalingCoefficient
                        : 0)
                    + (combatEvent?.Magnitude ?? 0) * effect.EventMagnitudeCoefficient;
        if (effect.ScalingCondition is { } condition)
        {
            value += ResolveScalingSubject(
                         effect.ScalingConditionSubject,
                         source,
                         target,
                         combatEvent)
                     .GetConditionStacks(condition)
                     * GetEffectivePower(source)
                     * effect.ConditionScalingCoefficient;
        }
        if (!string.IsNullOrWhiteSpace(effect.ScalingStatusId))
        {
            value += ResolveScalingSubject(
                         effect.ScalingStatusSubject,
                         source,
                         target,
                         combatEvent)
                     .GetStatusStacks(effect.ScalingStatusId)
                     * GetEffectiveAttribute(
                         ResolveScalingSubject(
                             effect.ScalingStatusSubject,
                             source,
                             target,
                             combatEvent),
                         effect.StatusScalingAttribute)
                     * effect.StatusScalingCoefficient;
        }
        if (!string.IsNullOrWhiteSpace(effect.ScalingOwnedSummonId))
        {
            value += CountLivingOwnedSummons(source, effect.ScalingOwnedSummonId, combatants)
                     * GetEffectivePower(source)
                     * effect.OwnedSummonScalingCoefficient;
        }

        if (effect.LivingNonSummonedAllyDamagePercent != 0)
        {
            var allyCount = combatants.Count(combatant =>
                combatant.IsAlive
                && !combatant.IsSummoned
                && AreAbilityAllies(source, combatant)
                && !ReferenceEquals(combatant, source));
            value *= 1 + allyCount * effect.LivingNonSummonedAllyDamagePercent / 100f;
        }

        if (targetIndex > 0 && effect.SubsequentTargetDamagePercent != 100)
            value *= (float)Math.Pow(effect.SubsequentTargetDamagePercent / 100d, targetIndex);

        return Math.Max(
            AllowsNegativeValue(effect.Operation) ? int.MinValue : 0,
            (int)Math.Round(value));
    }

    private static RuntimeCombatant ResolveScalingSubject(
        AbilityConditionSubject subject,
        RuntimeCombatant source,
        RuntimeCombatant target,
        CombatEvent? combatEvent) =>
        subject switch
        {
            AbilityConditionSubject.Target => target,
            AbilityConditionSubject.EventSource => combatEvent?.Source ?? source,
            AbilityConditionSubject.EventTarget => combatEvent?.Target ?? target,
            _ => source
        };

    private float GetEffectiveAttribute(RuntimeCombatant combatant, AttributeType attribute) =>
        attribute == AttributeType.Power
            ? GetEffectivePower(combatant)
            : combatant.GetAttribute(attribute);

    private static float GetEffectiveAttributeWithoutOvertime(
        RuntimeCombatant combatant,
        AttributeType attribute) =>
        attribute == AttributeType.Power
            ? GetConditionAdjustedPower(combatant)
            : combatant.GetAttribute(attribute);

    private float GetEffectivePower(RuntimeCombatant combatant)
    {
        var overtimeStacks = _overtimePowerIncreaseIntervalTicks > 0
            && _currentTick >= _overtimeStartsAtTick
            ? (_currentTick - _overtimeStartsAtTick) / _overtimePowerIncreaseIntervalTicks
            : 0;
        var overtimeMultiplier = 1 + overtimeStacks * _overtimePowerIncreasePercent / 100f;
        var furyMultiplier = combatant.Team == CombatTeam.Hostile
                             && !combatant.IsSummoned
                             && _hostileFuryOptions is not null
            ? 1 + GetFuryStacks() * _hostileFuryOptions.PowerPercentPerStack / 100f
            : 1;
        return Math.Max(0, GetConditionAdjustedPower(combatant) * overtimeMultiplier * furyMultiplier);
    }

    private int GetFuryStacks() =>
        _hostileFuryOptions is { IntervalTicks: > 0 }
            ? _currentTick / _hostileFuryOptions.IntervalTicks
            : 0;

    private static float GetConditionAdjustedPower(RuntimeCombatant combatant)
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
            or AbilityEffectOperation.ModifyDamageDealtToLowHealth
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition;

    private static bool IsTimedModifierOperation(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute
            or AbilityEffectOperation.ModifyThreat
            or AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyRegenerationInterval
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageDealtToLowHealth
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition;

    private static bool IsPeriodicEffect(CompiledEffect effect) =>
        effect.IntervalTicks > 0 && effect.DurationTicks > 0;

    private readonly record struct MaintainedThreatSourceKey(
        RuntimeCombatant Source,
        string StatsSource,
        AbilityThreatFunctionBand Band);

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
        int finalHealthDamage = 0,
        DamageType damageType = DamageType.None,
        int damageRedirectedTo = 0,
        int damageRedirectedAway = 0,
        bool countsAsTargetedAttack = false)
        => LogCore(
            source,
            target,
            sourceName,
            eventType,
            magnitude,
            details,
            statsSource,
            countsAsActivation,
            barrierAbsorbed,
            incomingRawDamage,
            avoidedDamage,
            typedMitigationPrevented,
            physicalMitigationPrevented,
            magicalMitigationPrevented,
            blockPrevented,
            damageReductionPrevented,
            damageAmplified,
            finalHealthDamage,
            damageType,
            damageRedirectedTo,
            damageRedirectedAway,
            countsAsTargetedAttack);

    private void Log(
        RuntimeCombatant source,
        RuntimeCombatant? target,
        string sourceName,
        EventType eventType,
        int magnitude,
        [InterpolatedStringHandlerArgument("")] ref CombatLogDetailsHandler details,
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
        int finalHealthDamage = 0,
        DamageType damageType = DamageType.None,
        int damageRedirectedTo = 0,
        int damageRedirectedAway = 0,
        bool countsAsTargetedAttack = false)
        => LogCore(
            source,
            target,
            sourceName,
            eventType,
            magnitude,
            details.GetFormattedText(),
            statsSource,
            countsAsActivation,
            barrierAbsorbed,
            incomingRawDamage,
            avoidedDamage,
            typedMitigationPrevented,
            physicalMitigationPrevented,
            magicalMitigationPrevented,
            blockPrevented,
            damageReductionPrevented,
            damageAmplified,
            finalHealthDamage,
            damageType,
            damageRedirectedTo,
            damageRedirectedAway,
            countsAsTargetedAttack);

    private void LogCore(
        RuntimeCombatant source,
        RuntimeCombatant? target,
        string sourceName,
        EventType eventType,
        int magnitude,
        string details,
        string? statsSource,
        bool countsAsActivation,
        int barrierAbsorbed,
        int incomingRawDamage,
        int avoidedDamage,
        int typedMitigationPrevented,
        int physicalMitigationPrevented,
        int magicalMitigationPrevented,
        int blockPrevented,
        int damageReductionPrevented,
        int damageAmplified,
        int finalHealthDamage,
        DamageType damageType,
        int damageRedirectedTo,
        int damageRedirectedAway,
        bool countsAsTargetedAttack)
    {
        if (!_captureEventLog)
        {
            (_checkpointStats ?? _balanceStats).Add(
                sourceName,
                statsSource ?? string.Empty,
                countsAsActivation,
                source.Id,
                GetTeamName(source.Team),
                target?.Id ?? string.Empty,
                target is null ? string.Empty : GetTeamName(target.Team),
                target?.Name,
                eventType,
                magnitude,
                barrierAbsorbed,
                incomingRawDamage,
                avoidedDamage,
                typedMitigationPrevented,
                physicalMitigationPrevented,
                magicalMitigationPrevented,
                blockPrevented,
                damageReductionPrevented,
                damageAmplified,
                finalHealthDamage,
                damageType,
                damageRedirectedTo,
                damageRedirectedAway,
                countsAsTargetedAttack,
                _currentTick);
        }

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
            DamageType = damageType,
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
            DamageRedirectedTo = damageRedirectedTo,
            DamageRedirectedAway = damageRedirectedAway,
            CountsAsTargetedAttack = countsAsTargetedAttack,
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
                    Barrier = (int)target.Barrier,
                    Threat = target.Threat,
                    CurrentStagger = target.Stagger?.Current ?? 0,
                    MaxStagger = target.Stagger?.Max ?? 0,
                    IsStaggered = target.Stagger?.IsStaggered == true,
                    IsStaggerRecovering = target.Stagger?.IsRecovering == true
                }
        });
    }

    private static string GetTeamName(CombatTeam team) =>
        team switch
        {
            CombatTeam.Friendly => nameof(CombatTeam.Friendly),
            CombatTeam.Hostile => nameof(CombatTeam.Hostile),
            _ => team.ToString()
        };

    [InterpolatedStringHandler]
    private ref struct CombatLogDetailsHandler
    {
        private DefaultInterpolatedStringHandler _builder;
        private readonly bool _enabled;

        public CombatLogDetailsHandler(
            int literalLength,
            int formattedCount,
            FastCombatEngine engine,
            out bool shouldAppend)
        {
            shouldAppend = engine._captureEventLog;
            _enabled = shouldAppend;
            _builder = shouldAppend
                ? new DefaultInterpolatedStringHandler(literalLength, formattedCount)
                : default;
        }

        public void AppendLiteral(string value) => _builder.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _builder.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _builder.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _builder.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) =>
            _builder.AppendFormatted(value, alignment, format);
        public string GetFormattedText() => _enabled
            ? _builder.ToStringAndClear()
            : string.Empty;
    }

    private void TrackBalanceDamage(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int healthDamage)
    {
        if (_captureEventLog
            || healthDamage <= 0
            || source.Team == target.Team)
            return;

        _balanceDamageDone[source.Id] = _balanceDamageDone.GetValueOrDefault(source.Id) + healthDamage;
        _balanceDamageTaken[target.Id] = _balanceDamageTaken.GetValueOrDefault(target.Id) + healthDamage;
    }

    private static BattleOutcome DetermineOutcome(IReadOnlyList<RuntimeCombatant> combatants)
    {
        var hasLivingFriendly = HasLivingTeam(combatants, CombatTeam.Friendly);
        var hasLivingHostile = HasLivingTeam(combatants, CombatTeam.Hostile);

        if (!hasLivingFriendly && !hasLivingHostile)
            return BattleOutcome.Draw;

        if (!hasLivingFriendly)
            return BattleOutcome.Defeat;

        if (!hasLivingHostile)
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

    private sealed class ThreatGenerationTelemetry
    {
        public int Total { get; set; }
        public Dictionary<string, int> ByAbility { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class EffectExecutionContext
    {
        private readonly long _activationSequence;
        private Dictionary<string, int>? _generatedHealingByEffect;
        private Dictionary<string, string>? _summonGroupInstances;
        private string? _activationId;

        public EffectExecutionContext(long activationSequence)
        {
            _activationSequence = activationSequence;
        }

        public string ActivationId =>
            _activationId ??= _activationSequence.ToString(CultureInfo.InvariantCulture);

        public int GetGeneratedHealing(string effectId) =>
            _generatedHealingByEffect?.GetValueOrDefault(effectId) ?? 0;

        public void AddGeneratedHealing(string effectId, int amount)
        {
            _generatedHealingByEffect ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _generatedHealingByEffect[effectId] = GetGeneratedHealing(effectId) + Math.Max(0, amount);
        }

        public string GetSummonGroupInstanceId(string groupId)
        {
            _summonGroupInstances ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_summonGroupInstances.TryGetValue(groupId, out var existing))
                return existing;

            var created = $"{groupId}:{ActivationId}";
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
