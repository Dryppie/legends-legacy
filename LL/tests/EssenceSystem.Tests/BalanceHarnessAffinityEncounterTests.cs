using System.Diagnostics;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityEncounterTests
{
    private sealed record CoverageCase(int Floor, string Reason, IReadOnlyList<string> RequiredSignals);
    private sealed record CoverageDefinition(string Version, string Purpose, int MaximumFights, int MaximumSeconds,
        long MaximumBytes, IReadOnlyList<CoverageCase> Cases);
    private sealed class CoverageFactAttribute : FactAttribute
    {
        public CoverageFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_AFFINITY_COVERAGE_SOURCE")))
                Skip = "Set LL_AFFINITY_COVERAGE_SOURCE, LL_AFFINITY_COVERAGE_PIN and LL_AFFINITY_COVERAGE_OUTPUT for encounter compatibility checks.";
        }
    }

    // The first five members of the archived references are identical. Keep the
    // trailing cell for smaller parties; cycle existing slots for larger parties.
    // Never reorder the Essences within a retained loadout.
    internal static int[] SourceSlots(int originalCount, int targetCount)
    {
        if (originalCount < 1 || targetCount < 1) throw new InvalidDataException("Positive party sizes are required.");
        return Enumerable.Range(0, targetCount).Select(i =>
            targetCount < originalCount ? originalCount - targetCount + i : i % originalCount).ToArray();
    }

    [Theory]
    [InlineData(5, new[] { 5, 6, 7, 8, 9 })]
    [InlineData(10, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
    [InlineData(15, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4 })]
    public void Fixed_projection_preserves_existing_cells_and_loadout_order(int count, int[] expected)
        => Assert.Equal(expected, SourceSlots(10, count));

    [Theory]
    [InlineData(0, 5)] [InlineData(10, 0)] [InlineData(-1, 5)]
    public void Empty_projection_is_rejected(int original, int target)
        => Assert.Throws<InvalidDataException>(() => SourceSlots(original, target));

    private static TowerProposalRacingPlan Project(TowerProposalRacingPlan source, int floor, string executionHash)
    {
        var p = TowerBatchRacing.Copy(source);
        var d = p.Racing.Scope;
        if (d.OwnedCopies is not null) throw new InvalidDataException("This diagnostic projection requires the explicit unlimited-copy source.");
        var inventory = p.DamageAffinityInventory!;
        var size = inventory.Bosses.Single(b => b.FloorNumber == floor).RequiredSlots;
        var indices = SourceSlots(d.RequiredPartySize, size);
        var context = d.Contexts.Single();
        var templates = indices.Select((index, i) => new TowerPartyRecipe(i + 1, context.CharacterTemplates[index].Build with {
            Id = $"tower-discovery-character-{i + 1}", EssenceIds = [], IdentityEssenceIds = null })).ToArray();
        var ids = d.References.Select((r, i) => (r.Id, New: $"coverage-reference-{i + 1}"))
            .ToDictionary(x => x.Id, x => x.New);
        var scenarioId = $"affinity-coverage-floor-{floor:D2}";
        var references = d.References.Select(r => new BossBenchmarkReference(ids[r.Id], context.Id,
            r.Scenario with { Id = scenarioId, FloorNumber = floor, Seeds = [],
                Assumptions = ["Diagnostic compatibility fixture; imported loadouts have no confirmation at this encounter or party size."],
                Party = templates.Select((actor, i) => actor with { Build = actor.Build with {
                    EssenceIds = r.Scenario.Party[indices[i]].Build.EssenceIds.ToArray(),
                    IdentityEssenceIds = Enumerable.Range(1, d.Budget.EssenceSlots).Select(n => $"neutral-identity-slot-{n}").ToArray()
                } }).ToArray() }, "Diagnostic projection of an archived reference; no transferred strength claim.",
            HarnessJson.Hash(new { sourceReference = r, floor, indices }))).ToArray();
        var starts = d.Starts.Select((s, i) => new BossDiscoveryStart($"coverage-start-{i + 1}", ids[s.ReferenceId],
            TowerPartySelection.Choice("diagnostic-reference", references.Single(r => r.Id == ids[s.ReferenceId])
                .Scenario.Party.ToDictionary(a => a.PartySlot, a => a.Build.EssenceIds)))).ToArray();
        d = d with { Id = scenarioId, Budget = d.Budget with { PriorityFloor = floor }, BudgetPurpose = "diagnostic",
            RequiredPartySize = size, Contexts = [new(context.Id, templates)], References = references, Starts = starts,
            Stages = d.Stages with { SelectionPrimaryReferenceId = ids[d.Stages.SelectionPrimaryReferenceId!] }, ExecutionHash = executionHash };
        var racing = p.Racing with { Scope = d, BenchmarkReferenceId = ids[p.Racing.BenchmarkReferenceId],
            Mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(d), inventory) };
        return TowerAffinitySearch.CreatePlan(racing, inventory, p.Policy.CreatedDamageAffinityIds!);
    }

    [CoverageFact]
    public async Task All_floors_prepare_and_representative_encounters_complete_the_supported_native_search()
    {
        var source = Environment.GetEnvironmentVariable("LL_AFFINITY_COVERAGE_SOURCE")!;
        var pin = Environment.GetEnvironmentVariable("LL_AFFINITY_COVERAGE_PIN");
        var output = Environment.GetEnvironmentVariable("LL_AFFINITY_COVERAGE_OUTPUT")
            ?? throw new InvalidDataException("Choose a new coverage output directory.");
        TowerProposalStudy.Unlinked(source); TowerProposalStudy.Unlinked(output);
        Assert.False(Path.Exists(output));
        Assert.False(Path.GetFullPath(output).StartsWith(Path.GetFullPath(source) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(pin, HarnessJson.FileHash(Path.Combine(source, "files.json")));
        var fixture = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "../../../tools/BalanceHarness/Fixtures/tower-affinity-encounter-coverage.json"));
        var definition = TowerContractJson.Read<CoverageDefinition>(fixture);
        Assert.Equal("affinity-encounter-coverage-v1", definition.Version);
        Assert.Equal(new[] { 3, 5, 7, 8, 10, 15 }, definition.Cases.Select(c => c.Floor));
        Assert.Equal((3168, 600, 1610612736L), (definition.MaximumFights, definition.MaximumSeconds, definition.MaximumBytes));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(definition.MaximumSeconds));
        var token = deadline.Token;
        Assert.Equal(528, TowerLoadoutArchive.Verify(source, token).Count);
        var original = HarnessJson.Read<TowerProposalRacingPlan>(Path.Combine(source, "racing/plan.json"));
        TowerAffinitySearch.Validate(original);
        var originalHash = HarnessJson.Hash(original);
        var captured = HarnessJson.Read<LoadoutScope>(Path.Combine(source, "scope.json"));
        var execution = ExecutionIdentity.Current();
        var executionHash = HarnessJson.Hash(execution);
        var plans = Enumerable.Range(1, 15).ToDictionary(f => f, f => Project(original, f, executionHash));
        Assert.Equal(originalHash, HarnessJson.Hash(original));
        foreach (var item in definition.Cases)
            Assert.Empty(item.RequiredSignals.Except(original.DamageAffinityInventory!.Bosses.Single(b => b.FloorNumber == item.Floor).Signals));
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
        var preflight = new List<object>();
        var contentRoot = Path.Combine(source, "content");
        var runner = new TowerBattleRunner(contentRoot, new OfflineContent(contentRoot, captured.Settings.Threat));
        // Prepare all 45 projected references and freeze every plan before any combat.
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preflight cannot fight.")).Activate())
            foreach (var (floor, plan) in plans)
            {
                token.ThrowIfCancellationRequested();
                Assert.Equal(HarnessJson.Hash(original.Policy), HarnessJson.Hash(plan.Policy));
                Assert.Equal(HarnessJson.Hash(original.Racing.Panels), HarnessJson.Hash(plan.Racing.Panels));
                Assert.Equal(original.Racing.RootSeed, plan.Racing.RootSeed);
                Assert.Equal(original.Racing.Scope.ExcludedCombatSeeds, plan.Racing.Scope.ExcludedCombatSeeds);
                Assert.Equal(floor, plan.Racing.Mechanics.Floor);
                Assert.Equal(3, plan.Racing.Scope.Starts.Select(s => s.Party.Id).Distinct().Count());
                foreach (var reference in plan.Racing.Scope.References)
                {
                    var scenario = reference.Scenario with { Seeds = [plan.Racing.Panels[0].Seeds[0]] };
                    var input = runner.CreateInput(scenario, scenario.Seeds[0], captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                    Assert.Equal(plan.Racing.Scope.RequiredPartySize, input.Party.Count);
                    _ = await runner.PrepareAsync(input, token);
                }
                HarnessJson.WriteNew(Path.Combine(output, $"plan-floor-{floor:D2}.json"), plan);
                preflight.Add(new { floor, partySize = plan.Racing.Scope.RequiredPartySize, preparedReferences = 3, planHash = HarnessJson.Hash(plan) });
            }
        HarnessJson.WriteNew(Path.Combine(output, "preflight.json"), preflight);
        var rows = new List<object>(); var started = 0; var completed = 0; var status = "Incomplete";
        try
        {
            foreach (var item in definition.Cases)
            {
                token.ThrowIfCancellationRequested();
                var plan = plans[item.Floor];
                var boss = plan.DamageAffinityInventory!.Bosses.Single(b => b.FloorNumber == item.Floor);
                var target = Path.Combine(output, $"floor-{item.Floor:D2}");
                Directory.CreateDirectory(target);
                var hashes = TowerBundle.CopyContent(contentRoot, Path.Combine(target, "content"), token);
                Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(hashes));
                var scope = captured with { Algorithm = TowerProposalRacingNative.ArchiveAlgorithm(plan), Execution = execution };
                HarnessJson.WriteNew(Path.Combine(target, "scope.json"), scope);
                Directory.CreateDirectory(Path.Combine(target, "recipes")); Directory.CreateDirectory(Path.Combine(target, "battles"));
                var archive = new TowerLoadoutArchive(target, scope, 528);
                var attempts = 0; var caseCompleted = 0;
                var trace = new TowerPerformanceTrace(done => { if (done) completed++; else { started++; Assert.True(started <= definition.MaximumFights); } });
                var clock = Stopwatch.StartNew();
                TowerAffinitySearchSummary summary;
                using (trace.Activate())
                    summary = await TowerAffinitySearch.RunAsync(plan, archive, 96 * 1048576, token.ThrowIfCancellationRequested, token,
                        done => { if (done) { caseCompleted++; if (caseCompleted % 64 == 0) CheckSize(); } else attempts++; });
                clock.Stop();
                Assert.Equal((528, 528, 528), (attempts, caseCompleted, archive.Trials.Count));
                Assert.Contains(summary.Status, new[] { "BenchmarkRetained", "ChallengerNeedsConfirmation" });
                var files = Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                    .ToDictionary(p => Path.GetRelativePath(target, p).Replace('\\', '/'), HarnessJson.FileHash);
                HarnessJson.WriteNew(Path.Combine(target, "files.json"), files);
                var archivePin = HarnessJson.FileHash(Path.Combine(target, "files.json"));
                // Native reconstruction must authenticate every archived trial; no additional fights.
                using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification cannot fight.")).Activate())
                {
                    var rebuilt = await TowerProposalRacingNative.VerifyAsync(target, archivePin, token);
                    Assert.Equal(summary, TowerAffinitySearch.Summarize(plan, rebuilt));
                }
                var row = new { floor = item.Floor, boss.GuardianName, partySize = plan.Racing.Scope.RequiredPartySize,
                    item.Reason, summary, seconds = clock.Elapsed.TotalSeconds, archivePin, verifiedTrials = archive.Trials.Count,
                    provisionalOnly = true, confirmedTeams = 0, timings = trace.Snapshot() };
                rows.Add(row); HarnessJson.WriteNew(Path.Combine(output, $"result-floor-{item.Floor:D2}.json"), row);
                CheckSize();
            }
            Assert.Equal((definition.MaximumFights, definition.MaximumFights), (started, completed));
            Assert.Equal(pin, HarnessJson.FileHash(Path.Combine(source, "files.json")));
            status = "CompatibilityVerified";
        }
        finally
        {
            HarnessJson.WriteNew(Path.Combine(output, "coverage.json"), new { status, definition.Purpose,
                sourceManifestSha256 = pin, execution, preparedFloors = plans.Count, started, completed, newSeeds = 0,
                confirmedTeams = 0, results = rows });
        }
        void CheckSize()
        {
            token.ThrowIfCancellationRequested();
            Assert.True(Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length) < definition.MaximumBytes);
        }
    }
}
