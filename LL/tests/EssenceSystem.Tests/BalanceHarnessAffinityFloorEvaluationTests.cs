using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

// Opt-in scientific operation. The normal test run never allocates or fights.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityFloorEvaluationTests
{
    private static string VersionFor(int floor) => floor switch {
        >= 1 and <= 9 or >= 12 and <= 15 => $"affinity-floor{floor}-baseline-evaluation-v1",
        _ => throw new InvalidDataException("This bounded evaluation supports floors 1–9 and 12–15.") };
    private const int SearchValues = 109; // root + 8/8/8/8/16/60
    private const int HeldoutSamples = 128;
    private const int MaximumFights = 528 + 5 * HeldoutSamples;
    private const long MaximumBytes = 1073741824;
    private sealed record EvaluationRequest(string Version, string Output, string Registry, string Runtime,
        string Plan, string PlanHash, string Handoff, string HandoffHash, string SourceScope, string SourceScopeHash,
        string HistoryLedger, string HistoryLedgerHash, IReadOnlyDictionary<string, string> RequiredHistory,
        IReadOnlyDictionary<string, string> Recoveries, IReadOnlyDictionary<string, string> RecoveryHashes, int Master,
        string? Screen = null, string? ScreenHash = null, int Floor = 3, int BenchmarkReference = 1,
        [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        string? ContentRoot = null);

    private sealed class EvaluationFactAttribute : FactAttribute
    {
        public EvaluationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_AFFINITY_FLOOR_EVALUATION")))
                Skip = "Set LL_AFFINITY_FLOOR_EVALUATION to a pinned one-attempt request; fresh allocation and 1,168 fights.";
        }
    }

    internal static IReadOnlyList<TowerRacingPanel> Panels(IReadOnlyList<int> values)
    {
        if (values.Count != SearchValues || values.Distinct().Count() != SearchValues)
            throw new InvalidDataException("One root and 108 distinct search seeds required.");
        return TowerBenchmarkValidation.PanelRoles.Select((role, index) => new TowerRacingPanel(role,
            values.Skip(index < 4 ? 1 + 8 * index : index == 4 ? 33 : 49)
                .Take(index < 4 ? 8 : index == 4 ? 16 : 60).ToArray())).ToArray();
    }

    internal static PartyChoice Composition(TowerScenario recipe) => TowerPartySelection.Choice($"supplied-floor{recipe.FloorNumber}",
        TowerCompositionSearch.CanonicalBuilds(recipe.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)));

    internal static TowerProposalRacingPlan Project(TowerProposalRacingPlan source, TowerScenario[] recipes,
        TowerBossInventoryReport inventory, LoadoutScope captured, IReadOnlyList<int> search,
        IReadOnlyList<int> history, IReadOnlyList<int> heldout, TowerSearchBudget budget, int partySize, int benchmarkReference)
    {
        var d = source.Racing.Scope;
        var context = d.Contexts.Single().Id;
        var floor = budget.PriorityFloor;
        _ = VersionFor(floor);
        Assert.InRange(benchmarkReference, 1, 3);
        Assert.Equal(3, recipes.Length);
        Assert.All(recipes, s => { Assert.Equal(floor, s.FloorNumber); Assert.Equal(partySize, s.Party.Count);
            Assert.Empty(s.Seeds); TowerBossDiscovery.ValidateEquipment(s.Party, budget, partySize); });
        Assert.Equal(inventory.Bosses.Single(b => b.FloorNumber == floor).RequiredSlots, partySize);
        Assert.Single(recipes.Select(s => TowerBossDiscovery.EquipmentBudgetHash(s.Party)).Distinct());
        var templates = recipes[0].Party.Select(p => p with { Build = p.Build with {
            Id = $"tower-discovery-character-{p.PartySlot}", EssenceIds = [], IdentityEssenceIds = null } }).ToArray();
        var starts = recipes.Select((s, i) => new BossDiscoveryStart($"floor{floor}-start-{i + 1}",
            $"floor{floor}-reference-{i + 1}", Composition(s))).ToArray();
        Assert.Equal(3, starts.Select(s => s.Party.Id).Distinct().Count());
        var legacy = d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
            .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? [])).ToHashSet();
        // Legacy labels are declared solely to satisfy the existing scope contract;
        // they are forbidden by the racing validator and never executed here.
        d = d with { Id = $"affinity-floor{floor}-baseline", Budget = budget, BudgetPurpose = "diagnostic",
            RequiredPartySize = partySize, StartsAt = recipes[0].StartsAt, Contexts = [new(context, templates)],
            Generation = d.Generation with { Seeds = [search[0]] },
            Stages = d.Stages with { SelectionPrimaryReferenceId = starts[benchmarkReference - 1].ReferenceId },
            ExcludedCombatSeeds = history.Except(legacy).Concat(heldout).Distinct().Order().ToArray(),
            Starts = starts, ContentHashes = captured.ContentHashes, SettingsHash = HarnessJson.Hash(captured.Settings),
            ExecutionHash = HarnessJson.Hash(captured.Execution) };
        d = d with { References = starts.Select((s, i) => new BossBenchmarkReference(s.ReferenceId, context,
            TowerBossDiscovery.Scenario(d, context, s.Party, []),
            $"Preselected floor-{floor} composition; canonical search encoding; no transferred quality claim.",
            HarnessJson.Hash(recipes[i]))).ToArray() };
        var racing = source.Racing with { Scope = d, BenchmarkReferenceId = starts[benchmarkReference - 1].ReferenceId,
            RootSeed = search[0], Panels = Panels(search),
            Mechanics = TowerBossPartyGenerator.FromInventory(TowerBossDiscovery.CopyGenerationInputs(d), inventory) };
        var plan = TowerAffinitySearch.CreatePlan(racing, inventory, source.Policy.CreatedDamageAffinityIds!);
        Assert.Equal(HarnessJson.Hash(source.Policy), HarnessJson.Hash(plan.Policy));
        return plan;
    }

    [Fact]
    public void Existing_requests_omit_the_new_optional_content_source()
    {
        var request = new EvaluationRequest("version", "output", "registry", "runtime", "plan", "planHash",
            "handoff", "handoffHash", "scope", "scopeHash", "history", "historyHash",
            new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), 1);
        Assert.False(JsonSerializer.SerializeToElement(request, HarnessJson.Options).TryGetProperty("contentRoot", out _));
        Assert.Equal("captured", JsonSerializer.SerializeToElement(request with { ContentRoot = "captured" }, HarnessJson.Options)
            .GetProperty("contentRoot").GetString());
    }

    [Fact]
    public void Fixed_panels_use_every_search_value_once_and_exclude_the_root()
    {
        var values = Enumerable.Range(1000, SearchValues).ToArray();
        var panels = Panels(values);
        Assert.Equal(new[] { 8, 8, 8, 8, 16, 60 }, panels.Select(p => p.Seeds.Count));
        Assert.Equal(values.Skip(1), panels.SelectMany(p => p.Seeds));
        Assert.Throws<InvalidDataException>(() => Panels(values.Skip(1).ToArray()));
        Assert.Throws<InvalidDataException>(() => Panels(Enumerable.Repeat(1, SearchValues).ToArray()));
    }

    [Fact]
    public void Search_encoding_deduplicates_permutations_without_mutating_saved_recipes()
    {
        var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(TestContentPaths.FindApiRoot(),
            "../../../tools/BalanceHarness/Fixtures/tower-floor-1.json"));
        var original = HarnessJson.Hash(recipe);
        var reversed = recipe with { Party = recipe.Party.Select(p => p with {
            Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() };
        Assert.Equal(Composition(recipe).Id, Composition(reversed).Id);
        Assert.Equal(original, HarnessJson.Hash(recipe));
    }

    [Theory]
    [InlineData(1, 4, 5, 1)] [InlineData(2, 4, 5, 1)] [InlineData(3, 4, 5, 1)]
    [InlineData(4, 4, 5, 1)] [InlineData(5, 5, 10, 1)] [InlineData(6, 5, 5, 1)]
    [InlineData(7, 5, 5, 1)] [InlineData(8, 5, 10, 1)] [InlineData(9, 5, 10, 1)]
    [InlineData(12, 7, 10, 1)] [InlineData(13, 7, 10, 1)]
    [InlineData(14, 7, 10, 1)] [InlineData(15, 8, 15, 1)] [InlineData(15, 10, 15, 2)]
    public async Task Floor_projection_prepares_the_complete_party_and_uses_the_explicit_benchmark(int floor, int slots, int partySize, int benchmarkReference)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Projection cannot fight.")).Activate();
        var root = TestContentPaths.FindApiRoot();
        var catalogs = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var budget = TowerPartyProgression.Budget(slots) with { PriorityFloor = floor };
        var baseline = TowerPartyProgression.Scenarios(root, catalogs, budget).Single(s => s.FloorNumber == floor);
        var controls = HarnessJson.Read<TowerWholePartyHistory>(Path.Combine(catalogs, TowerWholeParty.Fixture));
        var recipes = controls.Parties[slots].Take(3).Select(p => TowerPartySelection.Apply(baseline, p.Builds, [])).ToArray();
        var settings = TowerBundle.ReadSettings(root);
        var inventory = TowerBossInventory.CreateForTower(root, settings);
        var source = BalanceHarnessAffinitySearchTests.Baseline();
        source = source with { Racing = source.Racing with { Scope = source.Racing.Scope with {
            AllowedEssences = inventory.Essences.Select(e => new BossDiscoveryEssence(e.Id, e.SourceMonsterId)).ToArray(),
            OwnedCopies = null } }, Policy = TowerProposalPolicies.BenchmarkAffinityCreation(
                TowerDamageSourceAffinities.Create(inventory).Affinities.Select(a => a.Id)) };
        var captured = new LoadoutScope("projection-test", settings, ExecutionIdentity.Current(),
            TowerBundle.Files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(root, "Data", f))));
        var originalSource = HarnessJson.Hash(source); var originalRecipes = HarnessJson.Hash(recipes);
        var plan = Project(source, recipes, inventory, captured, Enumerable.Range(1000, SearchValues).ToArray(), [], [], budget, partySize, benchmarkReference);
        Assert.Equal($"floor{floor}-reference-{benchmarkReference}", plan.Racing.BenchmarkReferenceId);
        Assert.Equal(plan.Racing.BenchmarkReferenceId, plan.Racing.Scope.Stages.SelectionPrimaryReferenceId);
        Assert.Equal(Composition(recipes[benchmarkReference - 1]).Id, plan.Racing.Scope.Starts[benchmarkReference - 1].Party.Id);
        Assert.Equal(budget, plan.Racing.Scope.Budget);
        Assert.All(plan.Racing.Scope.Starts, s => {
            Assert.Equal(partySize, s.Party.Builds.Count);
            Assert.All(s.Party.Builds.Values, ids => Assert.Equal(slots, ids.Count)); });
        var runner = new TowerBattleRunner(root, OfflineContent.ForTower(root, settings));
        foreach (var reference in plan.Racing.Scope.References)
        {
            Assert.Equal(TowerBossDiscovery.EquipmentBudgetHash(recipes[0].Party),
                TowerBossDiscovery.EquipmentBudgetHash(reference.Scenario.Party));
            var scenario = reference.Scenario with { Seeds = [1000] };
            _ = await runner.PrepareAsync(runner.CreateInput(scenario, 1000, settings.Threat, settings.CheckpointIntervalTicks));
        }
        Assert.Equal(originalSource, HarnessJson.Hash(source));
        Assert.Equal(originalRecipes, HarnessJson.Hash(recipes));
    }

    [EvaluationFact]
    public async Task One_fresh_floor_search_and_fixed_finalist_panel_complete_and_reconstruct()
    {
        var requestPath = Environment.GetEnvironmentVariable("LL_AFFINITY_FLOOR_EVALUATION")!;
        var q = TowerContractJson.Read<EvaluationRequest>(requestPath);
        var version = VersionFor(q.Floor);
        Assert.Equal(version, q.Version);
        Assert.InRange(q.BenchmarkReference, 1, 3);
        if (q.Floor is 13 or 15) Assert.NotNull(q.Screen);
        if (q.Floor == 13)
        {
            Assert.NotNull(q.ContentRoot);
            Assert.Equal(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(q.Screen!)!, "content")), Path.GetFullPath(q.ContentRoot));
            TowerProposalStudy.Unlinked(q.ContentRoot);
        }
        else Assert.Null(q.ContentRoot);
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        foreach (var path in new[] { requestPath, q.Output, q.Runtime, q.Plan, q.Handoff, q.SourceScope, q.HistoryLedger })
            TowerProposalStudy.Unlinked(path);
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.PlanHash, HarnessJson.FileHash(q.Plan));
        Assert.Equal(q.HandoffHash, HarnessJson.FileHash(q.Handoff));
        Assert.Equal(q.SourceScopeHash, HarnessJson.FileHash(q.SourceScope));
        Assert.Equal(q.HistoryLedgerHash, HarnessJson.FileHash(q.HistoryLedger));
        foreach (var pin in q.RecoveryHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key));
        var source = HarnessJson.Read<TowerProposalRacingPlan>(q.Plan);
        // The pinned archive supplies the policy and schedule template. Its old
        // ability graph must not be interpreted as today's ability schema. Project
        // builds and fully validates a new inventory before any seed allocation.
        Assert.Equal(TowerProposalPolicies.BenchmarkValidationRacingVersion, source.Version);
        Assert.Equal(TowerBenchmarkValidation.Version, source.SelectionPolicyVersion);
        Assert.NotEmpty(source.Policy.CreatedDamageAffinityIds!);
        Assert.Equal(HarnessJson.Hash(TowerProposalPolicies.BenchmarkAffinityCreation(source.Policy.CreatedDamageAffinityIds!)),
            HarnessJson.Hash(source.Policy));
        var handoff = HarnessJson.Read<JsonElement>(q.Handoff);
        var floor = handoff.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("case").GetProperty("floor").GetInt32() == q.Floor);
        var floorCase = floor.GetProperty("case");
        var budget = floorCase.GetProperty("budget").Deserialize<TowerSearchBudget>(HarnessJson.Options)!;
        var partySize = floorCase.GetProperty("partySize").GetInt32();
        var sourceIds = floorCase.GetProperty("referenceIds").EnumerateArray().Select(id => id.GetString()!).ToArray();
        var recipes = floor.GetProperty("scenarios").Deserialize<TowerScenario[]>(HarnessJson.Options)!;
        var originalScope = HarnessJson.Read<LoadoutScope>(q.SourceScope);
        var historical = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(q.HistoryLedger));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(840));
        var token = stop.Token;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Registry, "complete-family-allocation"));
        using var outputLease = TowerCompactBundle.AcquireWriter(q.Output);
        Assert.False(Path.Exists(q.Output));
        Directory.CreateDirectory(q.Output);
        var started = System.Diagnostics.Stopwatch.StartNew();
        var attempts = 0; var completed = 0; var successful = false;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check()
        {
            token.ThrowIfCancellationRequested();
            Assert.True(attempts <= MaximumFights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < MaximumBytes);
        }
        void Attempt(bool done)
        {
            if (done) completed++;
            else
            {
                Check(); Assert.True(++attempts <= MaximumFights);
                TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"),
                    JsonSerializer.Serialize(new { attempt = attempts }, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }) + "\n");
            }
        }
        try
        {
            Save("request.json", q);
            Save("protocol.json", new { version, supportedProfile = TowerAffinitySearch.Profile,
                floor = q.Floor, benchmarkReference = q.BenchmarkReference, benchmarkSourceId = sourceIds[q.BenchmarkReference - 1], budget, partySize,
                roots = 1, searchFights = 528, heldoutSamples = HeldoutSamples, maximumFights = MaximumFights,
                maximumSeconds = 840, maximumBytes = MaximumBytes, retries = 0,
                interpretation = "One exploratory root; all five nominees frozen before heldout; no team confirmation or default change.",
                order = "Fixed ordinal Essence ID encoding required by unchanged composition search; original handoff retained." });
            Save("source-recipes.json", recipes);
            var history = TowerRefinementComparisonLaunch.Refresh(q.Registry, q.Output, q.RequiredHistory, historical, token, q.Recoveries);
            Save("history-files.json", history.Files);
            var contentSource = q.ContentRoot ?? TestContentPaths.FindApiRoot();
            var hashes = TowerBundle.CopyContent(contentSource, Path.Combine(q.Output, "content"), token);
            var execution = ExecutionIdentity.Current();
            var captured = originalScope with { Settings = TowerBundle.ReadSettings(contentSource),
                Execution = execution, ContentHashes = hashes, ReportStorage = "gzip-json-v1" };
            if (q.Floor == 13)
            {
                Assert.Equal(HarnessJson.Hash(originalScope.ContentHashes), HarnessJson.Hash(hashes));
                Assert.Equal(HarnessJson.Hash(originalScope.Settings), HarnessJson.Hash(captured.Settings));
            }
            if (q.Screen is not null)
            {
                Assert.Equal(q.ScreenHash, HarnessJson.FileHash(q.Screen));
                var screen = HarnessJson.Read<JsonElement>(q.Screen);
                Assert.Equal("ScreenComplete", screen.GetProperty("status").GetString());
                var rows = screen.GetProperty("rows").EnumerateArray().ToArray();
                if (q.Floor == 13)
                {
                    Assert.Equal("tower-floor13-geared-reference-screen-v1", screen.GetProperty("version").GetString());
                    Assert.True(screen.GetProperty("eligible").GetBoolean());
                    Assert.Equal(q.BenchmarkReference, screen.GetProperty("benchmarkReference").GetInt32());
                    Assert.Equal(6, rows.Length);
                    Assert.All(rows, r => Assert.InRange(r.GetProperty("wins").GetInt32(), 0, 28));
                }
                var screened = rows.Single(r => (q.Floor == 13 ? r.GetProperty("form").GetString() == "projected" : r.GetProperty("floor").GetInt32() == q.Floor)
                    && r.GetProperty("reference").GetInt32() == q.BenchmarkReference);
                Assert.Equal(sourceIds[q.BenchmarkReference - 1], screened.GetProperty("sourceId").GetString());
                Assert.InRange(screened.GetProperty("wins").GetInt32(), 4, 28);
                Assert.Equal(HarnessJson.Hash(execution), HarnessJson.Hash(screen.GetProperty("execution").Deserialize<ExecutionIdentity>(HarnessJson.Options)));
                var freezePath = Path.Combine(Path.GetDirectoryName(q.Screen)!, "freeze.json");
                Assert.Equal(screen.GetProperty("freezeSha256").GetString(), HarnessJson.FileHash(freezePath));
                var freeze = HarnessJson.Read<JsonElement>(freezePath);
                // The new floor-15 benchmark is explicitly nominated from a completed
                // diagnostic screen; the historical screen decision is never rewritten.
                for (var i = 0; i < recipes.Length; i++)
                {
                    var name = $"scenarios/floor-{q.Floor:D2}-reference-{i + 1}.json";
                    var path = Path.Combine(Path.GetDirectoryName(q.Screen)!, name);
                    Assert.Equal(freeze.GetProperty("captured").GetProperty(name).GetString(), HarnessJson.FileHash(path));
                    var screenedRecipe = HarnessJson.Read<TowerScenario>(path);
                    Assert.Equal(q.Floor, screenedRecipe.FloorNumber);
                    Assert.Equal(recipes[i].StartsAt, screenedRecipe.StartsAt);
                    Assert.Equal(HarnessJson.Hash(recipes[i].Party), HarnessJson.Hash(screenedRecipe.Party));
                }
                Assert.Equal(HarnessJson.Hash(captured.Settings.Balance), HarnessJson.Hash(freeze.GetProperty("balance").Deserialize<TowerBalanceSelection>(HarnessJson.Options)));
                var contentHashes = freeze.GetProperty("captured").EnumerateObject().Where(p => p.Name.StartsWith("content/Data/", StringComparison.Ordinal))
                    .ToDictionary(p => p.Name["content/Data/".Length..], p => p.Value.GetString()!);
                Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(new SortedDictionary<string, string>(contentHashes, StringComparer.Ordinal)));
                Save("reference-screen.json", screen);
            }
            Save("captured-scope.json", captured);
            foreach (var (name, hash) in execution.AssemblyHashes)
                Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(q.Runtime, name + ".dll")));
            foreach (var file in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, file);
                if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, false);
            }
            Save("runtime-files.json", Inventory(Path.Combine(q.Output, "executable")));
            var inventory = TowerBossInventory.CreateForTower(Path.Combine(q.Output, "content"), captured.Settings);
            Save("inventory.json", inventory);
            // Preparation uses literal labels solely for validation, behind a guard
            // that forbids battles. Production allocation begins only afterward.
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preflight cannot fight.")).Activate())
            {
                var preview = Project(source, recipes, inventory, captured, Enumerable.Range(1000, SearchValues).ToArray(), [], [], budget, partySize, q.BenchmarkReference);
                var runner = new TowerBattleRunner(Path.Combine(q.Output, "content"), OfflineContent.ForTower(Path.Combine(q.Output, "content"), captured.Settings));
                foreach (var reference in preview.Racing.Scope.References)
                {
                    var scenario = reference.Scenario with { Seeds = [1000] };
                    var input = runner.CreateInput(scenario, 1000, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                    _ = await runner.PrepareAsync(input, token);
                }
                Save("prepared-references.json", preview.Racing.Scope.References);
                Save("preflight.json", new { status = "PreparedNoFights", references = 3, executionHash = HarnessJson.Hash(execution), historyCount = history.Values.Length });
            }
            Check();
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            var allocator = new TowerCompleteAllocatorPlan("sha256-us-int32le-reject-v1", version, q.Master, 1, 236, 100000);
            var reservation = TowerCompleteReservation.Reserve(q.Output, allocator, history.Values, MaximumBytes, token);
            var values = reservation.Seeds.First.Concat(reservation.Seeds.Second).ToArray();
            Assert.Equal(237, values.Length);
            var searchValues = values.Take(SearchValues).ToArray();
            var heldout = values.Skip(SearchValues).ToArray();
            var plan = Project(source, recipes, inventory, captured, searchValues, history.Values, heldout, budget, partySize, q.BenchmarkReference);
            Save("plan.json", plan); Save("heldout-seeds.json", heldout);
            TowerCompleteReservation.Verify(q.Output, reservation.Seeds, allocator, token);
            var searchRoot = Path.Combine(q.Output, "search");
            var archive = Archive(searchRoot, captured with { Algorithm = TowerProposalRacingNative.ArchiveAlgorithm(plan) }, 528, q.Output, token);
            var summary = await TowerAffinitySearch.RunAsync(plan, archive, 96 * 1048576, Check, token, Attempt);
            Assert.Equal(528, completed); Assert.Equal(528, archive.Trials.Count);
            Save("search-summary.json", summary);
            var report = HarnessJson.Read<TowerProposalRacingReport>(Path.Combine(searchRoot, "racing/search.json"));
            Assert.Equal("Complete", report.Evaluation.Status);
            var nomination = report.Evaluation.Panels.Single(p => p.Freeze.Role == "nomination");
            var members = nomination.Freeze.Parties.ToArray();
            Assert.Equal(5, members.Length);
            var referenceIds = plan.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
            Assert.Equal(2, members.Count(p => !referenceIds.Contains(p.Id)));
            Save("heldout-freeze.json", members.Select(p => new { party = p,
                role = referenceIds.Contains(p.Id) ? "existing-reference" : "generated-finalist",
                scenario = TowerBossDiscovery.Scenario(plan.Racing.Scope, plan.Racing.Scope.Contexts[0].Id, p, heldout) }).ToArray());
            var heldoutRoot = Path.Combine(q.Output, "heldout");
            var heldoutArchive = Archive(heldoutRoot, captured with { Algorithm = version + "/heldout" }, 640, q.Output, token);
            var outcomes = new Dictionary<string, List<TowerBattleReport>>();
            foreach (var party in members)
            {
                var scenario = TowerBossDiscovery.Scenario(plan.Racing.Scope, plan.Racing.Scope.Contexts[0].Id, party, heldout);
                var reports = new List<TowerBattleReport>(); outcomes.Add(party.Id, reports);
                foreach (var seed in heldout)
                {
                    Attempt(false);
                    var trial = await heldoutArchive.EvaluateAsync(version, party.Id, scenario, seed, token);
                    Attempt(true); reports.Add(trial.Report);
                }
            }
            Assert.Equal(MaximumFights, attempts); Assert.Equal(attempts, completed);
            Seal(searchRoot); Seal(heldoutRoot);
            var rebuilt = await TowerProposalRacingNative.VerifyAsync(searchRoot, HarnessJson.FileHash(Path.Combine(searchRoot, "files.json")), token);
            Assert.Equal(summary, TowerAffinitySearch.Summarize(plan, rebuilt));
            var heldoutTrials = TowerLoadoutArchive.Verify(heldoutRoot, token);
            Assert.Equal(640, heldoutTrials.Count);
            var runnerVerify = new TowerBattleRunner(Path.Combine(heldoutRoot, "content"), OfflineContent.ForTower(Path.Combine(heldoutRoot, "content"), captured.Settings));
            foreach (var trial in heldoutTrials)
            {
                var scenario = HarnessJson.Read<TowerScenario>(Path.Combine(heldoutRoot, "recipes", trial.Recipe + ".json"));
                var input = runnerVerify.CreateInput(scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                Assert.Equal(trial.InputHash, HarnessJson.Hash(input));
                var saved = TowerLoadoutArchive.ReadBattle(heldoutRoot, trial.Id, captured.ReportStorage);
                Assert.Equal(trial.Seed, saved.Battle.Seed); Assert.Equal(scenario.Id, saved.Battle.ScenarioId);
                Assert.Equal(HarnessJson.Hash(outcomes[trial.Stage][Array.IndexOf(heldout, trial.Seed)]), HarnessJson.Hash(saved));
            }
            TowerCompleteReservation.Verify(q.Output, reservation.Seeds, allocator, token);
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            foreach (var (name, hash) in captured.ContentHashes)
                Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(q.Output, "content/Data", name)));
            foreach (var (name, hash) in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(q.Output, "executable", name)));
            var benchmark = outcomes[summary.BenchmarkId];
            var strongestReferenceId = referenceIds.OrderByDescending(id => outcomes[id].Count(r => r.Succeeded))
                .ThenBy(id => outcomes[id].Average(r => r.GuardianHealthRemainingPercent)).ThenBy(id => id, StringComparer.Ordinal).First();
            var strongest = outcomes[strongestReferenceId];
            Save("result.json", new { version, floor = q.Floor, status = "Complete", summary, strongestMeasuredReferenceId = strongestReferenceId,
                selectedOrigin = referenceIds.Contains(summary.SelectedId!) ? "existing-reference" : "generated-finalist",
                fights = completed, freshValues = values.Length, retries = 0, seconds = started.Elapsed.TotalSeconds,
                rows = members.Select(p => new { id = p.Id, role = referenceIds.Contains(p.Id) ? "existing-reference" : "generated-finalist",
                    benchmark = p.Id == summary.BenchmarkId, selected = p.Id == summary.SelectedId,
                    wins = outcomes[p.Id].Count(r => r.Succeeded), samples = HeldoutSamples,
                    gainedWins = outcomes[p.Id].Zip(benchmark).Count(v => v.First.Succeeded && !v.Second.Succeeded),
                    lostWins = outcomes[p.Id].Zip(benchmark).Count(v => !v.First.Succeeded && v.Second.Succeeded),
                    gainedWinsAgainstStrongestReference = outcomes[p.Id].Zip(strongest).Count(v => v.First.Succeeded && !v.Second.Succeeded),
                    lostWinsAgainstStrongestReference = outcomes[p.Id].Zip(strongest).Count(v => !v.First.Succeeded && v.Second.Succeeded),
                    meanGuardianHealth = outcomes[p.Id].Average(r => r.GuardianHealthRemainingPercent) }),
                interpretation = "One exploratory search; heldout measurements never change its choice. No independent team confirmation or algorithm superiority claim." });
            Check(); successful = true;
        }
        finally
        {
            Save("completion.json", new { status = successful ? "Complete" : "Failed", attempts, completed,
                seconds = started.Elapsed.TotalSeconds, retries = 0 });
            Seal(q.Output);
        }
    }

    internal static Dictionary<string, string> Inventory(string root) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash);
    internal static void Seal(string root) => HarnessJson.WriteNew(Path.Combine(root, "files.json"), Inventory(root));
    internal static TowerLoadoutArchive Archive(string root, LoadoutScope scope, int cap, string source, CancellationToken token)
    {
        Directory.CreateDirectory(root);
        TowerBundle.CopyContent(Path.Combine(source, "content"), Path.Combine(root, "content"), token);
        Directory.CreateDirectory(Path.Combine(root, "recipes")); Directory.CreateDirectory(Path.Combine(root, "battles"));
        HarnessJson.WriteNew(Path.Combine(root, "scope.json"), scope);
        return new(root, scope, cap);
    }
}
