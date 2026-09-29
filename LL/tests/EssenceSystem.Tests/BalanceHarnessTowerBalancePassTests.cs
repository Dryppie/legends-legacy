using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

// Bounded, explicitly invoked experiments. Ordinary regression runs never fight.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBalancePassTests
{
    internal const string Version = "tower-balance-pass-v1";
    internal sealed record Cell(string Id, string Composition, string Gear, string Origin, TowerScenario Scenario);
    private sealed record Request(string Mode, string ApiRoot, string Fixtures, string Output, int Floor,
        int[] Seeds, int[] SearchSeeds, string? Cells, string? Earned, string? History,
        int Benchmark, int MaximumFights, IReadOnlyDictionary<string, string> InputHashes);
    private sealed record Score(string Id, string Composition, string Gear, string Origin, int Wins,
        int Samples, decimal MeanGuardianHealth, double MeanSeconds);

    internal static TowerScenario Canonical(TowerScenario scenario) => scenario with {
        Seeds = [], Party = scenario.Party.Select(p => p with { Build = p.Build with {
            EssenceIds = p.Build.EssenceIds.Order(StringComparer.Ordinal).ToArray() } }).ToArray() };

    internal static Cell[] Family(string root, string fixtures, int floor, string? earned)
    {
        var draft = HarnessJson.Read<TowerProgressionDraft>(Path.Combine(fixtures, TowerProgressionPreview.CycleFixture));
        TowerProgressionPreview.Validate(draft);
        var budget = draft.Budgets.Single(b => b.PriorityFloor == floor);
        var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var template = TowerProgressionEquipment.Apply(TowerPartyProgression.Scenarios(root, fixtures, budget)
            .Single(s => s.FloorNumber == floor), draft.EquipmentCycle!, content);
        var controls = HarnessJson.Read<TowerWholePartyHistory>(Path.Combine(fixtures, TowerWholeParty.Fixture));
        var compositions = controls.Parties[budget.EssenceSlots].Take(3).Select((p, i) =>
            (Id: $"reference-{i + 1}", Origin: "retained-reference", Scenario: Canonical(TowerPartySelection.Apply(template, p.Builds, [])))).ToList();
        if (earned is not null)
        {
            var paths = Directory.GetFiles(earned, "*--continuation.json.gz").Order(StringComparer.Ordinal);
            foreach (var path in paths)
            {
                var saved = TowerUnlockStudy.Read(path);
                var party = saved.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
                Assert.Equal(floor, party.Floor.FloorNumber); Assert.Equal(template.Party.Count, party.Members.Count);
                var builds = party.Members.Select((m, i) => (Slot: i + 1,
                    Essences: (IReadOnlyList<string>)m.Character.Essences.Select(e => e.DefinitionId).ToArray()))
                    .ToDictionary(p => p.Slot, p => p.Essences);
                Assert.All(builds.Values, ids => Assert.Equal(budget.EssenceSlots, ids.Count));
                compositions.Add((Path.GetFileName(path).Replace("--continuation.json.gz", ""), "earned-composition-at-declared-budget",
                    Canonical(TowerPartySelection.Apply(template, builds, []))));
            }
        }
        // Fixed ordinal ordering and identical actor identities: permutations are never candidates.
        var unique = compositions.DistinctBy(p => HarnessJson.Hash(p.Scenario.Party.Select(m => m.Build.EssenceIds))).ToArray();
        var profiles = TowerGearProfiles.Read(Path.Combine(fixtures, "tower-gear-specialization-screen.json"));
        return unique.SelectMany(p => new[] { new Cell(p.Id + "/baseline", p.Id, "baseline", p.Origin, p.Scenario) }
            .Concat(profiles.Profiles.Select(g => new Cell(p.Id + "/" + g.Id, p.Id, g.Id, p.Origin,
                TowerGearProfiles.Apply(p.Scenario, g, content))))).ToArray();
    }

    [Theory]
    [InlineData(1, 30, 4, 5, 2, "Rare", "Standard")]
    [InlineData(2, 30, 4, 5, 2, "Rare", "Standard")]
    [InlineData(3, 30, 4, 5, 2, "Rare", "Standard")]
    [InlineData(4, 30, 4, 5, 3, "Epic", "Fine")]
    [InlineData(5, 40, 5, 10, 3, "Epic", "Fine")]
    [InlineData(6, 40, 5, 5, 3, "Epic", "Fine")]
    [InlineData(7, 40, 5, 5, 4, "Unique", "Exceptional")]
    [InlineData(8, 40, 5, 10, 4, "Unique", "Exceptional")]
    [InlineData(9, 40, 5, 10, 4, "Unique", "Exceptional")]
    public async Task Family_preserves_declared_power_and_order_without_fighting(
        int floor, int level, int essenceCount, int partySize, int rank, string rarity, string quality)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        var root = TestContentPaths.FindApiRoot(); var fixtures = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var cells = Family(root, fixtures, floor, null); Assert.Equal(21, cells.Length);
        var settings = TowerBundle.ReadSettings(root); var content = OfflineContent.ForTower(root, settings);
        var runner = new TowerBattleRunner(root, content);
        foreach (var cell in cells)
        {
            Assert.Equal(partySize, cell.Scenario.Party.Count);
            foreach (var member in cell.Scenario.Party)
            {
                var b = member.Build; Assert.Equal(level, b.CharacterLevel); Assert.Equal(1, b.Tier); Assert.Equal(rank, b.Rank);
                Assert.Equal(essenceCount, b.EssenceIds.Count); Assert.Equal(b.EssenceIds.Order(StringComparer.Ordinal), b.EssenceIds);
                var actual = content.CreateBuild(b);
                Assert.All(actual.EquippedEssences, e => { Assert.Equal(0, e.AscensionTier); Assert.False(e.IsEvolved); });
                Assert.All(actual.Equipment, e => { Assert.Equal(rarity, e.ProgressionData!.Rarity.ToString());
                    Assert.Equal(quality, e.ProgressionData.State.Quality.ToString()); });
            }
            _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario with { Seeds = [0] }, 0, settings.Threat, settings.CheckpointIntervalTicks));
        }
        var sample = cells[0].Scenario;
        Assert.Equal(HarnessJson.Hash(Canonical(sample)), HarnessJson.Hash(Canonical(sample with {
            Party = sample.Party.Select(p => p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() })));
    }

    private sealed class OwnedFactAttribute : FactAttribute
    {
        public OwnedFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_BALANCE_PASS")))
            Skip = "Requires a frozen bounded Tower balance request."; }
    }

    [OwnedFact]
    public async Task Frozen_tower_balance_pass()
    {
        var q = HarnessJson.Read<Request>(Environment.GetEnvironmentVariable("LL_TOWER_BALANCE_PASS")!);
        Assert.Contains(q.Mode, new[] { "prepare", "screen", "search", "confirm" });
        Assert.InRange(q.MaximumFights, 0, 20000); Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.Seeds.Length, q.Seeds.Distinct().Count());
        Assert.Empty(q.Seeds.Intersect(q.SearchSeeds));
        void Pins() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        Pins(); Directory.CreateDirectory(q.Output);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        var watch = System.Diagnostics.Stopwatch.StartNew(); var attempts = 0; var completed = 0; var success = false;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= q.MaximumFights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1024 * 1024 * 1024); }
        void Attempt(bool done) { if (done) completed++; else { attempts++; Check();
            File.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), JsonSerializer.Serialize(new { attempt = attempts }) + "\n"); } }
        try
        {
            Save("request.json", q);
            var root = Path.Combine(q.Output, "content"); var settings = TowerBundle.ReadSettings(q.ApiRoot);
            var hashes = TowerBundle.CopyContent(q.ApiRoot, root, token);
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), settings);
            var scope = new LoadoutScope(Version, settings, ExecutionIdentity.Current(), hashes, "gzip-json-v1"); Save("scope.json", scope);
            var cells = q.Mode == "prepare" ? Family(root, q.Fixtures, q.Floor, q.Earned)
                : HarnessJson.Read<Cell[]>(q.Cells!);
            Assert.NotEmpty(cells); Assert.Equal(cells.Length, cells.Select(c => c.Id).Distinct().Count());
            Assert.All(cells, c => { Assert.Equal(q.Floor, c.Scenario.FloorNumber); Assert.Empty(c.Scenario.Seeds); });
            var runner = new TowerBattleRunner(root, OfflineContent.ForTower(root, settings));
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preflight cannot fight.")).Activate())
                foreach (var cell in cells) _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario with { Seeds = [0] }, 0, settings.Threat, settings.CheckpointIntervalTicks), token);
            Save("cells.json", cells);
            if (q.Mode == "prepare")
            {
                Assert.Empty(q.Seeds); Assert.Empty(q.SearchSeeds); Assert.Equal(0, q.MaximumFights);
                Save("result.json", new { status = "PreparedNoFights", cells = cells.Length, compositions = cells.Select(c => c.Composition).Distinct().Count(), fights = 0 });
            }
            else
            {
                Assert.InRange(q.Seeds.Length, 16, 512);
                if (q.Mode == "search")
                {
                    Assert.Equal(3, cells.Length); Assert.Equal(109, q.SearchSeeds.Length);
                    var inventory = TowerBossInventory.CreateForTower(root, settings);
                    var source = BalanceHarnessAffinitySearchTests.Baseline();
                    source = source with { Racing = source.Racing with { Scope = source.Racing.Scope with {
                        AllowedEssences = inventory.Essences.Select(e => new BossDiscoveryEssence(e.Id, e.SourceMonsterId)).ToArray(), OwnedCopies = null } },
                        Policy = TowerProposalPolicies.BenchmarkAffinityCreation(TowerDamageSourceAffinities.Create(inventory).Affinities.Select(a => a.Id)) };
                    var draft = HarnessJson.Read<TowerProgressionDraft>(Path.Combine(q.Fixtures, TowerProgressionPreview.CycleFixture));
                    var plan = F.Project(source, cells.Select(c => c.Scenario).ToArray(), inventory, scope, q.SearchSeeds,
                        HarnessJson.Read<int[]>(q.History!), q.Seeds, draft.Budgets.Single(b => b.PriorityFloor == q.Floor), cells[0].Scenario.Party.Count, q.Benchmark);
                    Save("plan.json", plan);
                    var searchRoot = Path.Combine(q.Output, "search");
                    var search = F.Archive(searchRoot, scope with { Algorithm = TowerProposalRacingNative.ArchiveAlgorithm(plan) }, 528, q.Output, token);
                    var summary = await TowerAffinitySearch.RunAsync(plan, search, 96 * 1048576, Check, token, Attempt);
                    Assert.Equal(528, completed); Save("search-summary.json", summary); F.Seal(searchRoot);
                    Assert.Equal(summary, TowerAffinitySearch.Summarize(plan,
                        await TowerProposalRacingNative.VerifyAsync(searchRoot, HarnessJson.FileHash(Path.Combine(searchRoot, "files.json")), token)));
                    var report = HarnessJson.Read<TowerProposalRacingReport>(Path.Combine(searchRoot, "racing/search.json"));
                    var references = plan.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
                    cells = report.Evaluation.Panels.Single(p => p.Freeze.Role == "nomination").Freeze.Parties.Select(p => new Cell(p.Id, p.Id,
                        cells[0].Gear, references.Contains(p.Id) ? "retained-reference" : "generated-finalist",
                        TowerBossDiscovery.Scenario(plan.Racing.Scope, plan.Racing.Scope.Contexts[0].Id, p, []))).ToArray();
                    Save("evaluation-cells.json", cells);
                }
                else Assert.Empty(q.SearchSeeds);
                Assert.Equal(q.MaximumFights, (q.Mode == "search" ? 528 : 0) + cells.Length * q.Seeds.Length);
                var archiveRoot = Path.Combine(q.Output, "evaluation");
                var archive = F.Archive(archiveRoot, scope, cells.Length * q.Seeds.Length, q.Output, token);
                var scores = new List<Score>();
                foreach (var cell in cells)
                {
                    var outcomes = new List<TowerBattleReport>();
                    foreach (var seed in q.Seeds)
                    {
                        Attempt(false);
                        var trial = await archive.EvaluateAsync(Version, cell.Id, cell.Scenario with { Seeds = q.Seeds }, seed, token);
                        Attempt(true); outcomes.Add(trial.Report);
                    }
                    scores.Add(new(cell.Id, cell.Composition, cell.Gear, cell.Origin, outcomes.Count(r => r.Succeeded), outcomes.Count,
                        outcomes.Average(r => r.GuardianHealthRemainingPercent), outcomes.Average(r => (double)r.DisplayDurationSeconds)));
                }
                Assert.Equal(q.MaximumFights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(archiveRoot);
                var trials = TowerLoadoutArchive.Verify(archiveRoot, token);
                foreach (var trial in trials)
                {
                    var scenario = HarnessJson.Read<TowerScenario>(Path.Combine(archiveRoot, "recipes", trial.Recipe + ".json"));
                    Assert.Equal(trial.InputHash, HarnessJson.Hash(runner.CreateInput(scenario, trial.Seed, settings.Threat, settings.CheckpointIntervalTicks)));
                }
                Save("result.json", new { status = "Complete", mode = q.Mode, floor = q.Floor, fights = completed, rows = scores,
                    interpretation = "Fixed declared power, hypothetical ownership; earned compositions are projected, not earned inventory replays. Separate phases are never pooled. No universal composition coverage or acquisition-time claim." });
            }
            Pins(); Check(); success = true;
        }
        finally
        {
            Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, seconds = watch.Elapsed.TotalSeconds, retries = 0 });
            F.Seal(q.Output);
        }
    }
}
