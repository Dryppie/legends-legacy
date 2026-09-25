using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessLoadoutPlacementTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-loadout-placement-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Placement fixture entered combat.")).Activate();
    public BalanceHarnessLoadoutPlacementTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    internal static TowerProposalRacingPlan Plan(int seed = 17)
    {
        var racing = BalanceHarnessAdaptiveRacingTests.Plan(seed, 10);
        var d = racing.Scope with { Budget = TowerPartyProgression.Budget(5) with { PriorityFloor = 5 },
            Contexts = racing.Scope.Contexts.Select(c => c with { CharacterTemplates = c.CharacterTemplates.Select(a => a with {
                Build = a.Build with { CharacterLevel = 40, Rank = 2 }
            }).ToArray() }).ToArray() };
        var starts = d.Starts.Select(s => s with { Party = TowerPartySelection.Choice("literal", s.Party.Builds
            .ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Append("e" + (20 + p.Key)).Order(StringComparer.Ordinal).ToArray())) }).ToArray();
        d = d with { Starts = starts, References = d.References.Select(r => r with {
            Scenario = TowerBossDiscovery.Scenario(d, r.Context, starts.Single(s => s.ReferenceId == r.Id).Party, [])
        }).ToArray() };
        var p = BalanceHarnessBenchmarkValidationTests.OptIn(new(TowerProposalPolicies.RacingVersion,
            racing with { Scope = d, Mechanics = racing.Mechanics with { Floor = 5 } }, TowerProposalPolicies.BenchmarkSmallEdits()));
        return p with { Version = TowerProposalPolicies.LoadoutPlacementRacingVersion, Policy = TowerProposalPolicies.BenchmarkLoadoutPlacement() };
    }

    private static TowerProposalContext Context(TowerProposalRacingPlan p) =>
        new(p.Racing.Scope, p.Racing.Mechanics, p.Racing.BenchmarkReferenceId, p.Racing.RootSeed);
    private static PartyChoice Parent(TowerProposalRacingPlan p) => p.Racing.Scope.Starts.Single(s => s.ReferenceId == p.Racing.BenchmarkReferenceId).Party;
    private static TowerProposalExportRequest Request(TowerProposalRacingPlan p) => new(TowerProposalPolicies.LoadoutPlacementExportVersion,
        Context(p), [TowerProposalPolicies.BenchmarkSmallEdits(), p.Policy]);
    private static TowerLoadoutPlacementCatalogue Catalogue(TowerProposalRacingPlan p) => TowerLoadoutPlacement.Create(p.Racing.Scope, p.Racing.BenchmarkReferenceId);
    private static TowerProposalArmExport Candidate(TowerProposalExport e) => e.Arms.Single(a => a.LoadoutPlacementCatalogue is not null);
    private static Dictionary<string, int> Counts(IEnumerable<string> ids) => ids.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
    private static TowerPanelOutcome Outcome(TowerPanelTrial r, bool won = true) => new(HarnessJson.Hash(r),
        $"literal-{r.Ordinal:D6}", r.Seed, won ? BattleOutcome.Victory : BattleOutcome.Defeat, 50, 50, 1);

    private static TowerProposalRacingPlan ReplaceReference(TowerProposalRacingPlan p, string referenceId, PartyChoice party)
    {
        var d = p.Racing.Scope;
        return p with { Racing = p.Racing with { Scope = d with {
            Starts = d.Starts.Select(s => s.ReferenceId == referenceId ? s with { Party = party } : s).ToArray(),
            References = d.References.Select(r => r.Id == referenceId ? r with {
                Scenario = TowerBossDiscovery.Scenario(d, r.Context, party, []) } : r).ToArray()
        } } };
    }

    [Fact]
    public void Complete_catalogue_matches_independent_cartesian_bijection_oracle()
    {
        var p = Plan(); var parent = Parent(p); var expected = new HashSet<string>();
        foreach (var start in new[] { 1, 6 })
        for (var encoded = 0; encoded < 3125; encoded++)
        {
            var value = encoded; var sources = new int[5];
            for (var i = 0; i < 5; i++) { sources[i] = start + value % 5; value /= 5; }
            if (sources.Distinct().Count() != 5) continue;
            var builds = parent.Builds.ToDictionary(x => x.Key, x => x.Value);
            for (var i = 0; i < 5; i++) builds[start + i] = parent.Builds[sources[i]];
            if (HarnessJson.Hash(builds) != parent.Id) expected.Add(HarnessJson.Hash(builds));
        }
        var c = Catalogue(p);
        Assert.Equal(240, c.AssignmentsExamined); Assert.Equal(2, c.IdentityAssignments);
        Assert.Equal(0, c.ReferenceAssignments); Assert.Equal(0, c.DuplicateAssignments);
        Assert.Equal(238, c.Recipes.Count);
        Assert.True(expected.SetEquals(c.Recipes.Select(r => r.Party.Id)));
        Assert.Equal(new[] { 20, 40, 90, 88 }, Enumerable.Range(2, 4).Select(n => c.Recipes.Count(r => r.ChangedOwners.Count == n)));
        Assert.All(c.Recipes.GroupBy(r => r.Subgroup), g => Assert.Equal(119, g.Count()));
        Assert.Equal(c.Recipes.Select(r => r.Party.Id).Order(StringComparer.Ordinal), c.Recipes.Select(r => r.Party.Id));
    }

    [Fact]
    public void Every_recipe_preserves_subgroup_bundles_inventory_and_physical_actor_fields()
    {
        var p = Plan(); var d = p.Racing.Scope; var parent = Parent(p);
        var anchor = d.References.Single(r => r.Id == p.Racing.BenchmarkReferenceId).Scenario;
        foreach (var r in Catalogue(p).Recipes)
        {
            TowerBossDiscovery.ValidateParty(d, r.Party);
            foreach (var group in parent.Builds.Keys.GroupBy(WorldTowerPartyRules.GetPartyNumber))
            {
                Assert.Equal(HarnessJson.Hash(Counts(group.SelectMany(o => parent.Builds[o]))), HarnessJson.Hash(Counts(group.SelectMany(o => r.Party.Builds[o]))));
                Assert.Equal(group.Select(o => HarnessJson.Hash(parent.Builds[o])).Order(), group.Select(o => HarnessJson.Hash(r.Party.Builds[o])).Order());
            }
            Assert.Single(r.ChangedOwners.Select(WorldTowerPartyRules.GetPartyNumber).Distinct());
            Assert.Equal(TowerSuppliedCompositionSearch.Distance(parent, r.Party) / 2, r.ReplacementDistance);
            var scenario = TowerLoadoutPlacement.Scenario(d, p.Racing.BenchmarkReferenceId, r.Party);
            Assert.Equal(r.SeedFreeScenarioHash, HarnessJson.Hash(scenario)); Assert.Empty(scenario.Seeds);
            Assert.Equal(HarnessJson.Hash(anchor with { Party = [] }), HarnessJson.Hash(scenario with { Party = [] }));
            foreach (var actor in scenario.Party)
            {
                var original = anchor.Party.Single(a => a.PartySlot == actor.PartySlot);
                Assert.Equal(HarnessJson.Hash(original.Build with { EssenceIds = [] }), HarnessJson.Hash(actor.Build with { EssenceIds = [] }));
            }
        }
    }

    [Fact]
    public void Fixed_shuffle_yields_two_disjoint_waves_and_is_independent_of_arm_order()
    {
        var q = Request(Plan()); var original = HarnessJson.Hash(q);
        var first = TowerProposalPolicies.Export(q); var arm = Candidate(first);
        Assert.Equal("Complete", first.Status); Assert.Equal(0, first.NewFights); Assert.Equal(0, first.NewReservedValues);
        Assert.Equal(9, arm.Batch.Candidates.Count); Assert.Equal(8, arm.SecondWave!.Batch.Candidates.Count);
        var proposals = arm.Batch.Proposals.Concat(arm.SecondWave.Batch.Proposals).ToArray();
        Assert.Equal(17, proposals.Select(r => r.Party!.Id).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 17), proposals.Select(r => r.LoadoutPlacement!.DrawOrdinal));
        Assert.All(proposals, r => {
            Assert.Null(r.Rejection); Assert.Null(r.Fallback); Assert.Null(r.AffinityCreation);
            Assert.Equal(HarnessJson.Hash(arm.LoadoutPlacementCatalogue), r.LoadoutPlacement!.CatalogueHash);
            Assert.Contains(r.Party!.Id, arm.LoadoutPlacementCatalogue!.Recipes.Select(x => x.Party.Id));
        });
        Assert.Equal(original, HarnessJson.Hash(q));
        Assert.Equal(HarnessJson.Hash(first), HarnessJson.Hash(TowerProposalPolicies.Export(q)));
        Assert.Equal(HarnessJson.Hash(arm), HarnessJson.Hash(Candidate(TowerProposalPolicies.Export(q with { Policies = q.Policies.Reverse().ToArray() }))));
        Assert.NotEqual(HarnessJson.Hash(arm.Batch), HarnessJson.Hash(Candidate(TowerProposalPolicies.Export(Request(Plan(99)))).Batch));
        Assert.All(arm.Teams, t => Assert.Empty(t.Scenario.Seeds));
    }

    [Fact]
    public void Duplicate_loadouts_are_deduplicated_before_sampling_and_underfill_never_dispatches()
    {
        var p = Plan(); var parent = Parent(p);
        var builds = parent.Builds.ToDictionary(x => x.Key, x => x.Value);
        foreach (var start in new[] { 1, 6 })
            for (var i = 1; i < 4; i++) builds[start + i] = builds[start];
        p = ReplaceReference(p, p.Racing.BenchmarkReferenceId, TowerPartySelection.Choice("duplicates", builds));
        var c = Catalogue(p);
        Assert.Equal(8, c.Recipes.Count); Assert.Equal(48, c.IdentityAssignments); Assert.Equal(184, c.DuplicateAssignments);
        Assert.All(c.Recipes, r => Assert.Equal(24, r.Assignments.Count));
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Export(Request(p)));
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Validate(p));
    }

    [Fact]
    public async Task Empty_catalogue_rejects_before_evaluator_checkpoint_or_output_creation()
    {
        var p = Plan(); var parent = Parent(p);
        p = ReplaceReference(p, p.Racing.BenchmarkReferenceId, TowerPartySelection.Choice("identical",
            parent.Builds.ToDictionary(x => x.Key, x => parent.Builds[1])));
        Assert.Empty(Catalogue(p).Recipes); var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(p, (r, _) => {
            calls++; return Task.FromResult(Outcome(r)); }, checkpoint: _ => calls++));
        Assert.Equal(0, calls);
        var output = Path.Combine(root, "empty");
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.WriteExport(Request(p), output));
        Assert.False(Path.Exists(output));
    }

    [Fact]
    public void Other_reference_recipes_are_excluded_even_when_reachable_by_permutation()
    {
        var p = Plan(); var reachable = Catalogue(p).Recipes[0].Party;
        p = ReplaceReference(p, p.Racing.Scope.Starts[0].ReferenceId, reachable);
        var c = Catalogue(p);
        Assert.Equal(237, c.Recipes.Count); Assert.Equal(1, c.ReferenceAssignments);
        Assert.DoesNotContain(c.Recipes, r => r.Party.Id == reachable.Id);
    }

    [Fact]
    public void Tight_owned_copy_limits_remain_satisfied()
    {
        var p = Plan();
        var max = p.Racing.Scope.Starts.SelectMany(s => Counts(s.Party.Builds.Values.SelectMany(x => x)))
            .GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Max(x => x.Value));
        p = p with { Racing = p.Racing with { Scope = p.Racing.Scope with { OwnedCopies = max } } };
        Assert.Equal(238, Catalogue(p).Recipes.Count);
        max[Parent(p).Builds[1][0]] = 0;
        Assert.Throws<InvalidDataException>(() => Catalogue(p));
    }

    [Fact]
    public void Cycle_reads_parent_atomically_and_rejects_cross_subgroup_or_repeated_sources()
    {
        var p = Plan(); var parent = Parent(p);
        var map = Enumerable.Range(1, 5).ToDictionary(i => i, i => i % 5 + 1);
        var result = TowerLoadoutPlacement.Apply(p.Racing.Scope, parent, new(map));
        Assert.All(map, item => Assert.Equal(parent.Builds[item.Value], result.Builds[item.Key]));
        map[5] = 6;
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPlacement.Apply(p.Racing.Scope, parent, new(map)));
        map[5] = 2;
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPlacement.Apply(p.Racing.Scope, parent, new(map)));
        var cross = new Dictionary<int, int> { [1] = 6, [2] = 2, [3] = 3, [4] = 4, [6] = 1 };
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPlacement.Apply(p.Racing.Scope, parent, new(cross)));
    }

    [Fact]
    public void Caller_mutation_and_changed_wave_seen_set_cannot_change_future_draws()
    {
        var p = Plan(); var context = Context(p); var g = new TowerAdaptiveRacingGenerator(context, p.Policy);
        var seen = p.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
        var first = g.Generate(1, [], seen, [], default);
        var expected = HarnessJson.Hash(first);
        ((IList<string>)first.Candidates[0].Builds[1])[0] = "corrupted";
        Assert.Equal(expected, HarnessJson.Hash(g.Generate(1, [], seen, [], default)));
        Assert.Throws<InvalidDataException>(() => g.Generate(2, [], seen, [], default));
        Assert.Throws<InvalidDataException>(() => g.Generate(3, [], seen, [], default));
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("operator")]
    [InlineData("name")]
    [InlineData("affinity")]
    [InlineData("preserve")]
    public void New_policy_requires_the_exact_frozen_profile(string fault)
    {
        var p = Plan().Policy;
        p = fault switch {
            "parent" => p with { ParentTickets = ["beam"] },
            "operator" => p with { SecondWave = Enumerable.Repeat("single", 8).ToArray() },
            "name" => p with { Name = "renamed" },
            "affinity" => p with { PreservedDamageAffinityIds = [] },
            _ => p with { PreserveParentInteractions = true }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Validate(p));
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public async Task Earlier_racing_versions_reject_new_policy_before_dispatch(int version)
    {
        var p = Plan() with { Version = "tower-proposal-racing-v" + version };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(p,
            (_, _) => throw new InvalidOperationException("Must not dispatch")));
    }

    [Fact]
    public void Version_pairing_selector_scope_and_family_contracts_are_checked()
    {
        var p = Plan();
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Validate(p with { SelectionPolicyVersion = null }));
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Validate(p with { Policy = TowerProposalPolicies.BenchmarkSmallEdits() }));
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Export(Request(p) with { Version = TowerProposalPolicies.ExportVersion }));
        Assert.Throws<InvalidDataException>(() => Catalogue(p with { Racing = p.Racing with { Scope = p.Racing.Scope with { RequiredPartySize = 9 } } }));
        var ids = Parent(p).Builds[1];
        var d = p.Racing.Scope with { AllowedEssences = p.Racing.Scope.AllowedEssences.Select(e =>
            e.Id == ids[1] ? e with { Family = p.Racing.Scope.AllowedEssences.Single(x => x.Id == ids[0]).Family.ToUpperInvariant() } : e).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPlacement.Create(d, p.Racing.BenchmarkReferenceId));
    }

    [Fact]
    public async Task Literal_528_request_trajectory_retains_validation_and_reconstructs_provenance()
    {
        var p = Plan(); var parent = Parent(p);
        var result = await TowerProposalPolicies.RunAsync(p, (r, _) => Task.FromResult(Outcome(r)), panelFreezesOnly: true, checkpoint: null, token: default);
        Assert.Equal("Complete", result.Evaluation.Status); Assert.Equal(528, result.Evaluation.ChargedEvaluations);
        Assert.Equal(new[] { 96, 56, 120, 56, 80, 120 }, result.Evaluation.Panels.Select(x => x.Freeze.PlannedEvaluations));
        Assert.False(result.Evaluation.ValidationDecision!.Passed); Assert.Equal(parent.Id, result.Evaluation.RawSelectedId);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(p, result)));
        var other = await TowerProposalPolicies.RunAsync(p, (r, _) => Task.FromResult(Outcome(r, r.PartyId == parent.Id)), panelFreezesOnly: true, checkpoint: null, token: default);
        Assert.Equal(result.Batches.SelectMany(b => b.Candidates).Select(x => x.Id), other.Batches.SelectMany(b => b.Candidates).Select(x => x.Id));
        var first = result.Batches[0]; var proposal = first.Proposals[0];
        var changed = result with { Batches = [first with { Proposals = [proposal with {
            LoadoutPlacement = proposal.LoadoutPlacement! with { DrawOrdinal = 19 } }, .. first.Proposals.Skip(1)] }, result.Batches[1]] };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(p, changed));
    }

    [Theory]
    [InlineData("catalogue")]
    [InlineData("assignment")]
    [InlineData("physical-hash")]
    [InlineData("draw")]
    public void Rehashed_export_tampering_cannot_pass_reconstruction(string fault)
    {
        var q = Request(Plan()); var output = Path.Combine(root, "export");
        var pin = TowerProposalPolicies.WriteExport(q, output);
        Assert.Equal("Complete", TowerProposalPolicies.VerifyExport(output, pin).Status);
        var path = Path.Combine(output, "batches.json"); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var arm = json["arms"]!.AsArray().Single(a => a!["loadoutPlacementCatalogue"] is not null)!;
        var recipes = arm["loadoutPlacementCatalogue"]!["recipes"]!.AsArray();
        if (fault == "catalogue") recipes.RemoveAt(0);
        else if (fault == "assignment") recipes[0]!["assignments"]![0]!["sourceByDestination"]!["1"] = 10;
        else if (fault == "physical-hash") recipes[0]!["seedFreeScenarioHash"] = new string('0', 64);
        else arm["batch"]!["proposals"]![0]!["loadoutPlacement"]!["drawOrdinal"] = 18;
        File.WriteAllText(path, json.ToJsonString(HarnessJson.Options));
        var manifest = new Dictionary<string, string> { ["request.json"] = HarnessJson.FileHash(Path.Combine(output, "request.json")), ["batches.json"] = HarnessJson.FileHash(path) };
        File.WriteAllText(Path.Combine(output, "files.json"), JsonSerializer.Serialize(manifest, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.VerifyExport(output, HarnessJson.FileHash(Path.Combine(output, "files.json"))));
    }

    [Fact]
    public void Cancellation_is_observed_before_construction_export_and_wave_consumption()
    {
        var p = Plan(); var token = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => TowerLoadoutPlacement.Create(p.Racing.Scope, p.Racing.BenchmarkReferenceId, token));
        var output = Path.Combine(root, "cancelled");
        Assert.Throws<OperationCanceledException>(() => TowerProposalPolicies.WriteExport(Request(p), output, token));
        Assert.False(Path.Exists(output));
        var g = new TowerAdaptiveRacingGenerator(Context(p), p.Policy);
        Assert.Throws<OperationCanceledException>(() => g.Generate(1, [], p.Racing.Scope.Starts.Select(s => s.Party.Id).ToHashSet(), [], token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Earlier_racing_versions_keep_their_pre_cancelled_receipt(bool validation)
    {
        var p = Plan() with { Version = TowerProposalPolicies.BenchmarkValidationRacingVersion,
            Policy = TowerProposalPolicies.BenchmarkSmallEdits() };
        if (!validation) p = p with { Version = TowerProposalPolicies.RacingVersion, SelectionPolicyVersion = null,
            Racing = p.Racing with { Panels = BalanceHarnessAdaptiveRacingTests.Plan().Panels } };
        var result = await TowerProposalPolicies.RunAsync(p,
            (_, _) => throw new InvalidOperationException("Cancelled plan must not dispatch"), new CancellationToken(true));
        Assert.Equal("Cancelled", result.Evaluation.Status);
        Assert.Equal(0, result.Evaluation.ChargedEvaluations);
        Assert.Null(result.Evaluation.RawSelectedId);
    }
}
