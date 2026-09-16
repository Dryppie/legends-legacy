using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using Runtime = EssenceSystem.Tests.BalanceHarnessRefinementComparisonFixture;
using Model = BalanceHarness.TowerRefinementComparisonModel;

namespace EssenceSystem.Tests;

// Every observation is fabricated. No allocator, content preparation or combat engine is called.
public sealed class BalanceHarnessZeroWinSelectionTests : IDisposable
{
    readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Zero-win fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();

    static TowerBossDiscoveryDefinition[] Pair(string version = Model.ZeroWinVersion)
    {
        var source = Runtime.Definitions()[0]; var s = source.Stages.Schedules.Values.Single();
        var labels = new TowerRefinementComparisonSeeds(source.ExcludedCombatSeeds.ToArray(), source.Generation.Seeds.ToArray(),
            s.Discovery.ToArray(), s.Selection.ToArray(), s.Confirmation.ToArray());
        return Model.Policies.Select(p => Model.Definition(source, labels, p, version)).ToArray();
    }
    static TowerRefinementComparisonMember[] Nominations(TowerBossDiscoveryDefinition d, bool swapped = false)
    {
        var a = Model.Canonical(d.References[0].Scenario); var b = Model.Canonical(d.References[1].Scenario);
        return [new("a", a, ["baseline-rank-1"]), new("b", b, ["baseline-rank-2"]),
            new("c", swapped ? b : a, ["discovery-refinement-rank-1"]), new("d", swapped ? a : b, ["discovery-refinement-rank-2"])];
    }
    static TowerBalanceEvidence[] Evidence(TowerBalanceDefinition d, int firstWins = 0, int secondWins = 0) => d.Cells.Select((c, i) =>
        new TowerBalanceEvidence(c.Id, "Complete", HarnessJson.Hash(c.Scenario), HarnessJson.Hash(d.ContentHashes), d.SettingsHash,
            d.ExecutionHash, c.Scenario.Party.Count, c.Scenario.Seeds.Select((seed, j) =>
                new TowerBalanceTrial(seed, j < (i == 0 ? firstWins : secondWins) ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), new string('a', 64))).ToArray();
    static TowerRefinementSelectionHealth[] Health(IReadOnlyList<TowerBalanceEvidence> rows, double first = 80, double second = 20) => rows.Select((row, i) =>
        new TowerRefinementSelectionHealth(row.CellId, HarnessJson.Hash(row), row.Trials.Select(t => new TowerRefinementSelectionTrial(t.Seed, i == 0 ? first : second)).ToArray())).ToArray();

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(2, 1, true)]
    [InlineData(1, 1, true)]
    [InlineData(0, 0, false)]
    public void Wins_precede_health_and_positive_ties_keep_discovery_rank(int firstWins, int secondWins, bool first)
    {
        var d = Pair()[0]; var nominations = Nominations(d); var screen = Model.Screen(nominations); var definition = Model.Balance(d, screen, false);
        var evidence = Evidence(definition, firstWins, secondWins);
        var finalists = Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion, Health(evidence));
        Assert.All(finalists, row => Assert.Equal(screen[first ? 0 : 1].Id, row.Id));
    }

    [Fact] public void Zero_ties_use_arithmetic_mean_of_all_trials_and_ignore_enumeration_order()
    {
        var d = Pair()[0]; var nominations = Nominations(d); var screen = Model.Screen(nominations); var definition = Model.Balance(d, screen, false);
        var evidence = Evidence(definition); var health = Health(evidence, 15, 0);
        health[1] = health[1] with { Trials = health[1].Trials.Select((t, i) => t with { GuardianHealthRemainingPercent = i < 2 ? 100 : 0 }).ToArray() };
        // Median and best-trial progress favor the second team; its arithmetic mean is 25, so the first wins.
        var expected = Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion, health);
        Assert.All(expected, row => Assert.Equal(screen[0].Id, row.Id));
        Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(Model.Finalists(definition, nominations.Reverse().ToArray(),
            evidence.Reverse().ToArray(), Model.ZeroWinVersion, health.Reverse().ToArray())));
    }

    [Fact] public void Shared_recipes_keep_each_arms_original_rank_and_merge_shared_finalists()
    {
        var d = Pair()[0]; var nominations = Nominations(d, swapped: true); var screen = Model.Screen(nominations); var definition = Model.Balance(d, screen, false);
        Assert.Equal(new[] { "baseline-rank-1", "discovery-refinement-rank-2" }, screen[0].Origins);
        Assert.Equal(new[] { "baseline-rank-2", "discovery-refinement-rank-1" }, screen[1].Origins);
        var evidence = Evidence(definition); var tie = Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion, Health(evidence, 20, 20));
        Assert.Equal(screen[0].Id, tie[0].Id); Assert.Equal(screen[1].Id, tie[1].Id);
        var winners = Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion, Health(evidence));
        Assert.All(winners, row => Assert.Equal(screen[1].Id, row.Id));
        var shared = Model.Family(d, winners).Single(row => row.Origins.Contains("baseline-finalist"));
        Assert.Contains("discovery-refinement-finalist", shared.Origins);
        Assert.Contains(d.References[1].Id, shared.Origins);
    }

    [Fact] public void Exact_mean_and_discovery_rank_ties_fall_back_to_ordinal_id()
    {
        var rows = new[] { (Id: "z", Rank: 2, Wins: 0, Health: 20d), (Id: "b", Rank: 1, Wins: 0, Health: 20d), (Id: "a", Rank: 1, Wins: 0, Health: 20d) };
        Assert.Equal(new[] { "a", "b", "z" }, TowerZeroWinSelection.Rank(rows.Reverse(), r => r.Wins, r => r.Health, r => r.Rank, r => r.Id).Select(r => r.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Model.Version)]
    [InlineData(Model.NovelVersion)]
    [InlineData(Model.LocalVersion)]
    [InlineData(Model.FreshFirstVersion)]
    public void Legacy_versions_keep_identical_serialized_finalists_without_health(string? version)
    {
        var d = Pair(version ?? Model.Version)[0]; var nominations = Nominations(d); var screen = Model.Screen(nominations);
        var definition = Model.Balance(d, screen, false); var evidence = Evidence(definition);
        var expected = new[] { screen[0] with { Origins = ["baseline-finalist"] }, screen[0] with { Origins = ["discovery-refinement-finalist"] } };
        Assert.Equal(JsonSerializer.Serialize(expected, HarnessJson.Options), JsonSerializer.Serialize(Model.Finalists(definition, nominations, evidence, version), HarnessJson.Options));
        Assert.Equal("{\"seed\":201,\"outcome\":\"Defeat\"}", JsonSerializer.Serialize(new TowerBalanceTrial(201, BattleOutcome.Defeat),
            new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false }));
    }

    [Fact] public void Health_requires_every_valid_trial_bound_to_verified_selection_evidence()
    {
        var d = Pair()[0]; var nominations = Nominations(d); var definition = Model.Balance(d, Model.Screen(nominations), false);
        var evidence = Evidence(definition); var valid = Health(evidence);
        Assert.Throws<InvalidDataException>(() => Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion));
        foreach (var invalid in new IReadOnlyList<TowerRefinementSelectionHealth>[] { valid[..1], [valid[0], valid[0]],
            [valid[0] with { EvidenceHash = new string('b', 64) }, valid[1]],
            [valid[0] with { Trials = valid[0].Trials.Skip(1).ToArray() }, valid[1]],
            [valid[0] with { Trials = valid[0].Trials.Select((t, i) => i == 0 ? t with { Seed = 999 } : t).ToArray() }, valid[1]] })
            Assert.Throws<InvalidDataException>(() => Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion, invalid));
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, -1d, 101d }) {
            var invalid = valid.ToArray(); invalid[0] = invalid[0] with { Trials = invalid[0].Trials.Select(t => t with { GuardianHealthRemainingPercent = value }).ToArray() };
            Assert.Throws<InvalidDataException>(() => Model.Finalists(definition, nominations, evidence, Model.ZeroWinVersion, invalid));
        }
        Assert.Throws<InvalidDataException>(() => Model.Finalists(definition, nominations, evidence[..1], Model.ZeroWinVersion, valid));
        Assert.Throws<InvalidDataException>(() => Model.Finalists(definition with { Id = "comparison-confirmation" }, nominations, evidence, Model.ZeroWinVersion, valid));
    }

    [Fact] public void Version_contract_changes_only_opt_in_identity_and_rejects_mixed_arms_and_receipts()
    {
        var next = Pair(); var old = Pair(Model.FreshFirstVersion); Model.ValidatePair(next);
        for (var i = 0; i < 2; i++) Assert.Equal(HarnessJson.Hash(old[i]), HarnessJson.Hash(next[i] with { Id = old[i].Id }));
        Assert.Throws<InvalidDataException>(() => Model.ValidatePair([next[0], old[1]]));
        Assert.Throws<InvalidDataException>(() => Model.ResolveVersion("unknown"));
        var receipt = JsonSerializer.SerializeToElement(new { status = "VerifiedRefinementComparisonDriver", reservations = 1,
            comparisonVersion = Model.FreshFirstVersion, refinementPolicy = TowerDiscoveryRefinementSearch.FreshFirstVersion });
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonPreflight.VerifyDriverReceipt(receipt, 1, Model.ZeroWinVersion));
        var valid = JsonSerializer.SerializeToElement(new { status = "VerifiedRefinementComparisonDriver", reservations = 1,
            comparisonVersion = Model.ZeroWinVersion, refinementPolicy = TowerDiscoveryRefinementSearch.FreshFirstVersion });
        TowerRefinementComparisonPreflight.VerifyDriverReceipt(valid, 1, Model.ZeroWinVersion);
    }

    static PartyChoice Party(params string[][] owners) => TowerPartySelection.Choice("synthetic", owners.Select((ids, i) => (ids, i))
        .ToDictionary(p => p.i + 1, p => (IReadOnlyList<string>)p.ids));
    static BossDiscoveryMeasurement Score(BossDiscoveryInputs input, PartyChoice party, int wins, double health)
    {
        var initial = F.Measure(input, party); var cells = initial.Cells.Select(c => c with { GuardianHealth = health,
            Clears = input.DiscoverySeeds[c.Context].Select((_, i) => i < wins).ToArray() }).ToArray();
        return initial with { Cells = cells, Fitness = TowerBossGeneration.Fitness(input, cells, 100) };
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 1, true)]
    public void Staged_opt_in_uses_the_same_rule_and_frozen_discovery_rank(int firstWins, int secondWins, bool first)
    {
        var input = F.Input(); var a = Party(["e00", "e01", "e02", "e03"], ["e04", "e05", "e06", "e07"]);
        var b = Party(["e00", "e01", "e02", "e08"], ["e04", "e05", "e06", "e07"]);
        var rows = new[] { Score(input, a, firstWins, 80), Score(input, b, secondWins, 20) };
        var selected = TowerBossStudyPolicy.Select(input, F.Mechanics(input), [a, b], rows.Reverse().ToArray(), input.DiscoverySeeds, 1, TowerBossStudyPolicy.ZeroWinVersion);
        Assert.Equal(first ? a.Id : b.Id, selected.Single().Party.Id);
        if (firstWins == secondWins) Assert.Equal(b.Id, TowerBossStudyPolicy.Select(input, F.Mechanics(input), [a, b], rows, input.DiscoverySeeds, 1).Single().Party.Id);
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(input, F.Mechanics(input), [a, b], rows, input.DiscoverySeeds, 1, "unknown"));
    }

    sealed class HealthRuntime(string mode = "complete") : ITowerRefinementComparisonRuntime
    {
        internal readonly Runtime Inner = new(mode);
        public void CreateSharedExecutable(string path, long bytes, CancellationToken token) => Inner.CreateSharedExecutable(path, bytes, token);
        public void VerifySharedExecutable(string path, CancellationToken token) => Inner.VerifySharedExecutable(path, token);
        public Task<BossDiscoveryRunReport> Discover(TowerBossDiscoveryDefinition d, string path, TowerBulkOptions options, CancellationToken token) => Inner.Discover(d, path, options, token);
        public Task<BossDiscoveryRunReport> VerifyDiscovery(TowerBossDiscoveryDefinition d, string path, CancellationToken token) => Inner.VerifyDiscovery(d, path, token);
        public async Task Balance(TowerBalanceDefinition d, string path, TowerBulkOptions options, CancellationToken token)
        {
            // The fixture emits fabricated S/C journal events only, never combat engine entry.
            await Inner.Balance(d, path, options, token);
            if (d.Id == "comparison-screen") File.WriteAllText(Path.Combine(path, "evidence.json"), JsonSerializer.Serialize(Evidence(d), HarnessJson.Options));
        }
        public Task<IReadOnlyList<TowerBalanceEvidence>> VerifyBalance(TowerBalanceDefinition d, string path, CancellationToken token) => Inner.VerifyBalance(d, path, token);
        public async Task<IReadOnlyList<TowerRefinementSelectionHealth>> VerifySelectionHealth(TowerBalanceDefinition d, string path, CancellationToken token)
        {
            Assert.Equal("comparison-screen", d.Id); Assert.Equal("selection", Path.GetFileName(path));
            return Health(await Inner.VerifyBalance(d, path, token));
        }
    }
    static string Root()
    {
        var root = Path.Combine(Environment.GetEnvironmentVariable("TOWER_REFINEMENT_FIXTURE_ROOT") ?? Path.GetTempPath(), "tower-zero-win-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); return root;
    }
    [Fact] public async Task Execution_and_completed_reconstruction_use_the_same_version_and_preserve_accounting()
    {
        var root = Root(); var runtime = new HealthRuntime();
        var quality = await TowerRefinementComparisonRun.Execute(root, Pair(), 30, 8 * 1024 * 1024, runtime);
        var screen = HarnessJson.Read<TowerRefinementComparisonMember[]>(Path.Combine(root, "screen.json"));
        var finalists = HarnessJson.Read<TowerRefinementComparisonMember[]>(Path.Combine(root, "finalists.json"));
        Assert.All(finalists, row => Assert.Equal(screen[1].Id, row.Id));
        var family = HarnessJson.Read<TowerRefinementComparisonMember[]>(Path.Combine(root, "family.json"));
        TowerRescreenAttempts.Verify(Path.Combine(root, "attempts.bin"), 128 + screen.Length * 8 + family.Length * 32);
        Assert.Equal(HarnessJson.Hash(quality), HarnessJson.Hash(await TowerRefinementComparisonRun.Reconstruct(root, new HealthRuntime())));
        File.AppendAllText(Path.Combine(root, "finalists.json"), " ");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerRefinementComparisonRun.Reconstruct(root, new HealthRuntime()));
    }
    [Fact] public async Task Mixed_versions_fail_before_a_start_or_durable_attempt()
    {
        var root = Root(); var runtime = new HealthRuntime(); var definitions = Pair(); definitions[1] = Pair(Model.FreshFirstVersion)[1];
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerRefinementComparisonRun.Execute(root, definitions, 30, 8 * 1024 * 1024, runtime));
        Assert.Empty(runtime.Inner.Calls); Assert.Empty(Directory.EnumerateFileSystemEntries(root));
    }

    [Fact] public void Changing_the_requested_version_cannot_reuse_old_authorization_or_allocate()
    {
        var root = Root(); var input = Path.Combine(root, "literal-input.json");
        var preflight = new TowerRefinementPreflightRequest(root, input, input, input, input, input, 1,
            new Dictionary<string, string> { [input] = new string('a', 64) }, Model.FreshFirstVersion);
        var request = new TowerRefinementLaunchRequest(preflight, root, Path.Combine(root, "study"), 7, 10, 60, 16 * 1048576,
            new string('b', 64), HarnessJson.Hash(ExecutionIdentity.Current()));
        var permit = new TowerRefinementLaunchAuthorization(HarnessJson.Hash(request), request.ProtocolHash, 45, 288, 0);
        var changed = request with { Preflight = preflight with { ComparisonVersion = Model.ZeroWinVersion } };
        var inspected = false; var allocated = false;
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.BindCore(changed, permit,
            _ => { inspected = true; throw new InvalidOperationException("Must fail before inspection."); }, (_, _) => { }, default,
            (_, _) => { allocated = true; throw new InvalidOperationException("Must fail before allocation."); }));
        Assert.False(inspected); Assert.False(allocated); Assert.False(Directory.Exists(changed.StudyRoot));
    }

    [Theory]
    [InlineData("overrun", 64)]
    [InlineData("undercharge", 63)]
    public async Task Opt_in_preserves_attempt_caps_and_durable_charges(string mode, int charged)
    {
        var root = Root(); var runtime = new HealthRuntime(mode);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerRefinementComparisonRun.Execute(root, Pair(), 30, 8 * 1024 * 1024, runtime));
        Assert.Equal(charged, File.ReadAllBytes(Path.Combine(root, "attempts.bin")).Count(b => b == 'S'));
        Assert.DoesNotContain("discovery-1", runtime.Inner.Calls); Assert.False(File.Exists(Path.Combine(root, "finalists.json")));
    }
    [Fact] public async Task Opt_in_cancellation_preserves_completed_charge_and_forbids_retry()
    {
        var root = Root(); using var stop = new CancellationTokenSource(); var runtime = new HealthRuntime("cancel-completion");
        runtime.Inner.Cancel = stop;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerRefinementComparisonRun.Execute(root, Pair(), 30, 8 * 1024 * 1024, runtime, stop.Token));
        Assert.Equal(new byte[] { (byte)'S', (byte)'C' }, File.ReadAllBytes(Path.Combine(root, "attempts.bin")));
        var again = new HealthRuntime();
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerRefinementComparisonRun.Execute(root, Pair(), 30, 8 * 1024 * 1024, again));
        Assert.Empty(again.Inner.Calls);
    }
}
