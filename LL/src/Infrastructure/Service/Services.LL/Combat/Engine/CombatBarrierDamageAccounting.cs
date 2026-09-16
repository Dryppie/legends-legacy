namespace Services.LL.Combat.Engine;

public sealed record CombatBarrierDamageSlice(int WavesAlreadyEmitted, CombatMechanicEvent Observation);

public sealed record CombatBarrierDamageAccount(CombatMechanicEvent Started, CombatMechanicEvent? Ended,
    double DamageConsumed, double? UnattributedAmount, double Tolerance, bool Reconciled,
    IReadOnlyList<CombatBarrierDamageSlice> Contributions);

/// <summary>
/// Offline accounting only. Non-damage spending appears as a residual, never as inferred attack damage.
/// Pulse counts deduplicate target applications by activation/effect/tick and retain same-tick engine order.
/// </summary>
public static class CombatBarrierDamageAccounting
{
    public static IReadOnlyList<CombatBarrierDamageAccount> Analyze(CombatMechanicTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (trace.SchemaVersion != 2 || !trace.Completed || trace.Truncated || trace.DroppedObservations != 0)
            throw new ArgumentException("Attribution requires a completed, untruncated schema-2 trace.", nameof(trace));

        var accounts = new Dictionary<(string Entity, long? Order, string? Activation), Work>();
        var lastTick = int.MinValue;
        foreach (var e in trace.Events)
        {
            if (e.Tick < lastTick)
                throw new ArgumentException("Trace observations must be in engine order.", nameof(trace));
            lastTick = e.Tick;
            var key = (e.EntityId, e.ApplicationOrder, e.ActivationId);
            if (e.Kind == CombatMechanicEventKind.BarrierStarted)
            {
                if (e.ApplicationOrder is null || !double.IsFinite(e.Value) || e.Value <= 0
                    || !accounts.TryAdd(key, new Work(e)))
                    throw new ArgumentException("Invalid or duplicate barrier start.", nameof(trace));
            }
            else if (e.Kind == CombatMechanicEventKind.LinkedPeriodicApplication)
            {
                var matching = accounts.Values.Where(work => e.ActivationId is not null
                    && e.ActivationId == work.Started.ActivationId
                    && string.Equals(e.DefinitionId, work.Started.LinkedEffectId, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matching.Length == 0 || matching.All(work => work.Ended is not null))
                    throw new ArgumentException("Linked application has no active observed barrier.", nameof(trace));
                foreach (var work in matching)
                    if (work.Ended is null)
                        work.Waves.Add(e.Tick);
            }
            else if (e.Kind is CombatMechanicEventKind.BarrierDamageConsumed
                     or CombatMechanicEventKind.BarrierBroken or CombatMechanicEventKind.BarrierTimedOut)
            {
                if (!accounts.TryGetValue(key, out var work) || work.Ended is not null
                    || !string.Equals(e.DefinitionId, work.Started.DefinitionId, StringComparison.OrdinalIgnoreCase)
                    || !double.IsFinite(e.Value) || e.Value < 0)
                    throw new ArgumentException("Unmatched or invalid barrier observation.", nameof(trace));
                if (e.Kind == CombatMechanicEventKind.BarrierDamageConsumed)
                {
                    if (e.DamageSource is null || string.IsNullOrEmpty(e.DamageSource.AttackerId) || e.Value <= 0)
                        throw new ArgumentException("Consumed damage requires its attacker and positive amount.", nameof(trace));
                    work.Damage += e.Value;
                    work.Contributions.Add(new(work.Waves.Count, e));
                }
                else
                    work.Ended = e;
            }
        }
        return Array.AsReadOnly(accounts.Values.Select(work =>
        {
            // Runtime barrier storage is single precision. This tolerance only handles rounding;
            // it is not a permission to assign a positive unexplained residual to an attacker.
            var tolerance = Math.Max(0.00001, Math.Abs(work.Started.Value) * 0.000001);
            double? residual = work.Ended is null ? null : work.Started.Value - work.Damage
                - (work.Ended.Kind == CombatMechanicEventKind.BarrierTimedOut ? work.Ended.Value : 0);
            return new CombatBarrierDamageAccount(work.Started, work.Ended, work.Damage, residual, tolerance,
                residual is not null && Math.Abs(residual.Value) <= tolerance,
                Array.AsReadOnly(work.Contributions.ToArray()));
        }).ToArray());
    }

    private sealed class Work(CombatMechanicEvent started)
    {
        public CombatMechanicEvent Started { get; } = started;
        public CombatMechanicEvent? Ended { get; set; }
        public double Damage { get; set; }
        public HashSet<int> Waves { get; } = [];
        public List<CombatBarrierDamageSlice> Contributions { get; } = [];
    }
}
