using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

// Only fabricated complete-party measurements are used here. These tests never allocate
// a combat schedule, materialize a combat input, or invoke the combat engine.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessSuppliedCompositionTests : IDisposable
{
    private static readonly int[] FrozenConstructionSeeds = [17, 31, 47];
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Supplied-composition synthetic test entered combat.")).Activate();
    public BalanceHarnessSuppliedCompositionTests(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;
    public void Dispose() => guard.Dispose();

    private static PartyChoice Party(params string[][] owners) => TowerPartySelection.Choice("synthetic",
        owners.Select((ids, index) => (ids, index)).ToDictionary(p => p.index + 1,
            p => (IReadOnlyList<string>)p.ids.Order(StringComparer.Ordinal).ToArray()));

    private static TowerBossDiscoveryDefinition Source(int owners = 2, int pool = 12, int candidates = 24,
        int attempts = 96, params PartyChoice[] starts)
    {
        var input = F.Input(owners: owners, poolSize: pool, candidates: candidates, attempts: attempts);
        var definition = F.Definition(input);
        if (starts.Length == 0)
            starts = [Party(Enumerable.Range(0, owners).Select(_ => new[] { "e00", "e01", "e02", "e03" }).ToArray())];
        return definition with { References = starts.Select((party, index) => new BossBenchmarkReference(
            "saved-" + index, "fixture", TowerBossDiscovery.Scenario(definition, "fixture", party, []),
            "Synthetic archived composition; any historical score is unusable.", new string('d', 64))).ToArray() };
    }

    private static TowerBossDiscoveryDefinition Prepared(TowerBossDiscoveryDefinition source) =>
        TowerSuppliedCompositionSearch.Prepare(source, source.References.Select(r => r.Id).ToArray());

    private static BossDiscoveryMeasurement Score(BossDiscoveryInputs input, PartyChoice party, double score)
    {
        var original = F.Measure(input, party);
        var cells = original.Cells.Select(c => c with { GuardianHealth = 100 - score * 10 }).ToArray();
        return original with { Cells = cells, Fitness = TowerBossGeneration.Fitness(input, cells, 100) };
    }

    private static BossGeneratedProposal Proposal(PartyChoice party, int ordinal = 0) => new(
        new("synthetic-" + ordinal, 17, TowerSuppliedCompositionSearch.Block, "fresh", [], []),
        party, "synthetic", null, "evaluated");

    private static BossDiscoveryInputs BinaryInput(int owners = 1) => F.Input(owners: owners, poolSize: 8,
        candidates: 32, attempts: 256) with { AllowedEssences = Enumerable.Range(0, 8)
            .Select(i => new BossDiscoveryEssence("e" + i.ToString("D2"), "family" + i % 4)).ToArray() };

    private static string[] BinaryLoadout(int bits) => Enumerable.Range(0, 4)
        .Select(bit => "e" + (bit + ((bits & (1 << bit)) == 0 ? 0 : 4)).ToString("D2"))
        .Order(StringComparer.Ordinal).ToArray();

    private sealed class ChoiceTranscript(IEnumerable<int> choices) : Random
    {
        private readonly Queue<int> remaining = new(choices);
        internal int Remaining => remaining.Count;
        public override int Next(int maxValue) => Next(0, maxValue);
        public override int Next(int minValue, int maxValue)
        {
            var value = remaining.Dequeue(); Assert.InRange(value, minValue, maxValue - 1); return value;
        }
    }

    // Enumerate an explicit witness in the sampler's finite permutation support.
    // Fisher-Yates choices are specified directly, never searched for a lucky seed.
    private static int[] ShuffleTranscript(int count, IEnumerable<int> preferred)
    {
        var prefix = preferred.ToArray();
        var desired = prefix.Concat(Enumerable.Range(0, count).Except(prefix)).ToArray();
        var current = Enumerable.Range(0, count).ToArray(); var choices = new List<int>();
        for (var position = 0; position < count - 1; position++)
        {
            var from = Array.IndexOf(current, desired[position], position);
            choices.Add(from); (current[position], current[from]) = (current[from], current[position]);
        }
        return choices.ToArray();
    }

    private static int[] RecipeTranscript(BossDiscoveryInputs input, IEnumerable<string> desired)
    {
        var pool = input.AllowedEssences.OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => e.Id).ToArray();
        return ShuffleTranscript(pool.Length, desired.Select(id => Array.IndexOf(pool, id)));
    }

    private static BossGeneratedChoice EssenceWitness(BossDiscoveryInputs input, PartyChoice parent,
        int[] replacedPositions, string[] replacements)
    {
        var random = new ChoiceTranscript(new[] { 0 }.Concat(ShuffleTranscript(4, replacedPositions))
            .Concat(RecipeTranscript(input, replacements)));
        var choice = TowerSuppliedCompositionSearch.Propose(input, parent, null, "essence-block",
            replacedPositions.Length - 2, random);
        Assert.Equal(0, random.Remaining); Assert.Null(choice.Rejection); return choice;
    }

    private static IEnumerable<BossGeneratedChoice> BlockProposals(BossDiscoveryInputs input, PartyChoice parent,
        string operation, PartyChoice? donor = null)
    {
        // Frozen before execution: three construction roots, exactly 256 emitted
        // opportunities per root. This is a support fixture, not a combat study.
        foreach (var seed in FrozenConstructionSeeds)
        {
            var random = new Random(seed);
            for (var ordinal = 0; ordinal < 256; ordinal++)
                yield return TowerSuppliedCompositionSearch.Propose(input, parent, donor, operation, ordinal, random);
        }
    }

    private static void Legal(BossDiscoveryInputs input, PartyChoice party)
    {
        Assert.Equal(Enumerable.Range(1, input.RequiredPartySize), party.Builds.Keys.Order());
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family);
        foreach (var ids in party.Builds.Values)
        {
            Assert.Equal(input.Budget.EssenceSlots, ids.Count);
            Assert.True(TowerCompositionSearch.IsCanonical(ids));
            Assert.Equal(ids.Count, ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
        if (input.OwnedCopies is not null)
            Assert.All(party.Builds.Values.SelectMany(ids => ids).GroupBy(id => id),
                group => Assert.InRange(group.Count(), 1, input.OwnedCopies.GetValueOrDefault(group.Key)));
    }

    [Fact]
    public async Task Integrated_equal_budget_search_recovers_the_enumerated_conjunction_optimum_in_two_of_three_frozen_roots()
    {
        // Frozen before execution: one zero-bit anchor, the full 16 x 16 legal
        // space, roots 17/31/47, 24 evaluations and 96 proposals per method/root.
        // Passing means at least two block roots attain the known optimum; neither
        // superiority over the comparator nor exclusively mutation-led recovery
        // is assumed. The direct witness tests isolate coordinated-operator support.
        var source = Source(owners: 2, pool: 8, candidates: 24, attempts: 96);
        source = source with { AllowedEssences = BinaryInput(owners: 2).AllowedEssences };
        var definition = Prepared(source);
        definition = definition with { Generation = definition.Generation with { Seeds = FrozenConstructionSeeds } };
        var input = TowerBossImprovement.Inputs(definition);
        int Fitness(PartyChoice party) => party.Builds.Values.Any(ids => ids.Contains("e04") && ids.Contains("e05")) ? 4
            : party.Builds.Values.Any(ids => ids.Contains("e04") || ids.Contains("e05")) ? 1 : 2;
        var oracle = (from first in Enumerable.Range(0, 16) from second in Enumerable.Range(0, 16)
            select Party(BinaryLoadout(first), BinaryLoadout(second))).ToArray();
        Assert.Equal(256, oracle.Select(p => p.Id).Distinct().Count());
        var optimum = oracle.Max(Fitness); Assert.Equal(4, optimum);
        var anchor = Assert.Single(definition.Starts).Party; Assert.Equal(2, Fitness(anchor));
        Assert.All(oracle.Where(p => TowerSuppliedCompositionSearch.Distance(anchor, p) == 2),
            neighbor => Assert.True(Fitness(neighbor) <= Fitness(anchor)));
        Assert.Equal(1, Fitness(Party(BinaryLoadout(1), BinaryLoadout(0))));
        Assert.Equal(1, Fitness(Party(BinaryLoadout(1), BinaryLoadout(2))));

        var result = await TowerSuppliedCompositionSearch.RunAsync(definition,
            F.Mechanics(input) with { Cores = null, Coverage = null },
            (party, _, token) => { token.ThrowIfCancellationRequested(); return Task.FromResult(Score(input, party, Fitness(party))); });
        var bestByArm = new Dictionary<(string Method, int Seed), int>();
        foreach (var arm in result.Arms)
        {
            var measured = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            var best = measured.Count == 0 ? 0 : measured.Values.Max(p => Fitness(p.Party!));
            bestByArm.Add((arm.Method, arm.Seed), best);
            var initialBest = arm.Proposals.Take(7).Where(p => p.Result == "evaluated")
                .Select(p => Fitness(p.Party!)).DefaultIfEmpty(0).Max();
            var retainedScores = TowerSuppliedCompositionSearch.Retain(arm.Evaluations, measured)
                .Select(id => Fitness(measured[id].Party!)).Distinct().Order().ToArray();
            var losses = string.Join(",", arm.Proposals.Where(p => p.Result != "evaluated").GroupBy(p => p.Result)
                .OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => g.Key + "=" + g.Count()));
            output.WriteLine($"conjunction method={arm.Method} root={arm.Seed} oracle={optimum} best={best} initialBest={initialBest} "
                + $"evaluations={arm.Evaluations.Count}/24 proposals={arm.Proposals.Count}/96 "
                + $"retainedScores=[{string.Join(",", retainedScores)}] losses=[{losses}] stop={arm.StopReason}");
        }
        Assert.Equal("Complete", result.Status); Assert.Null(result.Error); Assert.Equal(6, result.Arms.Count);
        Assert.All(result.Arms, arm => {
            Assert.Equal(24, arm.Evaluations.Count); Assert.InRange(arm.Proposals.Count, 24, 96);
            Assert.All(arm.Proposals.Where(p => p.Result == "evaluated"), p => Legal(input, p.Party!));
            Assert.Equal(100 - 10 * bestByArm[(arm.Method, arm.Seed)], arm.Evaluations.Min(r => r.Fitness.GuardianHealth));
        });
        foreach (var root in FrozenConstructionSeeds)
        {
            var baseline = result.Arms.Single(a => a.Method == TowerSuppliedCompositionSearch.Baseline && a.Seed == root);
            var block = result.Arms.Single(a => a.Method == TowerSuppliedCompositionSearch.Block && a.Seed == root);
            Assert.Equal(baseline.Proposals.Take(7).Select(p => p.Party?.Id), block.Proposals.Take(7).Select(p => p.Party?.Id));
        }
        Assert.True(FrozenConstructionSeeds.Count(root => bestByArm[(TowerSuppliedCompositionSearch.Block, root)] == optimum) >= 2,
            "The predeclared integrated gate requires oracle recovery in at least two of the three frozen block roots.");
    }

    [Fact]
    public void Two_and_three_essence_conjunctions_cross_deceptive_barriers_including_held_out_bit_placements()
    {
        var input = BinaryInput(); var parent = Party(BinaryLoadout(0));
        var oracle = Enumerable.Range(0, 16).Select(bits => Party(BinaryLoadout(bits))).ToArray();
        var proposals = BlockProposals(input, parent, "essence-block").ToArray();
        var measured = proposals.Where(p => p.Rejection is null).Select(p => p.Party!).ToArray();
        Assert.NotEmpty(measured);
        Assert.All(measured, party => {
            Legal(input, party);
            Assert.InRange(party.Builds[1].Except(parent.Builds[1]).Count(), 2, 3);
        });
        foreach (var required in new[] { new[] { 0, 1 }, new[] { 1, 3 }, new[] { 0, 1, 2 }, new[] { 1, 2, 3 } })
        {
            int Fitness(PartyChoice party)
            {
                var changed = required.Count(bit => party.Builds[1].Contains("e" + (bit + 4).ToString("D2")));
                return changed == 0 ? 2 : changed == required.Length ? 4 : 1;
            }
            Assert.Equal(2, Fitness(parent)); Assert.Equal(4, oracle.Max(Fitness));
            var singleNeighbors = oracle.Where(p => TowerSuppliedCompositionSearch.Distance(parent, p) == 2).ToArray();
            Assert.All(singleNeighbors, neighbor => Assert.True(Fitness(neighbor) <= Fitness(parent)));
            var witness = EssenceWitness(input, parent, required,
                required.Select(bit => "e" + (bit + 4).ToString("D2")).ToArray()).Party!;
            Assert.Equal(4, Fitness(witness)); Legal(input, witness);
            Assert.All(required, bit => Assert.DoesNotContain("e" + bit.ToString("D2"), witness.Builds[1]));
        }
        Assert.Equal(parent.Id, Party(BinaryLoadout(0)).Id);
    }

    [Fact]
    public void Fresh_exploration_covers_the_tiny_legal_space_without_cores_roles_or_internal_loadout_uniqueness()
    {
        var input = BinaryInput();
        var expected = Enumerable.Range(0, 16).Select(bits => Party(BinaryLoadout(bits)).Id).ToHashSet();
        var observed = new HashSet<string>();
        foreach (var bits in Enumerable.Range(0, 16))
        {
            var random = new ChoiceTranscript(RecipeTranscript(input, BinaryLoadout(bits)));
            var choice = TowerSuppliedCompositionSearch.Fresh(input, random);
            Assert.Equal(0, random.Remaining); Assert.Null(choice.Rejection);
            Legal(input, choice.Party!); observed.Add(choice.Party!.Id);
        }
        Assert.Equal(16, expected.Count); Assert.True(expected.SetEquals(observed));
        // This optimum contains neither e00 nor e01, the hypothetical authored core,
        // and no capability metadata is even available to this constructor.
        Assert.Contains(Party(BinaryLoadout(15)).Id, observed);
        var repeatedInput = F.Input(owners: 10, poolSize: 4, candidates: 8, attempts: 32);
        var repeated = TowerSuppliedCompositionSearch.Fresh(repeatedInput, new Random(FrozenConstructionSeeds[0]));
        Assert.Null(repeated.Rejection); Legal(repeatedInput, repeated.Party!);
        Assert.Single(repeated.Party!.Builds.Values.Select(ids => string.Join(",", ids)).Distinct());
    }

    [Fact]
    public void Whole_character_blocks_can_change_complementary_owners_in_both_subgroup_placements()
    {
        var input = BinaryInput(owners: 10);
        var parent = Party(Enumerable.Range(0, 10).Select(_ => BinaryLoadout(0)).ToArray());
        var proposals = BlockProposals(input, parent, "character-block").Where(p => p.Rejection is null)
            .Select(p => p.Party!).ToArray();
        Assert.NotEmpty(proposals);
        Assert.All(proposals, child => {
            Legal(input, child);
            Assert.Equal(2, child.Builds.Count(p => !p.Value.SequenceEqual(parent.Builds[p.Key])));
        });
        foreach (var partner in new[] { 2, 6 })
        {
            int Fitness(PartyChoice party)
            {
                var changed = new[] { 1, partner }.Count(slot => party.Builds[slot].Contains("e04"));
                // Other owners are decoys and cannot substitute for the correctly placed pair.
                return changed == 0 ? 2 : changed == 2 ? 4 : 1;
            }
            Assert.Equal(2, Fitness(parent));
            foreach (var slot in new[] { 1, partner })
            {
                var one = parent.Builds.ToDictionary(p => p.Key, p => p.Value);
                one[slot] = BinaryLoadout(1);
                Assert.Equal(1, Fitness(TowerPartySelection.Choice("single-owner", one)));
            }
            // Owner 1 is first in both subgroup-pair enumerations, so the first
            // pair is (1,2) within the subgroup and (1,6) across subgroups.
            var random = new ChoiceTranscript(new[] { 0 }.Concat(RecipeTranscript(input, BinaryLoadout(1)))
                .Concat(RecipeTranscript(input, BinaryLoadout(1))));
            var witness = TowerSuppliedCompositionSearch.Propose(input, parent, null, "character-block",
                partner == 2 ? 0 : 1, random);
            Assert.Equal(0, random.Remaining); Assert.Null(witness.Rejection);
            Assert.Equal(4, Fitness(witness.Party!)); Legal(input, witness.Party!);
            Assert.All(witness.Party!.Builds.Where(p => p.Key != 1 && p.Key != partner),
                p => Assert.Equal(parent.Builds[p.Key], p.Value));
        }
    }

    [Fact]
    public void Donor_blocks_mix_distinct_parties_without_copying_either_complete_donor()
    {
        var input = BinaryInput(owners: 10);
        var parent = Party(Enumerable.Range(0, 10).Select(_ => BinaryLoadout(0)).ToArray());
        var donor = Party(Enumerable.Range(0, 10).Select(_ => BinaryLoadout(15)).ToArray());
        var proposals = BlockProposals(input, parent, "donor-block", donor).Where(p => p.Rejection is null)
            .Select(p => p.Party!).ToArray();
        Assert.NotEmpty(proposals);
        Assert.All(proposals, child => {
            Legal(input, child); Assert.NotEqual(parent.Id, child.Id); Assert.NotEqual(donor.Id, child.Id);
            Assert.Contains(child.Builds, p => p.Value.SequenceEqual(parent.Builds[p.Key]));
            Assert.Contains(child.Builds, p => p.Value.SequenceEqual(donor.Builds[p.Key]));
        });
        var fullRandom = new ChoiceTranscript(ShuffleTranscript(10, []).Append(8));
        var fullBlock = TowerSuppliedCompositionSearch.Propose(input, parent, donor, "donor-block", 2, fullRandom);
        Assert.Equal(0, fullRandom.Remaining); Assert.Null(fullBlock.Rejection);
        Assert.Equal(8, fullBlock.Party!.Builds.Count(p => p.Value.SequenceEqual(donor.Builds[p.Key])));
        var repeated = TowerSuppliedCompositionSearch.Propose(input, parent, parent, "donor-block", 0, new Random(17));
        Assert.NotNull(repeated.Rejection);
    }

    [Fact]
    public async Task Both_methods_reevaluate_identical_starts_and_fresh_batches_with_equal_caps_and_deterministic_lineage()
    {
        var firstStart = Party(["e00", "e01", "e02", "e03"], ["e04", "e05", "e06", "e07"]);
        var secondStart = Party(["e04", "e05", "e06", "e07"], ["e00", "e01", "e02", "e03"]);
        var definition = Prepared(Source(starts: [firstStart, secondStart]));
        definition = definition with { Generation = definition.Generation with { Seeds = FrozenConstructionSeeds } };
        var input = TowerBossImprovement.Inputs(definition);
        var before = HarnessJson.Hash(definition);
        var calls = 0;
        async Task<BossGenerationResult> Run() => await TowerSuppliedCompositionSearch.RunAsync(definition,
            F.Mechanics(input) with { Cores = null, Coverage = null }, (party, _, token) => {
                token.ThrowIfCancellationRequested(); calls++;
                return Task.FromResult(Score(input, party, definition.Starts.Any(s => s.Party.Id == party.Id) ? 1 : 4));
            });
        var result = await Run();
        Assert.Equal("Complete", result.Status); Assert.Null(result.Error);
        Assert.Equal(144, calls);
        Assert.Equal(6, result.Arms.Count);
        var generator = new TowerBossPartyGenerator(input, F.Mechanics(input));
        foreach (var pair in result.Arms.GroupBy(a => a.Seed))
        {
            var baseline = pair.Single(a => a.Method == TowerSuppliedCompositionSearch.Baseline);
            var block = pair.Single(a => a.Method == TowerSuppliedCompositionSearch.Block);
            Assert.Equal(baseline.Proposals.Take(8).Select(p => p.Party!.Id), block.Proposals.Take(8).Select(p => p.Party!.Id));
            var commonFresh = Math.Min(baseline.Proposals.Count, block.Proposals.Count);
            for (var ordinal = 8; ordinal < commonFresh; ordinal++)
                if (baseline.Proposals[ordinal].Provenance.ParentIds.Count == 0)
                {
                    Assert.Empty(block.Proposals[ordinal].Provenance.ParentIds);
                    Assert.Equal(baseline.Proposals[ordinal].Party?.Id, block.Proposals[ordinal].Party?.Id);
                }
        }
        foreach (var arm in result.Arms)
        {
            Assert.Equal(24, arm.Evaluations.Count); Assert.InRange(arm.Proposals.Count, 24, 96);
            Assert.Equal(24, arm.Evaluations.Select(e => e.Id).Distinct().Count());
            Assert.Equal(definition.Starts.Select(s => s.Party.Id).Order(), arm.Evaluations.Take(2).Select(e => e.Id).Order());
            Assert.All(arm.Evaluations.Take(2), row => Assert.Equal(90, row.Fitness.GuardianHealth));
            var prior = new Dictionary<string, BossGeneratedProposal>();
            foreach (var proposal in arm.Proposals)
            {
                if (proposal.Party is not null)
                    Assert.All(proposal.Party.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
                Assert.NotNull(proposal.Supplied);
                Assert.Equal(32, proposal.Supplied.MaximumConstructionChecks);
                Assert.InRange(proposal.Supplied.PopulationIds.Count, 0, arm.Method == TowerSuppliedCompositionSearch.Block ? 8 : 4);
                Assert.All(proposal.Supplied.PopulationIds, id => Assert.Contains(prior.Values,
                    p => p.Result == "evaluated" && p.Party!.Id == id));
                if (proposal.Provenance.Operator == "supplied")
                    Assert.Single(proposal.Provenance.ReferenceIds);
                else
                {
                    foreach (var parent in proposal.Provenance.ParentIds)
                        Assert.Equal("evaluated", prior[parent].Result);
                    var inherited = proposal.Provenance.ParentIds.SelectMany(id => prior[id].Provenance.ReferenceIds)
                        .Distinct().Order(StringComparer.Ordinal).ToArray();
                    Assert.Equal(inherited, proposal.Provenance.ReferenceIds);
                }
                if (proposal.Result == "evaluated") Assert.Null(generator.Invalid(proposal.Party!));
                Assert.NotEqual("order", proposal.Provenance.Operator);
                prior.Add(proposal.Provenance.Id, proposal);
            }
        }
        TowerBossDiscovery.ValidateProvenance(definition, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run()));
        Assert.Equal(before, HarnessJson.Hash(definition));
        var laterPanels = definition with { Stages = definition.Stages with {
            Schedules = definition.Stages.Schedules.ToDictionary(p => p.Key,
                p => p.Value with { Selection = [801, 802], Confirmation = [901, 902] }) } };
        var isolated = await TowerSuppliedCompositionSearch.RunAsync(laterPanels, F.Mechanics(input) with { Cores = null, Coverage = null },
            (party, _, _) => Task.FromResult(Score(input, party, definition.Starts.Any(s => s.Party.Id == party.Id) ? 1 : 4)));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(isolated));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.GenerationInputs(definition));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(definition with { Mode = TowerBossDiscovery.Independent }));
    }

    [Fact]
    public void Historical_order_is_canonicalized_without_mutating_the_reference_or_changing_owner_placement()
    {
        var source = Source();
        source = source with { References = source.References.Select(r => r with { Scenario = r.Scenario with {
            Party = r.Scenario.Party.Select(p => p with { Build = p.Build with {
                EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() } }).ToArray() };
        var before = HarnessJson.Hash(source);
        var prepared = Prepared(source);
        Assert.Equal(before, HarnessJson.Hash(source));
        Assert.Equal(source.References[0].EvidenceHash, prepared.References[0].EvidenceHash);
        Assert.All(prepared.References[0].Scenario.Party,
            p => Assert.True(TowerCompositionSearch.IsCanonical(p.Build.EssenceIds)));
        Assert.Equal(source.ContentHashes, prepared.ContentHashes);
        Assert.Equal(source.SettingsHash, prepared.SettingsHash);
        Assert.Equal(source.ExecutionHash, prepared.ExecutionHash);
        var start = Assert.Single(prepared.Starts);
        Assert.Equal(source.References[0].Id, start.ReferenceId);
        Assert.All(start.Party.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
        Assert.Equal(source.References[0].Scenario.Party.Select(p => p.PartySlot).Order(), start.Party.Builds.Keys.Order());
        Assert.NotEqual(TowerPartySelection.Choice("historical", source.References[0].Scenario.Party.ToDictionary(p => p.PartySlot,
            p => p.Build.EssenceIds)).Id, start.Party.Id);
    }

    [Fact]
    public void Admission_rejects_incompatible_identity_family_copies_and_more_than_two_starts()
    {
        var source = Source();
        var prepared = Prepared(source);
        Assert.Throws<InvalidDataException>(() => TowerSuppliedCompositionSearch.Prepare(source, []));
        Assert.Throws<InvalidDataException>(() => TowerSuppliedCompositionSearch.Prepare(source, ["unknown"]));
        Assert.Throws<InvalidDataException>(() => TowerSuppliedCompositionSearch.Prepare(source, ["saved-0", "saved-0"]));
        var extra = Source(starts: [Party(["e00", "e01", "e02", "e03"], ["e00", "e01", "e02", "e03"]),
            Party(["e04", "e05", "e06", "e07"], ["e04", "e05", "e06", "e07"]),
            Party(["e08", "e09", "e10", "e11"], ["e08", "e09", "e10", "e11"])]);
        Assert.Throws<InvalidDataException>(() => Prepared(extra));
        var noRefinementRoom = extra with { References = extra.References.Take(2).ToArray(),
            Generation = extra.Generation with { CandidatesPerArm = 8 } };
        Assert.Throws<InvalidDataException>(() => Prepared(noRefinementRoom));
        Assert.Throws<InvalidDataException>(() => Prepared(source with {
            AllowedEssences = source.AllowedEssences.Select(e => e.Id == "e01" ? e with { Family = "FAMILY0" } : e).ToArray() }));
        Assert.Throws<InvalidDataException>(() => Prepared(source with {
            OwnedCopies = source.AllowedEssences.ToDictionary(e => e.Id, _ => 1) }));
        var drift = source.References[0] with { Scenario = source.References[0].Scenario with {
            Party = source.References[0].Scenario.Party.Select(p => p with { Build = p.Build with { Id = "different-actor-" + p.PartySlot } }).ToArray() } };
        Assert.Throws<InvalidDataException>(() => Prepared(source with { References = [drift] }));
        foreach (var generation in new[] { prepared.Generation with { CandidatesPerArm = 7 },
            prepared.Generation with { CandidatesPerArm = 65 }, prepared.Generation with { MaximumAttemptsPerArm = 257 } })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(prepared with { Generation = generation }));
    }

    [Fact]
    public void Population_preserves_a_nonanchor_deceptive_family_and_uses_membership_at_fixed_owners()
    {
        var input = F.Input(owners: 1, poolSize: 20);
        var nearAnchor = Enumerable.Range(3, 10).Select(i => Party(["e00", "e01", "e02", "e" + i.ToString("D2")])).ToArray();
        var distant = Party(["e14", "e15", "e16", "e17"]);
        var proposals = nearAnchor.Append(distant).Select((party, index) => Proposal(party, index)).ToDictionary(p => p.Party!.Id);
        var rows = nearAnchor.Select(party => Score(input, party, 6)).Append(Score(input, distant, 5)).ToArray();
        var retained = TowerSuppliedCompositionSearch.Retain(rows, proposals);
        Assert.Equal(8, retained.Length); Assert.Contains(distant.Id, retained);
        Assert.Equal(TowerBossGeneration.Rank(rows).Take(4).Select(r => r.Id), retained.Take(4));
        Assert.Equal(retained, TowerSuppliedCompositionSearch.Retain(rows.Reverse(), proposals));
        Assert.Equal(0, TowerSuppliedCompositionSearch.Distance(nearAnchor[0], nearAnchor[0]));
        Assert.Equal(2, TowerSuppliedCompositionSearch.Distance(nearAnchor[0], nearAnchor[1]));
        Assert.Equal(8, TowerSuppliedCompositionSearch.Distance(nearAnchor[0], distant));
        var swapped = Party(["e14", "e15", "e16", "e17"], ["e00", "e01", "e02", "e03"]);
        Assert.Equal(16, TowerSuppliedCompositionSearch.Distance(swapped,
            Party(["e00", "e01", "e02", "e03"], ["e14", "e15", "e16", "e17"])));
        // The non-anchor B lineage can reach its coordinated optimum; no <=3-member
        // change from any A party can reach it. The four-elite-only archive loses B.
        var optimum = Party(["e14", "e15", "e18", "e19"]);
        Assert.All(nearAnchor, a => Assert.Equal(8, TowerSuppliedCompositionSearch.Distance(a, optimum)));
        Assert.DoesNotContain(distant.Id, TowerBossGeneration.Rank(rows).Take(4).Select(r => r.Id));
        var constrained = input with { AllowedEssences = input.AllowedEssences.Select(e => e with {
            Family = e.Id switch { "e00" or "e14" => "f0", "e01" or "e15" => "f1",
                "e02" or "e16" or "e18" => "f2", _ => "f3" } }).ToArray() };
        var child = EssenceWitness(constrained, proposals[retained.Single(id => id == distant.Id)].Party!,
            [2, 3], ["e18", "e19"]).Party!;
        Assert.Equal(optimum.Id, child.Id); Assert.Equal(9, child.Id == optimum.Id ? 9 : 5);
    }

    [Fact]
    public void Two_owner_inventory_swaps_are_validated_after_both_replacements_and_family_copy_violations_still_fail()
    {
        var input = BinaryInput(owners: 2);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var parent = Party(BinaryLoadout(0), BinaryLoadout(15));
        var swapped = Party(BinaryLoadout(15), BinaryLoadout(0));
        Legal(input, parent); Legal(input, swapped);
        var children = BlockProposals(input, parent, "character-block").Where(p => p.Rejection is null)
            .Select(p => p.Party!).ToArray();
        Assert.NotEmpty(children); Assert.All(children, party => Legal(input, party));
        var random = new ChoiceTranscript(new[] { 0 }.Concat(RecipeTranscript(input, BinaryLoadout(15)))
            .Concat(RecipeTranscript(input, BinaryLoadout(0))));
        var witness = TowerSuppliedCompositionSearch.Propose(input, parent, null, "character-block", 0, random);
        Assert.Equal(0, random.Remaining); Assert.Null(witness.Rejection); Assert.Equal(swapped.Id, witness.Party!.Id);
        // Taking only one entire donor owner creates an illegal duplicate inventory.
        foreach (var seed in FrozenConstructionSeeds)
        {
            var partial = TowerSuppliedCompositionSearch.Propose(input, parent, swapped, "donor-block", 0, new Random(seed));
            Assert.Equal("construction-exhausted", partial.Rejection); Assert.Null(partial.Party);
        }
        var definition = F.Definition(input);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateParty(definition,
            Party(BinaryLoadout(0), BinaryLoadout(0))));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateParty(definition,
            Party(["e00", "e04", "e02", "e03"], BinaryLoadout(15))));
    }

    [Fact]
    public async Task Exhausted_space_charges_every_proposal_and_never_scores_permutations_again()
    {
        var definition = Prepared(Source(owners: 1, pool: 4, candidates: 8, attempts: 32));
        var input = TowerBossImprovement.Inputs(definition); var calls = 0;
        var result = await TowerSuppliedCompositionSearch.RunAsync(definition, F.Mechanics(input), (party, _, _) => {
            calls++; return Task.FromResult(Score(input, party, 2));
        });
        Assert.Equal("Incomplete", result.Status); Assert.Equal(2, calls);
        Assert.All(result.Arms, arm => {
            Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Equal(32, arm.Proposals.Count);
            Assert.Single(arm.Evaluations); Assert.Contains(arm.Proposals, p => p.Result == "duplicate");
            Assert.DoesNotContain(arm.Proposals, p => p.Result == "evaluating");
        });
    }

    [Fact]
    public async Task Cancellation_and_invalid_measurements_keep_the_charged_proposal_without_selecting_it()
    {
        var definition = Prepared(Source()); var input = TowerBossImprovement.Inputs(definition);
        using var stop = new CancellationTokenSource();
        var cancelled = await TowerSuppliedCompositionSearch.RunAsync(definition, F.Mechanics(input), (_, _, token) => {
            stop.Cancel(); token.ThrowIfCancellationRequested(); throw new InvalidOperationException("Unreachable.");
        }, stop.Token);
        Assert.Equal("Cancelled", cancelled.Status);
        var arm = Assert.Single(cancelled.Arms); Assert.Single(arm.Proposals); Assert.Empty(arm.Evaluations);
        Assert.Equal("Cancelled", arm.Proposals[0].Result); Assert.Empty(cancelled.DiscoveryShortlist);
        var invalid = await TowerSuppliedCompositionSearch.RunAsync(definition, F.Mechanics(input), (party, _, _) =>
            Task.FromResult(Score(input, party, 3) with { Cells = [] }));
        Assert.Equal("Invalid", invalid.Status); Assert.Empty(invalid.DiscoveryShortlist);
        Assert.Single(invalid.Arms[0].Proposals); Assert.Empty(invalid.Arms[0].Evaluations);
    }
}
