namespace BalanceHarness;

public static partial class TowerPracticalReservationRecovery
{
    private static (int[] Historical, int[] Reserved) AuditAllocationRegistration(TowerPracticalRecoveryRequest q,
        TowerPracticalRequest source, Dictionary<string, string> files, Action<string, object> matches,
        Action<string, object> partial, CancellationToken ct, Func<string, int, int>? candidate)
    {
        var template = TowerBossDiscovery.Read(Path.Combine(q.StudyRoot, "source-definition.json"));
        TowerPracticalSearch.ValidateAllocationTemplate(source, template);
        matches("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>(), allocationRequestHash = HarnessJson.Hash(source) });
        var allocation = TowerPracticalSearch.ReadRecordedAllocation(q.StudyRoot, source, true, ct, candidate);
        var historical = template.ExcludedCombatSeeds.ToArray(); var reserved = allocation.Reserved;
        string[] writes = ["definition.json", "allocation.json", "seed-ledger.json", "history-input.json"];
        if (allocation.Bound is null)
        {
            Require(writes.All(n => !files.ContainsKey(n + ".pending"))
                && writes.Take(3).All(n => !files.ContainsKey(n)), "Binding artifacts precede a complete allocation journal.");
            return (historical, reserved);
        }

        // Atomic binding writes occur in this exact order. At most one can be
        // interrupted, and no successor exists until its predecessor is durable.
        object[] expected = [allocation.Bound,
            new TowerPracticalAllocationReceipt(TowerPracticalSearch.AllocationVersion, HarnessJson.Hash(source),
                HarnessJson.Hash(allocation.Bound), allocation.Candidates, allocation.Rejections),
            new { reservationState = "Complete", historical, reserved },
            new { reservationState = "Complete", reserved }];
        var predecessorComplete = true;
        for (var i = 0; i < writes.Length; i++)
        {
            var name = writes[i]; var interrupted = files.ContainsKey(name + ".pending");
            // history-input.json must remain Pending; only its replacement can
            // be partial. A durable Complete file needs no exclusion receipt.
            var complete = i < 3 && files.ContainsKey(name);
            Require(!(interrupted && complete) && (predecessorComplete || !interrupted && !complete),
                "Allocated registration artifacts are out of order.");
            if (complete) matches(name, expected[i]);
            if (interrupted) partial(name, expected[i]);
            predecessorComplete = complete;
        }
        return (historical, reserved);
    }
}
