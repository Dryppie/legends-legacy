namespace Services.LL.Combat.Engine;

public enum CombatMechanicEventKind
{
    BarrierStarted, BarrierBroken, BarrierTimedOut, LinkedPeriodicApplication,
    SummonSpawned, SummonKilled, SummonTimedOut, SummonOwnerDied, SummonEnded,
    SummonGroupResolved, StatusObserved, BarrierDamageConsumed
}

/// <summary>Scalar observations in engine order; Value means barrier amount, stack count or group survivors.</summary>
public sealed record CombatMechanicEvent(
    int Tick, CombatMechanicEventKind Kind, string EntityId, string SourceId, string DefinitionId,
    double Value = 0, string? ActivationId = null, string? LinkedEffectId = null,
    long? ApplicationOrder = null, string? GroupInstanceId = null, bool? Locked = null, string? Reason = null,
    int? ScheduledEndTick = null)
{
    // Absent in schema 1, including when explicitly serializing null properties.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CombatBarrierDamageSource? DamageSource { get; init; }
}

/// <summary>Attacking effect IDs and reporting labels are optional; neither is a canonical ability ID.</summary>
public sealed record CombatBarrierDamageSource(string AttackerId, string? EffectId, string? ReportingLabel,
    string DamageType, string Delivery);

/// <summary>Separate from combat results and archived battle reports. Incomplete/truncated traces are not exhaustive.</summary>
public sealed record CombatMechanicTrace(int SchemaVersion, bool Completed, int? FinalTick,
    int MaximumEvents, bool Truncated, long DroppedObservations, IReadOnlyList<string> BarrierEffectIds,
    IReadOnlyList<string> StatusIds, IReadOnlyList<string> SummonGroupIds, IReadOnlyList<CombatMechanicEvent> Events);

/// <summary>
/// Opt-in, single-engine collector. Copies filters and snapshots, never calls back into combat, and bounds storage.
/// Linked periodic events count target application attempts after the chance check, not damage or pulse waves.
/// </summary>
public sealed class CombatMechanicDiagnostics
{
    /// <summary>Explicit schema-2 opt-in. Default schema-1 serialization and observation order stay unchanged.</summary>
    public bool CaptureBarrierDamage { get; init; }

    private readonly HashSet<string> _barriers;
    private readonly HashSet<string> _statuses;
    private readonly HashSet<string> _groups;
    private readonly HashSet<(string Activation, string Effect)> _linked = [];
    private readonly List<CombatMechanicEvent> _events = [];
    private readonly int _maximumEvents;
    private bool _attached;
    private bool _started;
    private bool _completed;
    private int? _finalTick;
    private long _dropped;

    public CombatMechanicDiagnostics(IEnumerable<string> barrierEffectIds, IEnumerable<string> statusIds,
        IEnumerable<string> summonGroupIds, int maximumEvents = 8192)
    {
        if (maximumEvents is < 1 or > 65536)
            throw new ArgumentOutOfRangeException(nameof(maximumEvents));
        _maximumEvents = maximumEvents;
        _barriers = CopyFilter(barrierEffectIds);
        _statuses = CopyFilter(statusIds);
        _groups = CopyFilter(summonGroupIds);
    }

    public CombatMechanicTrace Snapshot() => new(CaptureBarrierDamage ? 2 : 1, _completed, _finalTick, _maximumEvents,
        _dropped > 0, _dropped, Array.AsReadOnly(_barriers.Order(StringComparer.Ordinal).ToArray()),
        Array.AsReadOnly(_statuses.Order(StringComparer.Ordinal).ToArray()),
        Array.AsReadOnly(_groups.Order(StringComparer.Ordinal).ToArray()), Array.AsReadOnly(_events.ToArray()));

    internal void Attach()
    {
        if (_attached)
            throw new InvalidOperationException("Mechanic diagnostics require a fresh collector per engine.");
        _attached = true;
    }

    internal void Complete(int tick)
    {
        _completed = true;
        _finalTick = tick;
    }

    internal void Begin()
    {
        if (_started)
            throw new InvalidOperationException("Mechanic diagnostics require a fresh engine and collector per encounter.");
        _started = true;
    }

    private static HashSet<string> CopyFilter(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Mechanic filter IDs must be nonempty.", nameof(values));
            result.Add(value);
            if (result.Count > 256)
                throw new ArgumentException("At most 256 distinct IDs per mechanic filter.", nameof(values));
        }
        return result;
    }

    // Stop retaining new correlations when full as well as stopping the event list.
    private bool HasRoom()
    {
        if (_events.Count < _maximumEvents)
            return true;
        if (_dropped < long.MaxValue)
            _dropped++;
        return false;
    }

    internal void Barrier(int tick, CombatMechanicEventKind kind, RuntimeCombatant source,
        RuntimeCombatant target, string? effectId, string? activationId, string? linkedEffectId,
        long applicationOrder, double amount)
    {
        if (effectId is null || !_barriers.Contains(effectId) || !HasRoom())
            return;
        if (kind == CombatMechanicEventKind.BarrierStarted && activationId is not null && linkedEffectId is not null)
            _linked.Add((activationId, linkedEffectId.ToUpperInvariant()));
        _events.Add(new(tick, kind, target.Id, source.Id, effectId, amount, activationId,
            linkedEffectId, applicationOrder));
    }

    internal void Periodic(int tick, RuntimeEffect effect)
    {
        if (effect.ActivationId is null
            || !_linked.Contains((effect.ActivationId, effect.Definition.Id.ToUpperInvariant())) || !HasRoom())
            return;
        _events.Add(new(tick, CombatMechanicEventKind.LinkedPeriodicApplication, effect.Target.Id,
            effect.Source.Id, effect.Definition.Id, ActivationId: effect.ActivationId));
    }

    // The engine calls this for the whole consumption result before any absorption callbacks.
    // An empty provider ID means unknown; it must not be attributed to the defender.
    internal void BarrierDamage(int tick, RuntimeCombatant attacker, RuntimeCombatant target,
        RuntimeBarrierConsumption consumption, string? attackingEffectId, string? reportingLabel,
        string damageType, string delivery)
    {
        if (!CaptureBarrierDamage)
            return;
        foreach (var contribution in consumption.Contributions)
        {
            if (contribution.EffectId is null || !_barriers.Contains(contribution.EffectId) || !HasRoom())
                continue;
            _events.Add(new(tick, CombatMechanicEventKind.BarrierDamageConsumed, target.Id,
                contribution.Source?.Id ?? string.Empty, contribution.EffectId, contribution.Amount,
                contribution.ActivationId, contribution.LinkedEffectId, contribution.ApplicationOrder)
            {
                DamageSource = new(attacker.Id, attackingEffectId, reportingLabel, damageType, delivery)
            });
        }
    }

    internal void Summon(int tick, RuntimeCombatant summon, CombatMechanicEventKind kind,
        string? reason = null, int? scheduledEndTick = null)
    {
        if (!summon.IsSummoned || summon.SummonGroupId is null || !_groups.Contains(summon.SummonGroupId) || !HasRoom())
            return;
        _events.Add(new(tick, kind, summon.Id, (summon.SummonOwner ?? summon).Id,
            summon.SummonGroupId, GroupInstanceId: summon.SummonGroupInstanceId, Reason: reason,
            ScheduledEndTick: scheduledEndTick));
    }

    internal void GroupResolved(int tick, RuntimeCombatant owner, string groupId, string instanceId, int survivors)
    {
        if (_groups.Contains(groupId) && HasRoom())
            _events.Add(new(tick, CombatMechanicEventKind.SummonGroupResolved, owner.Id, owner.Id,
                groupId, survivors, GroupInstanceId: instanceId));
    }

    internal void Status(int tick, RuntimeCombatant source, RuntimeCombatant target, RuntimeStatus status,
        string reason, bool removed = false)
    {
        if (_statuses.Contains(status.Definition.Id) && HasRoom())
            _events.Add(new(tick, CombatMechanicEventKind.StatusObserved, target.Id, source.Id,
                status.Definition.Id, removed ? 0 : status.Stacks, Locked: status.IsRemovalLocked, Reason: reason));
    }
}
