using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerPracticalAllocation(int Master, string Domain, int DiscoverySamples,
    int SelectionSamples, int ConfirmationSamples);
internal sealed record TowerPracticalAllocationIntent(string Version, string RequestHash, string TemplateHash, string HistoricalHash);
internal sealed record TowerPracticalAllocationReceipt(string Version, string RequestHash, string DefinitionHash, int Candidates, int Rejections);

public static partial class TowerPracticalSearch
{
    public const string AllocationVersion = "tower-practical-allocated-search-v1";
    internal const int MaximumAllocationCandidates = 100000;

    private static void ValidateAllocationContract(TowerPracticalRequest q)
    {
        var a = q.Allocation;
        Require(IsDeclaredVersion(q.Version) && a is null || IsAllocatedVersion(q.Version) && a is not null
            && a.Domain is { Length: > 0 and <= 80 } && TowerBenchmark.SafeId(a.Domain)
            && a.DiscoverySamples is >= 1 and <= 100 && a.SelectionSamples is >= 1 and <= 1000
            && a.ConfirmationSamples is >= 256 and <= 1000, "Invalid practical allocation version or sample contract.");
    }

    private static (string Stage, int Count)[] AllocationStages(TowerPracticalAllocation a)
        => [("construction", 1), ("discovery", a.DiscoverySamples), ("selection", a.SelectionSamples), ("confirmation", a.ConfirmationSamples)];

    // These fixed local labels validate shape/cost and native recipes only. They
    // are never allocated, persisted as schedules, or passed to study execution.
    internal static TowerBossDiscoveryDefinition ValidateAllocationTemplate(TowerPracticalRequest q, TowerBossDiscoveryDefinition template)
    {
        ValidateAllocationContract(q); Require(q.Allocation is not null, "Missing allocator contract.");
        ValidateRequestDefinition(q, template);
        var requiredValues = AllocationStages(q.Allocation!).Sum(p => p.Count);
        Require(template.Generation?.Seeds is { Count: 0 } && template.Stages?.Schedules is { Count: 1 }
            && template.Stages.Schedules.Values.All(s => s is not null && s.Discovery is { Count: 0 }
                && s.Selection is { Count: 0 } && s.Confirmation is { Count: 0 } && s.Diagnostics is { Count: 0 } && s.Feedback is null)
            && template.References is not null && template.References.All(r => r.Scenario.Seeds is { Count: 0 })
            && template.ExcludedCombatSeeds is { Count: > 0 } && template.ExcludedCombatSeeds.Count <= TowerStudyLimits.HistoricalSeeds - 331 - requiredValues
            && template.ExcludedCombatSeeds.SequenceEqual(template.ExcludedCombatSeeds.Distinct().Order()),
            "Allocation requires an unscheduled template and a sorted complete historical union.");
        var next = int.MinValue;
        var labels = AllocationStages(q.Allocation!).ToDictionary(p => p.Stage,
            p => Enumerable.Range(0, p.Count).Select(_ => next++).ToArray());
        return BindDefinition(template with { ExcludedCombatSeeds = [] }, labels);
    }

    private static TowerBossDiscoveryDefinition BindDefinition(TowerBossDiscoveryDefinition template, Dictionary<string, int[]> values)
        => Prepare(template with {
            Generation = template.Generation with { Seeds = values["construction"] },
            Stages = template.Stages with { Schedules = template.Stages.Schedules.ToDictionary(p => p.Key,
                _ => new BossDiscoverySchedule(values["discovery"], values["selection"], values["confirmation"], [])) }
        });

    private static int AllocationCandidate(TowerPracticalAllocation a, string stage, int ordinal, string version)
        => StableRandom.Seed(version, a.Domain, a.Master.ToString(CultureInfo.InvariantCulture),
            stage, ordinal.ToString(CultureInfo.InvariantCulture));

    private static TowerPracticalAllocationIntent AllocationIntent(TowerPracticalRequest q, TowerBossDiscoveryDefinition template)
        => new(q.Version, HarnessJson.Hash(q), q.DefinitionHash, HarnessJson.Hash(template.ExcludedCombatSeeds));

    // Runs in the watched worker while its parent holds the registry and output
    // leases. A Pending sentinel blocks every other allocator before derivation.
    internal static TowerPracticalInputs AllocateAndRegister(TowerPracticalRequest q, TowerPracticalInputs inputs,
        CancellationToken ct, Action check, Action<string>? boundary = null, Func<string, int, int>? candidate = null)
    {
        ct.ThrowIfCancellationRequested(); check();
        var template = inputs.Definition; ValidateAllocationTemplate(q, template);
        Require(inputs.History.Values.SequenceEqual(template.ExcludedCombatSeeds), "Changed allocation history.");
        TowerBossStudy.CopyBounded(q.DefinitionPath, P(q, "source-definition.json"),
            q.MaximumBytes - q.PriorBytes - CloseoutBytes - TowerBulkCampaign.StorageBytes(q.OutputRoot, ct), ct);
        Require(HarnessJson.FileHash(P(q, "source-definition.json")) == q.DefinitionHash
            && HarnessJson.Hash(TowerBossDiscovery.Read(P(q, "source-definition.json"))) == HarnessJson.Hash(template), "Changed allocation template.");
        var storage = new TowerCompleteReservation.Storage(q.OutputRoot, q.MaximumBytes - q.PriorBytes - CloseoutBytes);
        storage.Put("history-files.json", inputs.History.Files);
        storage.Put("allocation-intent.json", AllocationIntent(q, template));
        ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>(), allocationRequestHash = HarnessJson.Hash(q) });
        boundary?.Invoke("allocation-pending"); ct.ThrowIfCancellationRequested(); check();
        var used = inputs.History.Values.ToHashSet(); var values = new Dictionary<string, int[]>();
        var candidates = 0; var rejections = 0;
        using (var journal = new FileStream(P(q, "allocation-journal.jsonl"), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
        foreach (var (stage, count) in AllocationStages(q.Allocation!))
        {
            var accepted = new List<int>();
            for (var ordinal = 0; accepted.Count < count; ordinal++)
            {
                ct.ThrowIfCancellationRequested(); check();
                Require(candidates < MaximumAllocationCandidates, "Allocation candidate cap reached; preserve Pending and journal.");
                storage.Append(journal, new("Start", stage, ordinal));
                boundary?.Invoke("allocation-start"); ct.ThrowIfCancellationRequested(); check();
                var value = candidate is null ? AllocationCandidate(q.Allocation!, stage, ordinal, q.Version) : candidate(stage, ordinal);
                var keep = used.Add(value); candidates++;
                // No cancellation boundary between derivation and its durable result.
                storage.Append(journal, new("Candidate", stage, ordinal, value, keep));
                if (keep) accepted.Add(value); else rejections++;
                boundary?.Invoke("allocation-candidate");
            }
            values.Add(stage, accepted.ToArray());
        }
        boundary?.Invoke("allocation-complete"); ct.ThrowIfCancellationRequested(); check();
        var d = BindDefinition(template, values); var reserved = Reserved(d);
        storage.Put("definition.json", d);
        storage.Put("allocation.json", new TowerPracticalAllocationReceipt(q.Version, HarnessJson.Hash(q), HarnessJson.Hash(d), candidates, rejections));
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = inputs.History.Values, reserved });
        TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, ct);
        boundary?.Invoke("before-complete"); ct.ThrowIfCancellationRequested(); check();
        storage.Put("history-input.json", new { reservationState = "Complete", reserved }, true);
        VerifyAllocation(q.OutputRoot, q, d, ct, candidate);
        boundary?.Invoke("allocation-handoff"); ct.ThrowIfCancellationRequested(); check();
        return new(d, inputs.History);
    }

    // Reconstruct only completed recorded derivations. Missing, torn or extended
    // journals cannot authenticate a binding or be resumed by this path.
    internal static void VerifyAllocation(string output, TowerPracticalRequest q, TowerBossDiscoveryDefinition definition,
        CancellationToken ct, Func<string, int, int>? candidate = null)
    {
        var allocation = ReadRecordedAllocation(output, q, false, ct, candidate);
        var bound = allocation.Bound!; var reserved = allocation.Reserved;
        string Pinned(string name) => Path.Combine(output, name);
        Require(HarnessJson.Hash(bound) == HarnessJson.Hash(definition)
            && HarnessJson.Read<TowerPracticalAllocationReceipt>(Pinned("allocation.json"))
                == new TowerPracticalAllocationReceipt(q.Version, HarnessJson.Hash(q), HarnessJson.Hash(bound), allocation.Candidates, allocation.Rejections)
            && HarnessJson.Hash(HarnessJson.Read<JsonElement>(Pinned("history-input.json"))) == HarnessJson.Hash(new { reservationState = "Complete", reserved })
            && HarnessJson.Hash(HarnessJson.Read<JsonElement>(Pinned("seed-ledger.json"))) == HarnessJson.Hash(new {
                reservationState = "Complete", historical = bound.ExcludedCombatSeeds, reserved }), "Changed bound definition or permanent reservation.");
    }

    // A prefix may end only between completed pairs. Never derive the result of
    // an unresolved Start or manufacture the unallocated suffix of a schedule.
    internal static (TowerBossDiscoveryDefinition? Bound, int[] Reserved, int Candidates, int Rejections) ReadRecordedAllocation(
        string output, TowerPracticalRequest q, bool allowPrefix, CancellationToken ct, Func<string, int, int>? candidate = null)
    {
        ct.ThrowIfCancellationRequested(); ValidateRequestContract(q);
        string Pinned(string name) => Path.Combine(output, name);
        var template = TowerBossDiscovery.Read(Pinned("source-definition.json")); ValidateAllocationTemplate(q, template);
        Require(HarnessJson.FileHash(Pinned("source-definition.json")) == q.DefinitionHash
            && HarnessJson.Hash(HarnessJson.Read<TowerPracticalRequest>(Pinned("request.json"))) == HarnessJson.Hash(q)
            && HarnessJson.Read<TowerPracticalAllocationIntent>(Pinned("allocation-intent.json")) == AllocationIntent(q, template),
            "Changed allocation request, template or intent.");
        var values = new Dictionary<string, int[]>(); var used = template.ExcludedCombatSeeds.ToHashSet();
        var candidates = 0; var rejections = 0;
        var journal = Pinned("allocation-journal.jsonl");
        Require(allowPrefix || File.Exists(journal), "Missing allocation journal.");
        if (File.Exists(journal))
        {
            using var bytes = File.OpenRead(journal);
            Require(bytes.Length <= 2L * MaximumAllocationCandidates * 256, "Allocation journal exceeds its bound.");
            if (bytes.Length > 0)
            {
                bytes.Seek(-1, SeekOrigin.End);
                Require(bytes.ReadByte() == '\n', "Interrupted allocation journal row.");
            }
        }
        TowerReservationEvent Row(string line)
        {
            var json = JsonSerializer.Deserialize<JsonElement>(line, HarnessJson.Options);
            var row = json.Deserialize<TowerReservationEvent>(HarnessJson.Options);
            Require(row is not null && HarnessJson.Hash(json) == HarnessJson.Hash(row), "Unrecognized allocation journal row.");
            return row!;
        }
        using var rows = (File.Exists(journal) ? File.ReadLines(journal) : []).GetEnumerator();
        foreach (var (stage, count) in AllocationStages(q.Allocation!))
        {
            var accepted = new List<int>();
            for (var ordinal = 0; accepted.Count < count; ordinal++)
            {
                ct.ThrowIfCancellationRequested();
                if (!rows.MoveNext())
                {
                    Require(allowPrefix, "Missing allocation start.");
                    return (null, used.Except(template.ExcludedCombatSeeds).Order().ToArray(), candidates, rejections);
                }
                Require(candidates < MaximumAllocationCandidates, "Exceeded allocation candidate cap.");
                var start = Row(rows.Current);
                Require(start == new TowerReservationEvent("Start", stage, ordinal) && rows.MoveNext(), "Unresolved or reordered allocation start.");
                var result = Row(rows.Current);
                Require(result is { Kind: "Candidate", Value: not null, Accepted: not null }
                    && result.Stage == stage && result.Ordinal == ordinal, "Invalid completed allocation result.");
                var expected = candidate is null ? AllocationCandidate(q.Allocation!, stage, ordinal, q.Version) : candidate(stage, ordinal);
                Require(result!.Value == expected, "Changed recorded allocation candidate.");
                var keep = used.Add(expected); candidates++;
                Require(result.Accepted == keep, "Changed historical/duplicate allocation rejection.");
                if (keep) accepted.Add(expected); else rejections++;
            }
            values.Add(stage, accepted.ToArray());
        }
        Require(!rows.MoveNext(), "Unaccounted allocation attempts.");
        var bound = BindDefinition(template, values); var reserved = Reserved(bound);
        return (bound, reserved, candidates, rejections);
    }

    public static Task<TowerPracticalResult> AllocateAndRun(TowerPracticalRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); Require(IsAllocatedVersion(request.Version), "Use an allocated-search request.");
        return RunWithWorker(request, NativeWorker, token);
    }

    public static object AllocationCheck(TowerPracticalRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); Require(IsAllocatedVersion(request.Version), "Use an allocated-search request.");
        return CheckCore(request, token);
    }
}
