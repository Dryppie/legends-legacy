using System.Numerics;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments.Slots;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGearScreenTests
{
    internal const string Version = "tower-gear-specialization-screen-v1";
    private const int DiscoverySamples = 128, ConfirmationSamples = 256, Fights = 1408;
    private const long MaximumBytes = 1073741824;
    private sealed record Profile(string Id, string Description, IReadOnlyList<int> PartySlots,
        IReadOnlyDictionary<EquipmentSlotType, string> Specializations);
    private sealed record Request(string Version, string Source, string SourceManifestHash, string Output, string Registry,
        string Runtime, string BaselineId, string Profiles, string ProfilesHash, int Master,
        IReadOnlyDictionary<string, string> AssemblyHashes, IReadOnlyDictionary<string, string> RequiredHistory,
        IReadOnlyDictionary<string, string> Recoveries, IReadOnlyDictionary<string, string> RecoveryHashes);
    private sealed record SourceTeam(PartyChoice Party, string Role, TowerScenario Scenario);
    private sealed record Variant(string Id, string Description, TowerScenario Scenario);
    private sealed record Outcome(bool Win, decimal Health);
    private sealed record Score(string Id, int Wins, int Samples, decimal MeanGuardianHealth);
    private sealed record Contrast(int GainedWins, int LostWins, double ObservedGain,
        string TailNumerator, string TailDenominator, double OneSidedPValue, bool Qualifies);
    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_GEAR_SCREEN")))
                Skip = "Set LL_GEAR_SCREEN for one frozen 1,408-fight gear screen and selected-profile confirmation.";
        }
    }

    private static Profile[] ReadProfiles(string path)
    {
        var json = HarnessJson.Read<JsonElement>(path);
        Assert.Equal(Version, json.GetProperty("version").GetString());
        var profiles = json.GetProperty("profiles").Deserialize<Profile[]>(HarnessJson.Options)!;
        Assert.Equal(6, profiles.Length); Assert.Equal(6, profiles.Select(p => p.Id).Distinct().Count());
        Assert.All(profiles, p => { Assert.NotEqual("baseline", p.Id); Assert.NotEmpty(p.Specializations);
            Assert.Equal(p.PartySlots.Count, p.PartySlots.Distinct().Count()); Assert.All(p.PartySlots, slot => Assert.InRange(slot, 1, 15)); });
        return profiles;
    }

    private static Variant Apply(TowerScenario original, Profile? profile, OfflineContent content)
    {
        var party = original.Party.Select(p => p with { Build = p.Build with {
            IdentityEquipment = p.Build.IdentityEquipment ?? p.Build.Equipment,
            Equipment = p.Build.Equipment.Select(item => {
                if (profile is null || profile.PartySlots.Count != 0 && !profile.PartySlots.Contains(p.PartySlot)
                    || !profile.Specializations.TryGetValue(item.Slot, out var specialization)) return item;
                var evaluator = content.Equipment.Evaluator; var before = evaluator.GetDefinition(item.DefinitionId);
                var after = evaluator.Definitions.Single(d => d.ArchetypeId == before.ArchetypeId && d.Rarity == before.Rarity
                    && d.SpecializationId == specialization && d.NativeStyleId is null);
                var b = p.Build;
                var first = evaluator.Evaluate(before.Id, b.Tier, b.Rank, null, b.Quality, b.AttributeRollMultiplier);
                var second = evaluator.Evaluate(after.Id, b.Tier, b.Rank, null, b.Quality, b.AttributeRollMultiplier);
                Assert.Equal(first.TargetBudget, second.TargetBudget); Assert.Equal(first.Archetype.Id, second.Archetype.Id);
                Assert.Null(item.ActiveStyleId); Assert.False(item.UseNativeStyle);
                return item with { DefinitionId = after.Id };
            }).ToArray() } }).ToArray();
        return new(profile?.Id ?? "baseline", profile?.Description ?? "Unchanged equipment", original with { Party = party, Seeds = [] });
    }

    private static Contrast Compare(int gains, int losses)
    {
        if (gains < 0 || losses < 0 || gains + losses > ConfirmationSamples) throw new InvalidDataException("Invalid paired counts.");
        BigInteger choose = 1, tail = 0; var n = gains + losses;
        for (var k = 0; k <= n; k++) { if (k >= gains) tail += choose; if (k < n) choose = choose * (n - k) / (k + 1); }
        var denominator = BigInteger.One << n;
        return new(gains, losses, (gains - losses) / (double)ConfirmationSamples, tail.ToString(), denominator.ToString(),
            (double)tail / (double)denominator, gains - losses >= 13 && 20 * tail <= denominator);
    }

    [Theory]
    [InlineData(0, 0, false)] [InlineData(12, 0, false)] [InlineData(13, 0, true)]
    [InlineData(120, 107, false)] [InlineData(256, 0, true)]
    public void Single_selected_profile_requires_a_five_point_gain_and_exact_fresh_evidence(int gains, int losses, bool expected)
    {
        Assert.Equal(expected, Compare(gains, losses).Qualifies);
        Assert.Equal(1.0 / 32, Compare(5, 0).OneSidedPValue);
        Assert.Throws<InvalidDataException>(() => Compare(256, 1));
    }

    [Fact]
    public async Task All_profiles_prepare_at_equal_item_budgets_with_fixed_instances_and_essences()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Gear preflight cannot fight.")).Activate();
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var catalogs = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var source = TowerPartyProgression.Scenarios(root, catalogs, TowerPartyProgression.Budget(10)).Single(s => s.FloorNumber == 15);
        var originalHash = HarnessJson.Hash(source); var content = OfflineContent.ForTower(root, settings);
        var variants = ReadProfiles(Path.Combine(catalogs, "tower-gear-specialization-screen.json")).Select(p => Apply(source, p, content)).ToArray();
        Assert.Equal(6, variants.Select(v => HarnessJson.Hash(v.Scenario.Party)).Distinct().Count());
        var runner = new TowerBattleRunner(root, content);
        foreach (var variant in variants.Prepend(Apply(source, null, content)))
        {
            foreach (var pair in source.Party.Zip(variant.Scenario.Party))
            {
                var before = content.CreateBuild(pair.First.Build); var after = content.CreateBuild(pair.Second.Build);
                Assert.Equal(before.Character.Id, after.Character.Id);
                Assert.Equal(before.Equipment.Select(i => i.Id), after.Equipment.Select(i => i.Id));
                Assert.Equal(before.EquippedEssences.Select(i => i.Id), after.EquippedEssences.Select(i => i.Id));
                Assert.Equal(before.Definition.EssenceIds, after.Definition.EssenceIds);
                Assert.Equal(HarnessJson.Hash(pair.First.Build), HarnessJson.Hash(pair.Second.Build with {
                    Equipment = pair.First.Build.Equipment, IdentityEquipment = pair.First.Build.IdentityEquipment }));
            }
            _ = await runner.PrepareAsync(runner.CreateInput(variant.Scenario with { Seeds = [0] }, 0, settings.Threat, settings.CheckpointIntervalTicks));
        }
        Assert.Equal(originalHash, HarnessJson.Hash(source));
    }

    [ScreenFact]
    public async Task Frozen_gear_profiles_screen_and_confirm_one_selected_profile()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_GEAR_SCREEN")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        foreach (var path in new[] { q.Source, q.Output, q.Registry, q.Runtime, q.Profiles }) TowerProposalStudy.Unlinked(path);
        Assert.Equal(q.SourceManifestHash, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { var path = Path.Combine(q.Source, name); Assert.Equal(files[name], HarnessJson.FileHash(path)); return HarnessJson.Read<T>(path); }
        var originalScope = Source<LoadoutScope>("scope.json");
        var captured = originalScope with { Algorithm = Version, Execution = ExecutionIdentity.Current() };
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(captured.Execution.AssemblyHashes));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), captured.Settings.Balance);
        var baseline = Source<SourceTeam[]>("teams.json").Single(t => t.Party.Id == q.BaselineId);
        Assert.Equal("existing-reference", baseline.Role); Assert.Empty(baseline.Scenario.Seeds);
        Assert.Equal(15, baseline.Scenario.FloorNumber); Assert.Equal(15, baseline.Scenario.Party.Count);
        Assert.Equal(q.ProfilesHash, HarnessJson.FileHash(q.Profiles)); var profiles = ReadProfiles(q.Profiles);
        var historical = TowerSearchBenchmark.History(Source<JsonElement>("seed-ledger.json"));
        foreach (var pin in q.RecoveryHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = stop.Token;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Registry, "complete-family-allocation"));
        using var outputLease = TowerCompactBundle.AcquireWriter(q.Output);
        Assert.False(Path.Exists(q.Output)); Directory.CreateDirectory(q.Output);
        var clock = System.Diagnostics.Stopwatch.StartNew(); var attempts = 0; var completed = 0; var success = false;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < MaximumBytes); }
        try
        {
            Save("request.json", q); Save("scope.json", captured); Save("source-scope.json", originalScope);
            Save("profiles.json", profiles); Save("source-baseline.json", baseline);
            Save("protocol.json", new { version = Version, discoverySamples = DiscoverySamples, confirmationSamples = ConfirmationSamples,
                maximumFights = Fights, maximumSeconds = 840, maximumBytes = MaximumBytes, retries = 0,
                selection = "Highest discovery wins among six alternatives; then lower mean boss health, then ordinal profile ID.",
                decision = "One selected profile versus baseline on 256 separate fresh seeds: at least 13 net wins AND exact one-sided paired p <= .05. No pooled discovery results or extensions." });
            var history = TowerRefinementComparisonLaunch.Refresh(q.Registry, q.Output, q.RequiredHistory, historical, token, q.Recoveries);
            Save("history-files.json", history.Files);
            var contentRoot = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(Path.Combine(q.Source, "content"), contentRoot, token)));
            TowerBundle.WriteSettings(Path.Combine(contentRoot, "appsettings.json"), captured.Settings);
            foreach (var pin in captured.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var file in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, file); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative); Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(contentRoot, captured.Settings); var runner = new TowerBattleRunner(contentRoot, content);
            var variants = profiles.Select(p => Apply(baseline.Scenario, p, content)).Prepend(Apply(baseline.Scenario, null, content)).ToArray();
            Save("variants.json", variants);
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Gear admission cannot fight.")).Activate())
            {
                // The optional identity pin must preserve the existing baseline's frozen actors exactly.
                var before = runner.CreateInput(baseline.Scenario with { Seeds = [0] }, 0, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                foreach (var variant in variants)
                {
                    var input = runner.CreateInput(variant.Scenario with { Seeds = [0] }, 0, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                    Assert.Equal(before.Party.Select(p => p.Character.Id), input.Party.Select(p => p.Character.Id));
                    if (variant.Id == "baseline") Assert.Equal(HarnessJson.Hash(before.Party), HarnessJson.Hash(input.Party));
                    _ = await runner.PrepareAsync(input, token);
                }
                // Qualify the new build against every archived baseline input before allocating new seeds.
                var oldTrials = Source<JsonElement>("study/scope.json");
                Assert.Equal(HarnessJson.Hash(originalScope), HarnessJson.Hash(oldTrials));
                var trialPath = Path.Combine(q.Source, "study/trials.jsonl"); Assert.Equal(files["study/trials.jsonl"], HarnessJson.FileHash(trialPath));
                var checkedInputs = 0;
                foreach (var trial in File.ReadLines(trialPath).Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).Where(t => t.Stage == q.BaselineId))
                {
                    var scenario = Source<TowerScenario>("study/recipes/" + trial.Recipe + ".json");
                    Assert.Equal(trial.InputHash, HarnessJson.Hash(runner.CreateInput(scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks)));
                    checkedInputs++;
                }
                Assert.Equal(512, checkedInputs);
            }
            Save("preflight.json", new { status = "PreparedNoFights", variants = variants.Length, historicalInputsMatched = 512, historyCount = historical.Length });
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); Check();
            var storage = new TowerCompleteReservation.Storage(q.Output, MaximumBytes);
            storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
            var values = new List<int>(); var allocations = new List<(string Path, TowerCompleteAllocatorPlan Plan, TowerCompleteSeeds Seeds)>();
            foreach (var (name, count) in new[] { ("discovery", DiscoverySamples), ("confirmation", ConfirmationSamples) })
            {
                var path = Path.Combine(q.Output, "allocation-" + name); Directory.CreateDirectory(path);
                var plan = new TowerCompleteAllocatorPlan("sha256-us-int32le-reject-v1", Version + "/" + name, q.Master, 1, count - 1, 100000);
                var allocation = TowerCompleteReservation.Reserve(path, plan, historical.Concat(values).Order().ToArray(), MaximumBytes, token);
                TowerCompleteReservation.Verify(path, allocation.Seeds, plan, token); allocations.Add((path, plan, allocation.Seeds));
                values.AddRange(allocation.Seeds.First.Concat(allocation.Seeds.Second));
                storage.Put("history-input.json", new { reservationState = "Pending", reserved = values.ToArray() }, true);
            }
            Assert.Equal(384, values.Distinct().Count());
            Save("panel-seeds.json", values);
            storage.Put("seed-ledger.json", new { reservationState = "Complete", historical, reserved = values.ToArray() });
            storage.Put("history-input.json", new { reservationState = "Complete", reserved = values.ToArray() }, true);
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, captured, Fights, q.Output, token);
            var evidence = new Dictionary<string, List<Outcome>>();
            async Task Run(string phase, Variant variant, int[] panel)
            {
                var key = phase + "/" + variant.Id; var outcomes = new List<Outcome>(); evidence.Add(key, outcomes);
                var scenario = variant.Scenario with { Seeds = panel };
                foreach (var seed in panel)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    var trial = await archive.EvaluateAsync(Version, key, scenario, seed, token); completed++;
                    outcomes.Add(new(trial.Report.Succeeded, trial.Report.GuardianHealthRemainingPercent));
                }
            }
            Score Row(string phase, string id) => new(id, evidence[phase + "/" + id].Count(o => o.Win), evidence[phase + "/" + id].Count, evidence[phase + "/" + id].Average(o => o.Health));
            foreach (var variant in variants) await Run("discovery", variant, values.Take(DiscoverySamples).ToArray());
            var scores = variants.Select(v => Row("discovery", v.Id)).ToArray();
            var selected = scores.Where(s => s.Id != "baseline").OrderByDescending(s => s.Wins).ThenBy(s => s.MeanGuardianHealth).ThenBy(s => s.Id, StringComparer.Ordinal).First().Id;
            Save("selection-freeze.json", new { selected, afterFights = completed, scores, variantsHash = HarnessJson.Hash(variants) });
            foreach (var variant in new[] { variants[0], variants.Single(v => v.Id == selected) }) await Run("confirmation", variant, values.Skip(DiscoverySamples).ToArray());
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            foreach (var trial in trials)
            {
                Check(); var parts = trial.Stage.Split('/'); var panel = parts[0] == "discovery" ? values.Take(DiscoverySamples).ToArray() : values.Skip(DiscoverySamples).ToArray();
                var scenario = variants.Single(v => v.Id == parts[1]).Scenario with { Seeds = panel };
                Assert.Equal(HarnessJson.Hash(scenario), trial.Recipe);
                var input = runner.CreateInput(scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(captured, Version, input), trial.CacheKey);
                var report = TowerLoadoutArchive.ReadBattle(study, trial.Id, captured.ReportStorage);
                Assert.Equal(trial.Seed, report.Battle.Seed); Assert.Equal(scenario.Id, report.Battle.ScenarioId);
                Assert.Equal(report.Battle.Summary.ContentOutcome == BattleOutcome.Victory, report.Succeeded);
                Assert.Equal(evidence[trial.Stage][Array.IndexOf(panel, trial.Seed)], new Outcome(report.Succeeded, report.GuardianHealthRemainingPercent));
            }
            foreach (var a in allocations) TowerCompleteReservation.Verify(a.Path, a.Seeds, a.Plan, token);
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            foreach (var pin in captured.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(contentRoot, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json"))) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var pairs = evidence["confirmation/" + selected].Zip(evidence["confirmation/baseline"]).ToArray();
            var contrast = Compare(pairs.Count(p => p.First.Win && !p.Second.Win), pairs.Count(p => !p.First.Win && p.Second.Win));
            Save("result.json", new { version = Version, status = "Complete", fights = completed, freshValues = values.Count, selected,
                decision = contrast.Qualifies ? "GearProfileImprovementConfirmed" : "GearProfileImprovementNotDemonstrated", discovery = scores,
                confirmation = new[] { Row("confirmation", "baseline"), Row("confirmation", selected) }, contrast, seconds = clock.Elapsed.TotalSeconds, retries = 0,
                interpretation = "One selected gear profile on a fixed Essence team, floor and item budget; no joint-search or global-optimality claim." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, seconds = clock.Elapsed.TotalSeconds, retries = 0 }); F.Seal(q.Output); }
    }
}
