using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPracticalAllocationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-practical-allocation-fixture-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Allocation fixture entered combat.")).Activate();
    private sealed record Input(TowerPracticalRequest Request, TowerPracticalInputs Inputs, TowerBossDiscoveryDefinition Bound);
    public BalanceHarnessPracticalAllocationTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    internal static TowerBossDiscoveryDefinition Template(TowerBossDiscoveryDefinition d) => d with {
        Generation = d.Generation with { Seeds = [] },
        Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, _ => new BossDiscoverySchedule([], [], [], [])) }
    };

    private Input Fixture(bool createOutput = true)
    {
        var bound = I.Definition() with { ExcludedCombatSeeds = [-987] }; var template = Template(bound);
        var prior = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = bound.ExcludedCombatSeeds });
        var source = Path.Combine(root, "template.json"); HarnessJson.WriteNew(source, template);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var q = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, content, source, HarnessJson.FileHash(source), root,
            Path.Combine(root, "run"), new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) }, 60, 32 * 1048576, 5, 128,
            Allocation: new(19, "literal-allocation-fixture", 8, 32, 256));
        var history = TowerRefinementComparisonLaunch.Refresh(root, q.OutputRoot, q.RequiredHistory, [-987], default);
        if (createOutput)
        {
            Directory.CreateDirectory(q.OutputRoot);
            HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "request.json"), q);
        }
        return new(q, new(template, history), bound);
    }

    private TowerPracticalInputs Bind(Input i, Action<string>? boundary = null, Func<string, int, int>? candidate = null)
        => TowerPracticalSearch.AllocateAndRegister(i.Request, i.Inputs, default,
            () => TowerPracticalSearch.CheckEnvelope(i.Request, 0, TowerBulkCampaign.StorageBytes(i.Request.OutputRoot, default)),
            boundary, candidate ?? FixtureHost.AllocationCandidate);

    private int[] Visible(Input i, int[] expected) => TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next-run"),
        i.Request.RequiredHistory, expected, default).Values;

    [Fact]
    public void Completed_handoff_binds_the_exact_definition_and_is_visible_to_the_next_allocator()
    {
        var i = Fixture(); var bound = Bind(i);
        Assert.Equal(HarnessJson.Hash(i.Bound), HarnessJson.Hash(bound.Definition));
        Assert.Equal(i.Request.DefinitionHash, HarnessJson.FileHash(i.Request.DefinitionPath));
        var expected = i.Bound.ExcludedCombatSeeds.Concat(TowerPracticalSearch.Reserved(i.Bound)).Order().ToArray();
        Assert.Equal(expected, Visible(i, expected));
        var receipt = HarnessJson.Read<TowerPracticalAllocationReceipt>(Path.Combine(i.Request.OutputRoot, "allocation.json"));
        Assert.Equal(297, receipt.Candidates); Assert.Equal(0, receipt.Rejections);
        TowerPracticalSearch.VerifyAllocation(i.Request.OutputRoot, i.Request, bound.Definition, default, FixtureHost.AllocationCandidate);
        Assert.Equal(5, i.Request.PriorSeconds); Assert.Equal(128, i.Request.PriorBytes);
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "attempts.jsonl")));
    }

    [Theory]
    [InlineData("allocation-pending", 0, false)]
    [InlineData("allocation-start", 1, false)]
    [InlineData("allocation-candidate", 2, false)]
    [InlineData("allocation-complete", 594, false)]
    [InlineData("before-complete", 594, false)]
    [InlineData("allocation-handoff", 594, true)]
    public async Task Every_interruption_preserves_a_blocker_or_the_complete_union(string boundary, int rows, bool complete)
    {
        var i = Fixture(); var calls = 0;
        Assert.Throws<OperationCanceledException>(() => Bind(i, stage => {
            if (stage == boundary) throw new OperationCanceledException("Synthetic interruption");
        }, (stage, ordinal) => { calls++; return FixtureHost.AllocationCandidate(stage, ordinal); }));
        var journal = Path.Combine(i.Request.OutputRoot, "allocation-journal.jsonl");
        Assert.Equal(rows, File.Exists(journal) ? File.ReadAllLines(journal).Length : 0);
        // The completed handoff also reconstructs each recorded result once.
        Assert.Equal(rows / 2 * (complete ? 2 : 1), calls);
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "attempts.jsonl")));
        if (complete)
        {
            var expected = i.Bound.ExcludedCombatSeeds.Concat(TowerPracticalSearch.Reserved(i.Bound)).Order().ToArray();
            Assert.Equal(expected, Visible(i, expected));
        }
        else Assert.Throws<InvalidDataException>(() => Visible(i, [-987]));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.AllocateAndRun(i.Request));
    }

    [Fact]
    public void Historical_and_current_panel_collisions_are_durable_charged_rejections()
    {
        var i = Fixture();
        int Candidate(string stage, int ordinal) => stage switch {
            "construction" => ordinal == 0 ? -987 : 17,
            "discovery" => ordinal == 0 ? 17 : 100 + ordinal,
            _ => FixtureHost.AllocationCandidate(stage, ordinal)
        };
        var bound = Bind(i, candidate: Candidate);
        Assert.Equal(HarnessJson.Hash(i.Bound), HarnessJson.Hash(bound.Definition));
        var receipt = HarnessJson.Read<TowerPracticalAllocationReceipt>(Path.Combine(i.Request.OutputRoot, "allocation.json"));
        Assert.Equal(299, receipt.Candidates); Assert.Equal(2, receipt.Rejections);
        TowerPracticalSearch.VerifyAllocation(i.Request.OutputRoot, i.Request, bound.Definition, default, Candidate);
    }

    [Fact]
    public void Changed_live_history_prevents_complete_registration_and_keeps_all_accepted_rows()
    {
        var i = Fixture();
        Assert.Throws<InvalidDataException>(() => Bind(i, stage => {
            if (stage == "allocation-complete") File.WriteAllText(i.Request.RequiredHistory.Keys.Single(), "{\"historical\":[-987,-986]}");
        }));
        Assert.Equal(594, File.ReadAllLines(Path.Combine(i.Request.OutputRoot, "allocation-journal.jsonl")).Length);
        Assert.Throws<InvalidDataException>(() => Visible(i, [-987, -986]));
    }

    [Fact]
    public void Changed_template_stops_before_any_derivation_or_pending_reservation()
    {
        var i = Fixture(); File.AppendAllText(i.Request.DefinitionPath, " ");
        Assert.Throws<InvalidDataException>(() => Bind(i, candidate: (_, _) => throw new Exception("Must not derive")));
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "history-input.json")));
    }

    [Theory]
    [InlineData("construction")]
    [InlineData("confirmation")]
    [InlineData("reference")]
    [InlineData("history")]
    [InlineData("confirmation-count")]
    [InlineData("domain")]
    [InlineData("version")]
    public void Ambiguous_templates_and_wrong_contracts_fail_before_allocation(string change)
    {
        var i = Fixture(); var q = i.Request; var d = i.Inputs.Definition;
        switch (change)
        {
            case "construction": d = d with { Generation = d.Generation with { Seeds = [17] } }; break;
            case "confirmation": d = d with { Stages = d.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> { ["fixture"] = new([], [], [301], []) } } }; break;
            case "reference": d = d with { References = d.References.Select(r => r with { Scenario = r.Scenario with { Seeds = [301] } }).ToArray() }; break;
            case "history": d = d with { ExcludedCombatSeeds = [-987, -987] }; break;
            case "confirmation-count": q = q with { Allocation = q.Allocation! with { ConfirmationSamples = 255 } }; break;
            case "domain": q = q with { Allocation = q.Allocation! with { Domain = "bad/domain" } }; break;
            case "version": q = q with { Version = TowerPracticalSearch.Version }; break;
        }
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateAllocationTemplate(q, d));
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "allocation-journal.jsonl")));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("accepted")]
    [InlineData("stage")]
    [InlineData("ordinal")]
    [InlineData("missing-result")]
    [InlineData("extra-start")]
    [InlineData("intent")]
    [InlineData("definition")]
    [InlineData("prior-cost")]
    [InlineData("foreign-output")]
    public void Completed_binding_rejects_semantic_tampering(string change)
    {
        var i = Fixture(); var bound = Bind(i); var q = i.Request;
        var journal = Path.Combine(q.OutputRoot, "allocation-journal.jsonl"); var lines = File.ReadAllLines(journal).ToList();
        if (change is "value" or "accepted" or "stage" or "ordinal")
        {
            var row = JsonNode.Parse(lines[1])!;
            row[change] = change switch { "value" => JsonValue.Create(18), "accepted" => JsonValue.Create(false), "stage" => JsonValue.Create("selection"), _ => JsonValue.Create(1) };
            lines[1] = row.ToJsonString(); File.WriteAllLines(journal, lines);
        }
        else if (change == "missing-result") File.WriteAllLines(journal, lines.Take(lines.Count - 1));
        else if (change == "extra-start") File.AppendAllText(journal, lines[0] + "\n");
        else if (change == "intent")
        {
            var path = Path.Combine(q.OutputRoot, "allocation-intent.json"); var row = JsonNode.Parse(File.ReadAllText(path))!;
            row["requestHash"] = new string('0', 64); File.WriteAllText(path, row.ToJsonString());
        }
        else if (change == "definition") bound = bound with { Definition = bound.Definition with { Id = "other-definition" } };
        else if (change == "prior-cost") q = q with { PriorSeconds = 0, PriorBytes = 0 };
        else if (change == "foreign-output") q = q with { OutputRoot = Path.Combine(root, "other-run") };
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.VerifyAllocation(i.Request.OutputRoot, q, bound.Definition, default, FixtureHost.AllocationCandidate));
    }

    [Fact]
    public async Task Allocation_uses_the_existing_exclusive_registry_lease_and_rejects_competing_owners()
    {
        var i = Fixture(createOutput: false);
        using var lease = TowerCompactBundle.AcquireWriter(Path.Combine(root, "complete-family-allocation"));
        await Assert.ThrowsAsync<IOException>(() => TowerPracticalSearch.AllocateAndRun(i.Request));
        Assert.False(Directory.Exists(i.Request.OutputRoot));
    }

    [Fact]
    public async Task Worker_start_failure_spends_no_values_and_leaves_a_nonreusable_output()
    {
        var i = Fixture(createOutput: false);
        var result = await TowerPracticalSearch.RunWithWorker(i.Request, _ => throw new IOException("Synthetic worker-start failure"));
        Assert.Equal("Invalid", result.ExecutionStatus);
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "history-input.json")));
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "allocation-journal.jsonl")));
        Assert.Equal(new[] { -987 }, Visible(i, [-987]));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.AllocateAndRun(i.Request));
    }

    [Fact]
    public async Task Precancelled_and_wrong_entry_calls_do_not_create_a_claim()
    {
        var i = Fixture(createOutput: false); using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPracticalSearch.AllocateAndRun(i.Request, stop.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => TowerPracticalSearch.AllocationCheck(i.Request, stop.Token));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Run(i.Request));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Check(i.Request));
        Assert.False(Directory.Exists(i.Request.OutputRoot));
        var legacy = i.Request with { Version = TowerPracticalSearch.Version, Allocation = null };
        Assert.DoesNotContain("\"allocation\"", JsonSerializer.Serialize(legacy, HarnessJson.Options));
    }

    [Fact]
    public void Admission_leaves_room_for_the_entire_panel_in_future_history_reads()
    {
        var i = Fixture();
        var tooFull = i.Inputs.Definition with { ExcludedCombatSeeds = Enumerable.Range(0, TowerStudyLimits.HistoricalSeeds - 331 - 297 + 1).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateAllocationTemplate(i.Request, tooFull));
    }

    [Theory]
    [InlineData("tower-practical-search-allocation-check")]
    [InlineData("tower-practical-search-allocate-run")]
    public async Task Public_commands_reject_synthetic_native_bindings_without_deriving_any_value(string command)
    {
        var i = Fixture(createOutput: false); var path = Path.Combine(root, "command-request.json"); HarnessJson.WriteNew(path, i.Request);
        Assert.Equal(2, await BalanceHarness.Program.Main([command, path]));
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "history-input.json")));
        Assert.False(File.Exists(Path.Combine(i.Request.OutputRoot, "allocation-journal.jsonl")));
        Assert.Equal(new[] { -987 }, Visible(i, [-987]));
    }
}
