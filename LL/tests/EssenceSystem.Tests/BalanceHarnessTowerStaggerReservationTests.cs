using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerStaggerReservationTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.StaggerReservationVersion,
            Methods = TowerBossGeneration.StaggerReservationMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }
    private static TowerBossPartyGenerator Generator(BossDiscoveryInputs input, BossGenerationMechanics m, bool reservation = true)
        => new(input, m, attributeDefense: true, compatibleDefense: true, staggerReservation: reservation);
    private static TowerMechanicNode Node<T>(string key, TowerMechanicNodeKind kind, T value)
        => new(key, kind, key, JsonSerializer.SerializeToElement(value, HarnessJson.Options), [], []);
    private static TowerBossInventoryReport WithStagger(BossStaggerDefinition? stagger)
        => Inventory.Value with { Bosses = Inventory.Value.Bosses.Select(b => b with { Definition = new() { Stagger = stagger } }).ToArray() };
    private static BossGenerationMechanics Narrow(BossGenerationMechanics m, IReadOnlyList<BossCoverageFeature> features)
        => m with { Cores = [], Coverage = features, DefenseCoverage = features,
            CompatibleDefense = m.CompatibleDefense! with { Coverage = features, Excluded = [] },
            StaggerReservations = m.StaggerReservations! with { Providers = m.StaggerReservations.Providers.Where(p => features.Any(f => f.Kind == "recurring-control" && f.EssenceId == p.EssenceId)).ToArray() } };

    [Fact]
    public void Metadata_keeps_every_provider_and_predicate_and_legacy_serialization_is_unchanged()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var inventory = Inventory.Value; var before = HarnessJson.Hash(inventory);
        var m = TowerBossPartyGenerator.FromInventory(input, inventory); var r = m.StaggerReservations!;
        Assert.Equal(250, r.FirstThreshold); Assert.Equal(80, m.Essences.Count); Assert.Equal(71, m.DefenseCoverage!.Count); Assert.Equal(69, m.CompatibleDefense!.Coverage.Count);
        Assert.Equal(new[] { (25, 10), (35, 8), (40, 7), (50, 5) }, r.Providers.OrderBy(p => p.StaggerPower).Select(p => (p.StaggerPower!.Value, p.MinimumCount!.Value)));
        Assert.All(r.Providers, p => { Assert.Null(p.FallbackReason); var route = Assert.Single(p.Routes); Assert.NotEmpty(route.SelectedTriggers); Assert.Equal(80, route.SeparateRuntimeControlChancePercent); });
        Assert.Contains(r.Providers, p => p.Routes[0].Effect!.Conditions.Any(c => c.Type == AbilityConditionType.HealthBelowPercent && c.Value == 30));
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CompatibleDefenseVersion, Methods = TowerBossGeneration.CompatibleDefenseMethods } };
        var old = TowerBossPartyGenerator.FromInventory(oldInput, inventory);
        Assert.Equal(HarnessJson.Hash(old), HarnessJson.Hash(m with { StaggerReservations = null }));
        Assert.DoesNotContain("staggerReservations", JsonSerializer.Serialize(old, HarnessJson.Options));
        Assert.Equal(HarnessJson.Hash(r), HarnessJson.Hash(TowerStaggerReservation.Create(input, inventory with { Nodes = inventory.Nodes.Reverse().ToArray(), Essences = inventory.Essences.Reverse().ToArray() }, m.Coverage!.Reverse().ToArray())));
        r.Providers[0].Routes[0].Effect!.StaggerPower = 999;
        Assert.Equal(before, HarnessJson.Hash(inventory));
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(10, 6)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Fresh_construction_is_legal_deterministic_and_preserves_uniform_route(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var before = HarnessJson.Hash(m); var g = Generator(input, m); var comparator = Generator(input, m, false); var uniform = 0;
        for (var seed = 0; seed < 32; seed++)
        {
            var choice = g.FreshCoverage(new Random(seed)); Assert.Null(choice.Rejection); TowerBossDiscovery.ValidateParty(d, choice.Party!);
            Assert.Equal(HarnessJson.Hash(choice), HarnessJson.Hash(g.FreshCoverage(new Random(seed))));
            if (choice.Intent.Contains("uniform")) { uniform++; Assert.Null(choice.Reservations); Assert.Equal(HarnessJson.Hash(choice), HarnessJson.Hash(comparator.FreshCoverage(new Random(seed)))); }
            else Assert.All(choice.Reservations!, r => { Assert.InRange(r.Satisfied, 0, r.Requested); Assert.NotEmpty(r.EvidenceKeys); });
        }
        Assert.True(uniform > 0); Assert.Equal(before, HarnessJson.Hash(m));
    }

    private sealed class CountRandom(int seed, bool upper = false) : Random(seed)
    {
        public List<(int Min, int Max)> CountRanges { get; } = [];
        public override int Next(int maxValue) => maxValue == 8 ? 1 : base.Next(maxValue);
        public override int Next(int minValue, int maxValue)
        { CountRanges.Add((minValue, maxValue)); return upper ? maxValue - 1 : minValue; }
    }

    [Fact]
    public void Every_supported_provider_uses_one_bounded_count_draw_and_records_actual_satisfaction()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        foreach (var p in m.StaggerReservations!.Providers)
        foreach (var upper in new[] { false, true })
        {
            var feature = m.Coverage!.Single(f => f.Kind == "recurring-control" && f.EssenceId == p.EssenceId);
            var random = new CountRandom(3, upper); var result = Generator(input, Narrow(m, [feature])).FreshCoverage(random);
            Assert.Null(result.Rejection); var trace = Assert.Single(result.Reservations!);
            Assert.Equal((p.MinimumCount!.Value, input.RequiredPartySize + 1), Assert.Single(random.CountRanges, r => r.Max == input.RequiredPartySize + 1));
            Assert.Equal(upper ? input.RequiredPartySize : p.MinimumCount, trace.Requested); Assert.Equal(trace.Requested, trace.Satisfied);
            Assert.Equal(p.EssenceId, trace.EssenceId); Assert.Equal(p.StaggerPower, trace.StaggerPower); Assert.Equal(250, trace.FirstThreshold); Assert.Null(trace.FallbackReason);
        }
    }

    [Theory]
    [InlineData("absent-stagger")] [InlineData("disabled-stagger")] [InlineData("no-first-break")] [InlineData("unattainable-minimum")]
    public void Boss_fallbacks_preserve_old_count_draw_and_party(string reason)
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        BossStaggerDefinition? stagger = reason == "absent-stagger" ? null : new() { Enabled = reason != "disabled-stagger",
            MaximumBreaks = reason == "no-first-break" ? 0 : null, BaseThreshold = reason == "unattainable-minimum" ? int.MaxValue : 250,
            ReferenceParticipantCount = input.RequiredPartySize };
        m = m with { StaggerReservations = TowerStaggerReservation.Create(input, WithStagger(stagger), m.Coverage!) };
        var g = Generator(input, m); var old = Generator(input, m, false);
        for (var seed = 0; seed < 16; seed++)
        {
            var actual = g.FreshCoverage(new Random(seed)); var expected = old.FreshCoverage(new Random(seed));
            Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(actual with { Reservations = null }));
            if (actual.Reservations is not null) Assert.Equal(reason, Assert.Single(actual.Reservations, r => r.Kind == "recurring-control").FallbackReason);
        }
        if (reason == "unattainable-minimum") Assert.All(m.StaggerReservations!.Providers, p => Assert.True(p.MinimumCount > input.RequiredPartySize));
    }

    [Theory]
    [InlineData(5, 2, 3)] [InlineData(501, 20, 251)] [InlineData(250, 10, 250)]
    public void Threshold_uses_full_party_scaling_and_away_from_zero_rounding(int threshold, int reference, int expected)
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        // First case uses one participant: 5 / 2 rounds 2.5 away from zero.
        if (threshold == 5) input = input with { RequiredPartySize = 1 };
        var m = TowerStaggerReservation.Create(input, WithStagger(new() { Enabled = true, BaseThreshold = threshold, ReferenceParticipantCount = reference }), TowerPartyCoverage.Create(input, Inventory.Value));
        Assert.Equal(expected, m.FirstThreshold);
        Assert.All(m.Providers, p => Assert.Equal((expected + p.StaggerPower!.Value - 1) / p.StaggerPower, p.MinimumCount));
    }

    [Theory]
    [InlineData("zero")] [InlineData("chance")] [InlineData("multiple")] [InlineData("nested")] [InlineData("unknown-effect")]
    [InlineData("unknown-ability")] [InlineData("missing")] [InlineData("unselected")]
    public void Unsupported_direct_routes_remain_eligible_but_do_not_get_a_count_bias(string kind)
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var id = input.AllowedEssences[0].Id;
        var e = new AbilityEffectSpec { Id = "control", Operation = AbilityEffectOperation.ApplyCondition, Condition = StandardConditionType.Stun,
            Target = AbilityTargetSelector.CurrentTarget, StaggerPower = kind == "zero" ? 0 : 40, ChancePercent = kind == "chance" ? 0 : 100 };
        var ability = new AbilitySpec { Id = "fixture", Effects = [e] };
        if (kind == "unselected") ability.Triggers = [new() { Event = AbilityTriggerEvent.OnInterval, EffectIds = ["another"] }];
        var key = "Effect:Ability:fixture/control";
        var nodes = new List<TowerMechanicNode> { Node("Ability:fixture", TowerMechanicNodeKind.Ability, ability), Node(key, TowerMechanicNodeKind.Effect, e) };
        var evidence = new List<string> { "Ability:fixture", key };
        if (kind == "multiple") { evidence.Add(key + "-second"); nodes.Add(Node(key + "-second", TowerMechanicNodeKind.Effect, e)); }
        if (kind == "nested") evidence[1] = "Effect:Status:fixture/control";
        if (kind == "missing") nodes.RemoveAt(1);
        if (kind.StartsWith("unknown")) { var index = kind == "unknown-effect" ? 1 : 0; nodes[index] = nodes[index] with { Unknowns = ["unresolved"] }; }
        var inventory = Inventory.Value with { Nodes = nodes, Essences = Inventory.Value.Essences.Select(x => x.Id == id ? x with { AbilityIds = ["fixture"] } : x).ToArray() };
        var feature = new BossCoverageFeature(id, "recurring-control", evidence.Order(StringComparer.Ordinal).ToArray());
        var reservation = TowerStaggerReservation.Create(input, inventory, [feature]); var provider = Assert.Single(reservation.Providers);
        Assert.Equal(id, provider.EssenceId); Assert.NotNull(provider.FallbackReason); Assert.Null(provider.MinimumCount);
        var m = Narrow(TowerBossPartyGenerator.FromInventory(input, Inventory.Value), [feature]) with { StaggerReservations = reservation };
        var actual = Generator(input, m).FreshCoverage(new CountRandom(2)); var expectedChoice = Generator(input, m, false).FreshCoverage(new CountRandom(2));
        Assert.Equal(HarnessJson.Hash(expectedChoice), HarnessJson.Hash(actual with { Reservations = null }));
        Assert.Equal(provider.FallbackReason, Assert.Single(actual.Reservations!).FallbackReason);
    }

    [Fact]
    public void Scarce_ownership_and_shared_category_copies_preserve_existing_add_semantics()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var g = Generator(input, m); var accepted = 0; var unsatisfied = 0;
        for (var seed = 0; seed < 24; seed++)
        {
            var c = g.FreshCoverage(new Random(seed)); if (c.Rejection is not null) continue;
            accepted++; Assert.Null(g.Invalid(c.Party!)); Assert.All(c.Party!.Builds.Values.SelectMany(x => x).GroupBy(x => x), copies => Assert.Single(copies));
            if (c.Reservations?.Any(r => r.Kind == "recurring-control" && r.Requested > r.Satisfied) == true) unsatisfied++;
        }
        Assert.True(accepted > 0); Assert.True(unsatisfied > 0);
        var control = m.Coverage!.First(f => f.Kind == "recurring-control");
        // Fixture deliberately gives the same copy two categories and exactly one owned copy per party member.
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => input.RequiredPartySize) };
        var overlap = Narrow(m, new[] { control, control with { Kind = "recovery" } }.OrderBy(f => f.Kind, StringComparer.Ordinal).ToArray());
        var c2 = Generator(input, overlap).FreshCoverage(new AllCopiesRandom());
        Assert.Null(c2.Rejection); Assert.All(c2.Reservations!, r => Assert.Equal(input.RequiredPartySize, r.Satisfied));
        Assert.Equal(input.RequiredPartySize, c2.Party!.Builds.Values.Sum(ids => ids.Count(id => id == control.EssenceId)));
    }
    private sealed class AllCopiesRandom() : Random(0)
    {
        public override int Next(int maxValue) => maxValue == 8 ? 1 : maxValue == 11 ? 10 : base.Next(maxValue);
        public override int Next(int minValue, int maxValue) => maxValue - 1;
    }

    [Fact]
    public void Empty_coverage_falls_back_and_metadata_is_required_only_for_v10()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var empty = Narrow(m, []); var g = Generator(input, empty);
        var result = g.FreshCoverage(new CountRandom(2)); Assert.Equal("coverage:no-features-uniform", result.Intent); Assert.Null(result.Reservations);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(Generator(input, empty, false).FreshCoverage(new CountRandom(2))));
        Assert.Throws<InvalidDataException>(() => Generator(input, m with { StaggerReservations = null }));
        Assert.Throws<InvalidDataException>(() => Generator(input, m with { StaggerReservations = m.StaggerReservations! with { FirstThreshold = 249 } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m, staggerReservation: true));
        var old = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CompatibleDefenseVersion, Methods = TowerBossGeneration.CompatibleDefenseMethods } };
        Assert.Throws<InvalidDataException>(() => Generator(old, m, false));
    }

    [Fact]
    public async Task V10_preserves_v9_comparator_mutations_provenance_and_reference_boundary()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var originalInput = HarnessJson.Hash(input); var originalMechanics = HarnessJson.Hash(m);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var result = await TowerBossGeneration.RunAsync(input, m, Score); Assert.Equal("Complete", result.Status);
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CompatibleDefenseVersion, Methods = TowerBossGeneration.CompatibleDefenseMethods } };
        var old = await TowerBossGeneration.RunAsync(oldInput, TowerBossPartyGenerator.FromInventory(oldInput, Inventory.Value), Score);
        Assert.Equal(HarnessJson.Hash(old.Arms[1]), HarnessJson.Hash(result.Arms[0])); Assert.All(result.Arms[0].Proposals, p => Assert.Null(p.Reservations));
        var provenance = result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray(); TowerBossDiscovery.ValidateProvenance(d, provenance);
        var fresh = result.Arms[1].Proposals.First(p => p.Provenance.Operator == "fresh-stagger-reservation").Provenance;
        Assert.Empty(fresh.ParentIds); Assert.All(provenance, p => Assert.Empty(p.ReferenceIds));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, provenance.Select(p => p.Id == fresh.Id ? fresh with { Method = "compatible-defense-joint" } : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, provenance.Select(p => p.Id == fresh.Id ? fresh with { ReferenceIds = ["saved-control"] } : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = d.Generation.Methods.Reverse().ToArray() } }));
        var parent = result.Arms[1].Proposals.First(p => p.Result == "evaluated").Party!; var before = HarnessJson.Hash(parent);
        var context = d.Contexts[0].Id;
        var reference = new BossBenchmarkReference("fixture-reference", context, TowerBossDiscovery.Scenario(d, context, parent, []), "Fixture boundary check", new string('a', 64));
        Assert.Equal(originalInput, HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d with { References = [reference] })));
        var changed = Generator(input, m); var baseline = Generator(input, m, false);
        foreach (var op in TowerBossGeneration.Operators.Where(op => op != "recombine"))
            Assert.Equal(HarnessJson.Hash(baseline.Mutate(new Random(5), op, parent)), HarnessJson.Hash(changed.Mutate(new Random(5), op, parent)));
        Assert.Equal(HarnessJson.Hash(baseline.ChangeCoverage(new Random(5), parent)), HarnessJson.Hash(changed.ChangeCoverage(new Random(5), parent)));
        Assert.Equal(HarnessJson.Hash(baseline.ChangeCollectiveCoverageProvider(new Random(5), parent)), HarnessJson.Hash(changed.ChangeCollectiveCoverageProvider(new Random(5), parent)));
        Assert.Equal(HarnessJson.Hash(baseline.ChangePlacement(new Random(5), parent)), HarnessJson.Hash(changed.ChangePlacement(new Random(5), parent)));
        Assert.Equal(before, HarnessJson.Hash(parent)); Assert.Equal(originalInput, HarnessJson.Hash(input)); Assert.Equal(originalMechanics, HarnessJson.Hash(m));
    }
}
