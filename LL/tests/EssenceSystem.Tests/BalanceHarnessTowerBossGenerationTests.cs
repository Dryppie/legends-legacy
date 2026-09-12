using System.Numerics;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossGenerationTests
{
    private static readonly Lazy<TowerBossDiscoveryDefinition> Base = new(() => BalanceHarnessTowerBossDiscoveryContractTests.Definition());
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));

    private static BossDiscoveryInputs Input(int partySize = 2, int candidates = 64, params BossDiscoveryEssence[] pool)
    {
        var input = TowerBossDiscovery.GenerationInputs(Base.Value);
        return input with { RequiredPartySize = partySize,
            AllowedEssences = pool.Length == 0 ? new[] { "a", "b", "c", "d", "enabler", "consumer", "g", "h" }.Select(id => new BossDiscoveryEssence(id, id)).ToArray() : pool,
            Generation = input.Generation with { CandidatesPerArm = candidates, MaximumAttemptsPerArm = 2048, Seeds = [17] },
            EquipmentContexts = input.EquipmentContexts.ToDictionary(p => p.Key, p => (IReadOnlyList<BossDiscoveryCharacterBudget>)p.Value.Take(partySize).ToArray()),
            ShortlistCandidates = Math.Min(8, candidates * 2) };
    }

    private static BossGenerationMechanics Mechanics(BossDiscoveryInputs input, bool pairs = true, bool sameOwner = false) => new(input.Floor,
        ["focused-damage", "protection"], input.AllowedEssences.Select(e => new TowerEssenceMechanics(e.Id, e.Id, e.Family, [], [],
            e.Id == "enabler" ? ["intent:sustain"] : e.Id == "consumer" ? ["intent:protection"] : ["intent:focused-damage"])).ToArray(),
        pairs ? [new("enabler", "consumer", "event:synthetic-support", "effect:producer", "trigger:listener",
            sameOwner ? "same-owner-or-explicit-recipient-required" : "recipient-and-trigger-scope-unverified", ["Synthetic interaction oracle, not game evidence."])] : [],
        TowerBossInventory.SourceFiles.ToDictionary(file => file, file => input.ContentHashes[file]));

    internal static BossDiscoveryMeasurement Measure(BossDiscoveryInputs input, PartyChoice party, int wins, double health = 50, double survival = 40)
    {
        var cells = input.DiscoverySeeds.Select(p => new PartyFloorScore(p.Key, input.Floor,
            p.Value.Select((_, i) => i < wins).ToArray(), 0, health, survival, p.Value.Select(seed => p.Key + "/" + seed).ToArray())).ToArray();
        return new(party.Id, TowerBossGeneration.Fitness(input, cells, 100), cells, new(0, .5, 0, 0, 0));
    }

    private static IEnumerable<string[]> Ordered(IReadOnlyList<string> pool, int count)
    {
        if (count == 0) { yield return []; yield break; }
        foreach (var id in pool)
        foreach (var rest in Ordered(pool.Where(x => x != id).ToArray(), count - 1)) yield return new[] { id }.Concat(rest).ToArray();
    }

    [Fact]
    public void Uniform_sampler_matches_an_exhaustive_weighted_family_oracle_and_keeps_order()
    {
        var pool = new[] { new BossDiscoveryEssence("a1", "a"), new("a2", "a"), new("b", "b"), new("c", "c"), new("d", "d"), new("e", "e") };
        var input = Input(1, 64, pool); var sampler = new TowerBossPartyGenerator(input, Mechanics(input, pairs: false));
        var families = pool.ToDictionary(e => e.Id, e => e.Family);
        var oracle = Ordered(pool.Select(e => e.Id).ToArray(), 4).Where(ids => ids.Select(id => families[id]).Distinct().Count() == 4)
            .Select(ids => string.Join(",", ids)).ToHashSet();
        Assert.Equal(216, oracle.Count);
        Assert.Equal(new BigInteger(oracle.Count), sampler.LegalOrderedCharacterCount);
        var counts = new Dictionary<string, int>(); var random = new Random(5197);
        for (var i = 0; i < 21600; i++)
        {
            var tuple = string.Join(",", sampler.UniformCharacter(random));
            Assert.Contains(tuple, oracle); counts[tuple] = counts.GetValueOrDefault(tuple) + 1;
        }
        Assert.Equal(oracle.Count, counts.Count);
        Assert.All(counts.Values, n => Assert.InRange(n, 55, 155));
    }

    [Fact]
    public async Task Tiny_complete_space_recovers_exhaustive_best_and_reports_attempt_exhaustion_honestly()
    {
        var input = Input(1, 30, new[] { "a", "b", "c", "d" }.Select(id => new BossDiscoveryEssence(id, id)).ToArray());
        var oracle = Ordered(["a", "b", "c", "d"], 4).Select(ids => TowerPartySelection.Choice("oracle", new Dictionary<int, IReadOnlyList<string>> { [1] = ids })).ToArray();
        BossDiscoveryMeasurement Score(PartyChoice party) => Measure(input, party, 0, Convert.ToUInt32(party.Id[..6], 16) / (double)0xffffff * 100);
        var result = await TowerBossGeneration.RunAsync(input, Mechanics(input, pairs: false), (party, _, _) => Task.FromResult(Score(party)));
        Assert.Equal("Incomplete", result.Status);
        Assert.All(result.Arms, arm => { Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Equal(24, arm.Evaluations.Count); Assert.Equal(2048, arm.Proposals.Count); });
        Assert.Equal(TowerBossGeneration.Rank(oracle.Select(Score)).First().Id, result.DiscoveryShortlist[0].Id);
        Assert.All(result.Arms.SelectMany(a => a.Evaluations), row => Assert.Equal(0, row.Fitness.WorstContextWinRate));
        Assert.All(result.Arms[0].Proposals, proposal => { Assert.Equal("fresh-random", proposal.Provenance.Operator); Assert.Empty(proposal.Provenance.ParentIds); });
    }

    [Fact]
    public async Task Coordinated_policy_preserves_the_legacy_comparator_and_records_new_parentage()
    {
        var input = Input(candidates: 128);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var legacy = await TowerBossGeneration.RunAsync(input, Mechanics(input), Score);
        var coordinated = input with { Generation = input.Generation with {
            PolicyVersion = TowerBossGeneration.CoordinatedVersion, Methods = TowerBossGeneration.CoordinatedMethods } };
        var result = await TowerBossGeneration.RunAsync(coordinated, Mechanics(coordinated), Score);
        Assert.Equal("Complete", result.Status);
        Assert.Equal(TowerBossGeneration.CoordinatedVersion, result.Version);
        Assert.Equal(HarnessJson.Hash(legacy.Arms.Single(a => a.Method == "constructive-joint")),
            HarnessJson.Hash(result.Arms.Single(a => a.Method == "constructive-joint")));
        var arm = result.Arms.Single(a => a.Method == "coordinated-joint");
        Assert.Contains(arm.Proposals, p => p.Provenance.Operator == "broadcast-core" && p.Provenance.ParentIds.Count == 1);
        Assert.Contains(arm.Proposals, p => p.Provenance.Operator == "fresh-coordinated" && p.Provenance.ParentIds.Count == 0);
        Assert.All(arm.Proposals, p => Assert.Empty(p.Provenance.ReferenceIds));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBossGeneration.RunAsync(coordinated, Mechanics(coordinated), Score)));
    }

    [Fact]
    public void Shared_cores_respect_family_and_owned_copy_limits_without_mutating_the_parent()
    {
        var input = Input(); var free = new TowerBossPartyGenerator(input, Mechanics(input));
        var parent = TowerPartySelection.Choice("generated-parent", new Dictionary<int, IReadOnlyList<string>> {
            [1] = ["a", "b", "c", "d"], [2] = ["enabler", "consumer", "g", "h"] });
        var before = HarnessJson.Hash(parent);
        var shared = free.BroadcastCore(new Random(1), parent);
        Assert.Null(shared.Rejection); Assert.Null(free.Invalid(shared.Party!));
        Assert.NotEmpty(shared.Party!.Builds[1].Intersect(shared.Party.Builds[2]));
        var limited = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var owned = new TowerBossPartyGenerator(limited, Mechanics(limited));
        Assert.Equal("owned-copies-exceeded", owned.BroadcastCore(new Random(1), parent).Rejection);
        Assert.Equal(before, HarnessJson.Hash(parent));
        for (var seed = 0; seed < 20; seed++)
        {
            var proposal = owned.FreshCoordinated(new Random(seed));
            if (proposal.Rejection is null) Assert.Null(owned.Invalid(proposal.Party!));
        }
    }

    [Fact]
    public void Coordinated_operator_provenance_requires_its_explicit_policy_and_method()
    {
        var d = Base.Value with { Generation = Base.Value.Generation with {
            PolicyVersion = TowerBossGeneration.CoordinatedVersion, Methods = TowerBossGeneration.CoordinatedMethods } };
        TowerBossDiscovery.Validate(d);
        var seed = d.Generation.Seeds[0];
        var fresh = new BossDiscoveryProvenance("generated-0", seed, "coordinated-joint", "fresh-coordinated", [], []);
        var spread = new BossDiscoveryProvenance("generated-1", seed, "coordinated-joint", "broadcast-core", [fresh.Id], []);
        TowerBossDiscovery.ValidateProvenance(d, [fresh, spread]);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(Base.Value, [fresh, spread]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh with { Method = "constructive-joint" }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh, spread with { ParentIds = [] }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh with { ReferenceIds = ["hidden-reference"] }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossDiscovery.Methods } }));
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Coordinated_generation_fills_the_declared_floor_budget_with_legal_shared_cores(int floor, int slots)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        var input = TowerBossDiscovery.GenerationInputs(d);
        var sampler = new TowerBossPartyGenerator(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value));
        for (var seed = 0; seed < 8; seed++)
        {
            var choice = sampler.FreshCoordinated(new Random(seed));
            Assert.Null(choice.Rejection); TowerBossDiscovery.ValidateParty(d, choice.Party!);
            Assert.Contains(choice.Party!.Builds.Values.SelectMany(x => x).GroupBy(x => x), g => g.Count() > 1);
        }
    }

    [Fact]
    public async Task Every_operator_is_recorded_and_every_fourth_refinement_attempt_is_fresh()
    {
        var input = Input(candidates: 128);
        var result = await TowerBossGeneration.RunAsync(input, Mechanics(input), (party, _, _) => Task.FromResult(Measure(input, party, 8)));
        Assert.Equal("Complete", result.Status);
        var arm = result.Arms.Single(a => a.Method == "constructive-joint");
        var first32 = arm.Proposals.TakeWhile(p => p.Provenance.ParentIds.Count == 0).ToArray();
        Assert.Equal(32, first32.Count(p => p.Result == "evaluated"));
        Assert.All(first32, p => Assert.Equal("fresh-constructive", p.Provenance.Operator));
        var remaining = arm.Proposals.Skip(first32.Length).ToArray();
        for (var i = 3; i < remaining.Length; i += 4) Assert.Equal("fresh-constructive", remaining[i].Provenance.Operator);
        Assert.All(TowerBossGeneration.Operators, op => Assert.Contains(arm.Proposals, p => p.Provenance.Operator == op));
        var known = new HashSet<string>();
        foreach (var p in arm.Proposals)
        {
            Assert.All(p.Provenance.ParentIds, parent => Assert.Contains(parent, known));
            Assert.Empty(p.Provenance.ReferenceIds);
            if (p.Result == "evaluated") known.Add(p.Provenance.Id);
        }
        Assert.Equal(1, result.Arms.SelectMany(a => a.Evaluations).Max(r => r.Fitness.WorstContextWinRate));
        Assert.Equal(1, result.Arms.SelectMany(a => a.Evaluations).First(r => r.Id == result.DiscoveryShortlist[0].Id).Fitness.WorstContextWinRate);
    }

    [Fact]
    public async Task Coordinated_interaction_crosses_a_single_change_valley_without_assuming_same_owner_compatibility()
    {
        var input = Input(candidates: 48); var mechanics = Mechanics(input); var sampler = new TowerBossPartyGenerator(input, mechanics);
        var parent = TowerPartySelection.Choice("synthetic-parent", new Dictionary<int, IReadOnlyList<string>> { [1] = ["a", "b", "c", "d"], [2] = ["a", "b", "c", "d"] });
        int Wins(PartyChoice p) => p.Builds[1].Contains("enabler") && p.Builds[2].Contains("consumer") ? 8 : 0;
        var combined = sampler.PairReplacement(parent, mechanics.Interactions[0], 1, 2, 0, 0);
        Assert.Null(combined.Rejection); Assert.Equal(8, Wins(combined.Party!));
        foreach (var slot in new[] { 1, 2 })
        {
            var builds = parent.Builds.ToDictionary(p => p.Key, p => p.Value);
            builds[slot] = combined.Party!.Builds[slot];
            Assert.Equal(0, Wins(TowerPartySelection.Choice("single-ingredient", builds)));
        }
        var oracle = from left in input.AllowedEssences from right in input.AllowedEssences
            select TowerPartySelection.Choice("two-position-oracle", new Dictionary<int, IReadOnlyList<string>> {
                [1] = [left.Id, "b", "c", "d"], [2] = [right.Id, "b", "c", "d"] });
        Assert.Equal(oracle.Where(p => sampler.Invalid(p) is null).Max(Wins), Wins(combined.Party!));
        var restricted = Mechanics(input, sameOwner: true); var restrictedSampler = new TowerBossPartyGenerator(input, restricted);
        Assert.Equal("same-owner-required", restrictedSampler.PairReplacement(parent, restricted.Interactions[0], 1, 2, 0, 0).Rejection);
        Assert.Null(restrictedSampler.PairReplacement(parent, restricted.Interactions[0], 1, 1, 0, 1).Rejection);
        var result = await TowerBossGeneration.RunAsync(input, mechanics, (p, _, _) => Task.FromResult(Measure(input, p, Wins(p))));
        Assert.Equal("Complete", result.Status);
        Assert.Contains(result.Arms.SelectMany(a => a.Proposals), p => p.Provenance.Operator == "cross-character" && p.Interaction is not null && p.Result == "evaluated");
        Assert.Equal(8, Wins(result.DiscoveryShortlist[0]));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task References_cannot_change_proposals_scores_or_shortlist_even_when_a_reference_is_the_generated_winner(bool coordinated)
    {
        var d = Base.Value with { Generation = Base.Value.Generation with { CandidatesPerArm = 48, Seeds = [431] } };
        if (coordinated) d = d with { Generation = d.Generation with {
            PolicyVersion = TowerBossGeneration.CoordinatedVersion, Methods = TowerBossGeneration.CoordinatedMethods } };
        async Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition definition)
        {
            var input = TowerBossDiscovery.GenerationInputs(definition);
            return await TowerBossGeneration.RunAsync(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value),
                (p, _, _) => Task.FromResult(Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9)));
        }
        var original = await Run(d);
        Assert.Equal("Complete", original.Status);
        var reference = BalanceHarnessTowerBossDiscoveryContractTests.Reference();
        var winner = new BossBenchmarkReference("converged-generated-winner", d.Contexts[0].Id,
            TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, original.DiscoveryShortlist[0], []), "Explicit reference registered after this test generated the same recipe", new string('b', 64));
        var registered = d with { References = [reference, winner] };
        Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(await Run(registered)));
        Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(await Run(registered with { References = [winner, reference] })));
        Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(await Run(registered with { References = [] })));
        TowerBossDiscovery.ValidateProvenance(registered, original.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
    }

    [Fact]
    public async Task Owned_copies_are_enforced_for_both_methods_and_repeated_support_remains_legal()
    {
        var input = Input(candidates: 24);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var sampler = new TowerBossPartyGenerator(input, Mechanics(input));
        var result = await TowerBossGeneration.RunAsync(input, Mechanics(input), (p, _, _) => Task.FromResult(Measure(input, p, 0)));
        Assert.Equal("Complete", result.Status);
        Assert.All(result.Arms.SelectMany(a => a.Proposals).Where(p => p.Result == "evaluated"), p => {
            Assert.Null(sampler.Invalid(p.Party!)); Assert.All(p.Party!.Builds.Values.SelectMany(ids => ids).GroupBy(id => id), g => Assert.Single(g)); });
        Assert.Contains(result.Arms[0].Proposals, p => p.Result == "owned-copies-exceeded");
        var free = input with { OwnedCopies = null };
        var unrestricted = new TowerBossPartyGenerator(free, Mechanics(free));
        var repeated = TowerPartySelection.Choice("repeated-support", new Dictionary<int, IReadOnlyList<string>> {
            [1] = ["enabler", "b", "c", "d"], [2] = ["enabler", "b", "c", "d"] });
        Assert.Null(unrestricted.Invalid(repeated));
        Assert.Equal("owned-copies-exceeded", sampler.Invalid(repeated));
    }

    [Theory]
    [InlineData(5,5)] [InlineData(11,7)] [InlineData(15,10)]
    public void Fresh_parties_fill_every_production_position_without_repeating_a_five_character_template(int floor, int slots)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        var input = TowerBossDiscovery.GenerationInputs(d);
        var sampler = new TowerBossPartyGenerator(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value));
        foreach (var constructive in new[] { false, true })
        {
            var party = sampler.Fresh(new Random(7104), constructive).Party!;
            TowerBossDiscovery.ValidateParty(d, party);
            Assert.Equal(d.RequiredPartySize, party.Builds.Count);
            Assert.Equal(d.RequiredPartySize, party.Builds.Values.Select(ids => string.Join(",", ids)).Distinct().Count());
            var content = new OfflineContent(TestContentPaths.FindApiRoot(), new());
            var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, party, [912311]);
            var prepared = new TowerBattleRunner(TestContentPaths.FindApiRoot(), content).CreateInput(scenario, 912311, new(), 10);
            Assert.Equal(d.RequiredPartySize, prepared.Party.Count);
        }
    }

    [Fact]
    public async Task Fitness_uses_worst_context_then_progress_survival_and_duration_without_a_balance_ceiling()
    {
        var input = Input();
        var first = input.DiscoverySeeds.First();
        input = input with { DiscoverySeeds = new Dictionary<string, IReadOnlyList<int>> { [first.Key] = first.Value, ["second"] = Enumerable.Range(100, 8).ToArray() },
            EquipmentContexts = new Dictionary<string, IReadOnlyList<BossDiscoveryCharacterBudget>>(input.EquipmentContexts) { ["second"] = input.EquipmentContexts[first.Key] } };
        var party = new TowerBossPartyGenerator(input, Mechanics(input)).Fresh(new Random(5), false).Party!;
        var cells = Measure(input, party, 8).Cells.ToArray();
        cells[1] = cells[1] with { Clears = Enumerable.Range(0, 8).Select(i => i < 2).ToArray() };
        Assert.Equal(.25, TowerBossGeneration.Fitness(input, cells, 100).WorstContextWinRate);
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.Fitness(input, cells.Take(1).ToArray(), 100));
        var strong = Measure(input, party, 8); var weak = strong with { Id = "weaker", Fitness = strong.Fitness with { WorstContextWinRate = .375, GuardianHealth = 0 } };
        Assert.Equal(strong.Id, TowerBossGeneration.Rank([weak, strong]).First().Id);
        var invalid = await TowerBossGeneration.RunAsync(input, Mechanics(input), (p, _, _) => Task.FromResult(Measure(input, p, 0) with { Fitness = strong.Fitness }));
        Assert.Equal("Invalid", invalid.Status); Assert.Empty(invalid.DiscoveryShortlist);
        using var cancelled = new CancellationTokenSource(); var calls = 0;
        var stopped = await TowerBossGeneration.RunAsync(input, Mechanics(input), (p, _, ct) => {
            calls++; if (calls == 2) { cancelled.Cancel(); ct.ThrowIfCancellationRequested(); }
            return Task.FromResult(Measure(input, p, 0));
        }, cancelled.Token);
        Assert.Equal("Cancelled", stopped.Status); Assert.Single(stopped.Arms[0].Evaluations);
        Assert.Equal("Cancelled", stopped.Arms[0].Proposals[^1].Result);
    }
}
