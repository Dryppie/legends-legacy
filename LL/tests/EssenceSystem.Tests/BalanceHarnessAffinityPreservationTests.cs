using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityPreservationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-affinity-preservation-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Preservation fixtures entered combat or native preparation.")).Activate();
    public BalanceHarnessAffinityPreservationTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private static TowerProposalExportRequest Request(params string[] producers)
    {
        var (p, inventory) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        if (producers.Length == 0) producers = ["e00", "e01"];
        var ids = TowerDamageSourceAffinities.Create(inventory).Affinities
            .Where(a => producers.Contains(a.ProducerEssenceId) && a.ModifierEssenceId == "e10")
            .Select(a => a.Id).ToArray();
        return new(TowerProposalPolicies.PreservingCreationExportVersion,
            new(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed, inventory),
            [TowerProposalPolicies.BenchmarkAffinityCreation(ids), TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids)]);
    }

    private static PartyChoice Parent(TowerProposalExportRequest q) =>
        q.Context.Scope.Starts.Single(s => s.ReferenceId == q.Context.BenchmarkReferenceId).Party;
    private static TowerDamageSourceAffinity[] Affinities(TowerProposalExportRequest q) =>
        TowerProposalPolicies.SelectedAffinities(q.Context, q.Policies[1], creation: true);

    [Fact]
    public void Protects_all_completable_partners_not_only_the_chosen_pair_without_mutating_parent_protection()
    {
        var q = Request(); var parent = Parent(q); var before = HarnessJson.Hash(q);
        var baseline = new HashSet<string>(StringComparer.Ordinal) { "e02" };
        var c = new TowerAffinityCreation(TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope), Affinities(q), (_, _) => baseline, true);
        var rows = c.Opportunities(parent, 1, default);
        Assert.Equal(2, rows.Length);
        Assert.All(rows, r => {
            Assert.Equal(new[] { "e00", "e01", "e02" }, r.ProtectedEssences);
            var edit = Assert.Single(r.LegalEdits);
            Assert.Equal("e10", Assert.Single(edit.Added)); Assert.Equal("e09", Assert.Single(edit.Removed));
        });
        var made = c.Create(parent, 1, new Random(17), default);
        Assert.Null(made.Rejection); Assert.Equal(2, made.Step!.NewlyActivatedAffinityIds.Count);
        Assert.Equal(new[] { "e00", "e01", "e02" }, made.Step.RemovalSelection!.ProtectedEssences);
        Assert.Equal(2, made.Step.RemovalSelection.EligiblePairs); Assert.Equal(1, made.Step.RemovalSelection.EligibleEdits);
        Assert.Equal(new[] { "e02" }, baseline); Assert.Equal(before, HarnessJson.Hash(q));
        Assert.All(parent.Builds.Keys.Where(o => o != 1), o => Assert.Equal(parent.Builds[o], made.Party!.Builds[o]));
        TowerBossDiscovery.ValidateParty(q.Context.Scope, made.Party!);
    }

    [Fact]
    public void Already_active_routes_survive_and_uncompletable_routes_do_not_protect_unrelated_endpoints()
    {
        var q = Request(); var selected = Affinities(q);
        // e00/e01 is already active; e02/e11 cannot be completed by adding e10.
        var active = selected[0] with { Id = new string('a', 64), ProducerEssenceId = "e00", ModifierEssenceId = "e01" };
        var incomplete = active with { Id = new string('b', 64), ProducerEssenceId = "e02", ModifierEssenceId = "e11" };
        var input = TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope);
        var c = new TowerAffinityCreation(input, [.. selected, active, incomplete], (_, _) => [], true);
        var row = c.Opportunities(Parent(q), 1, default).Single(r => r.MissingEssences.SequenceEqual(new[] { "e10" })
            && r.PairId == HarnessJson.Hash(new[] { "e00", "e10" }));
        Assert.Equal(new[] { "e00", "e01" }, row.ProtectedEssences);
        Assert.Contains(row.LegalEdits, e => e.Removed.SequenceEqual(new[] { "e02" }));
        Assert.Contains(row.LegalEdits, e => e.Removed.SequenceEqual(new[] { "e09" }));
        var alreadyActive = new TowerAffinityCreation(input, [active], (_, _) => [], true)
            .Create(Parent(q), 1, new Random(1), default);
        Assert.Null(alreadyActive.Party); Assert.Equal("affinities-already-active", alreadyActive.Rejection);
    }

    [Fact]
    public void Two_missing_endpoints_use_minimum_removals_and_do_not_protect_other_owners()
    {
        var q = Request(); var parent = Parent(q);
        var c = new TowerAffinityCreation(TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope), Affinities(q), (_, _) => [], true);
        Assert.All(c.Opportunities(parent, 2, default), r => {
            Assert.Empty(r.ProtectedEssences); Assert.Equal(6, r.LegalEdits.Count);
            Assert.All(r.LegalEdits, e => { Assert.Equal(2, e.Added.Count); Assert.Equal(2, e.Removed.Count); });
        });
        var made = c.Create(parent, 2, new Random(17), default);
        Assert.Null(made.Rejection); Assert.Equal(parent.Builds[1], made.Party!.Builds[1]);
        Assert.Equal(2, made.Step!.Removed.Count); TowerBossDiscovery.ValidateParty(q.Context.Scope, made.Party);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Family_conflicts_and_copy_limits_cannot_bypass_preservation(bool copies)
    {
        var q = Request(); var input = TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope);
        input = copies ? input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e10" ? 0 : 5) }
            : input with { AllowedEssences = input.AllowedEssences.Select(e => e.Id == "e10" ? e with { Family = "FAMILY1" } : e).ToArray() };
        var c = new TowerAffinityCreation(input, Affinities(q), (_, _) => [], true);
        Assert.All(c.Opportunities(Parent(q), 1, default), r => Assert.Empty(r.LegalEdits));
        var blocked = c.Create(Parent(q), 1, new Random(17), default);
        Assert.Null(blocked.Party); Assert.Null(blocked.Step);
        Assert.Equal("no-legal-preserving-affinity-creation", blocked.Rejection);
        if (!copies)
        {
            // The old rule could satisfy the target by deleting the other producer.
            var old = new TowerAffinityCreation(input, Affinities(q), (_, _) => []);
            Assert.Contains(old.Opportunities(Parent(q), 1, default), r => r.LegalEdits.Count > 0);
        }
    }

    private sealed class ChoiceRandom(int pair, int edit) : Random
    {
        public List<int> Bounds { get; } = [];
        public override int Next(int maxValue) { Bounds.Add(maxValue); return Bounds.Count == 1 ? pair : edit; }
    }

    [Fact]
    public void Samples_eligible_pairs_then_their_edits_without_route_multiplicity_weighting()
    {
        var q = Request(); var selected = Affinities(q);
        var extra = selected[0] with { Id = new string('c', 64), ProducerRoute = ["another-route"] };
        var c = new TowerAffinityCreation(TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope), [.. selected, extra], (_, _) => [], true);
        var rows = c.Opportunities(Parent(q), 1, default);
        Assert.Equal(2, rows.Length);
        for (var pair = 0; pair < rows.Length; pair++)
        for (var edit = 0; edit < rows[pair].LegalEdits.Count; edit++)
        {
            var random = new ChoiceRandom(pair, edit);
            var made = c.Create(Parent(q), 1, random, default);
            Assert.Equal(new[] { 2, rows[pair].LegalEdits.Count }, random.Bounds);
            Assert.Equal(rows[pair].PairId, made.Step!.PairId);
            Assert.Equal(rows[pair].LegalEdits[edit].Removed, made.Step.Removed);
        }
    }

    [Fact]
    public void Two_wave_export_is_deterministic_independent_of_arm_order_and_has_seventeen_unique_legal_candidates()
    {
        var q = Request(); var before = HarnessJson.Hash(q); var result = TowerProposalPolicies.Export(q);
        Assert.Equal("Complete", result.Status); Assert.Equal("TwoWaveGenerationOnlyNoMeasurementsOrPromotion", result.Interpretation);
        Assert.Equal(0, result.NewFights); Assert.Equal(0, result.NewReservedValues);
        Assert.Equal(before, HarnessJson.Hash(q)); Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(TowerProposalPolicies.Export(q)));
        var reversed = TowerProposalPolicies.Export(q with { Policies = q.Policies.Reverse().ToArray() });
        foreach (var arm in result.Arms)
        {
            Assert.Equal(HarnessJson.Hash(arm), HarnessJson.Hash(reversed.Arms.Single(a => a.Name == arm.Name)));
            var second = Assert.IsType<TowerProposalSecondWaveExport>(arm.SecondWave);
            Assert.Equal(9, arm.Batch.Candidates.Count); Assert.Equal(8, second.Batch.Candidates.Count);
            Assert.Equal(17, arm.Batch.Candidates.Concat(second.Batch.Candidates).Select(p => p.Id).Distinct().Count());
            Assert.Equal(20, arm.Teams.Count); Assert.Equal(3, arm.Teams.Count(t => t.Role == "reference"));
            Assert.All(arm.Teams, t => Assert.Empty(t.Scenario.Seeds));
            Assert.All(arm.Batch.Candidates, p => Assert.Contains(p.Id, second.Batch.SeenBefore));
            foreach (var batch in new[] { arm.Batch, second.Batch })
            {
                Assert.Empty(batch.BeamIds); Assert.Empty(batch.FeedbackPanels); Assert.Equal(0, batch.AfterEvaluations);
                Assert.All(batch.Candidates, p => TowerBossDiscovery.ValidateParty(q.Context.Scope, p));
                Assert.All(batch.Proposals, p => Assert.Null(p.Fallback));
            }
        }
        // The old first wave and optional-field omission are unchanged in a v3 request.
        var old = TowerProposalPolicies.Export(q with { Version = TowerProposalPolicies.CreationExportVersion,
            Policies = [q.Policies[0], TowerProposalPolicies.BenchmarkSmallEdits()] });
        Assert.Equal(HarnessJson.Hash(result.Arms[0].Batch), HarnessJson.Hash(old.Arms[0].Batch));
        var json = JsonSerializer.Serialize(old, HarnessJson.Options);
        Assert.DoesNotContain("secondWave", json); Assert.DoesNotContain("removalSelection", json);
        Assert.DoesNotContain("creationRemovalRule", JsonSerializer.Serialize(q.Policies[0], HarnessJson.Options));
    }

    [Fact]
    public void Exhausted_neighborhood_is_bounded_and_never_silently_unprotects()
    {
        var q = Request("e00", "e01", "e02", "e09", "e04", "e05", "e06", "e07");
        var result = TowerProposalPolicies.Export(q); var arm = result.Arms[1];
        Assert.Equal("Incomplete", result.Status); Assert.Equal("Incomplete", arm.Status);
        foreach (var batch in new[] { arm.Batch, arm.SecondWave!.Batch })
        {
            Assert.Empty(batch.Candidates); Assert.Equal(128, batch.Proposals.Count);
            Assert.All(batch.Proposals, p => {
                Assert.Equal("no-legal-preserving-affinity-creation", p.Rejection); Assert.Null(p.Fallback); Assert.Null(p.Party);
            });
        }
        Assert.Equal(0, arm.AffinityCreationCoverage!.EligiblePairOwners);
        Assert.Equal(0, arm.SecondWave!.AffinityCreationCoverage!.EligiblePairOwners);
    }

    [Fact]
    public void Complete_first_wave_does_not_hide_duplicate_exhaustion_in_second_wave()
    {
        var q = Request("e00", "e04");
        // Each owner has one producer and three removal choices. Exhausted
        // producer copies prevent the other target's two-endpoint placements.
        var copies = q.Context.Scope.AllowedEssences.ToDictionary(e => e.Id,
            e => e.Id == "e00" ? 1 : e.Id == "e04" ? 4 : 5);
        q = q with { Context = q.Context with { Scope = q.Context.Scope with { OwnedCopies = copies } } };
        var arm = TowerProposalPolicies.Export(q).Arms[1];
        Assert.Equal(9, arm.Batch.Candidates.Count); Assert.Equal(6, arm.SecondWave!.Batch.Candidates.Count);
        Assert.Equal("Incomplete", arm.Status); Assert.Equal(128, arm.SecondWave.Batch.Proposals.Count);
        Assert.Equal(15, arm.Batch.Candidates.Concat(arm.SecondWave.Batch.Candidates).Select(p => p.Id).Distinct().Count());
        Assert.All(arm.SecondWave.Batch.Proposals.Where(p => p.Rejection is not null),
            p => Assert.Equal("duplicate-recipe", p.Rejection));
        Assert.All(arm.Batch.Proposals.Concat(arm.SecondWave.Batch.Proposals), p => {
            Assert.Null(p.Fallback);
            Assert.Equal(1, p.AffinityCreation!.RemovalSelection!.EligiblePairs);
            Assert.Equal(3, p.AffinityCreation.RemovalSelection.EligibleEdits);
        });
    }

    [Theory]
    [InlineData("old-export")]
    [InlineData("old-policy")]
    [InlineData("missing-rule")]
    [InlineData("unknown-rule")]
    [InlineData("beam")]
    [InlineData("recombine")]
    [InlineData("missing-inventory")]
    [InlineData("unknown-affinity")]
    public void Invalid_opt_in_bindings_reject_before_generation(string change)
    {
        var q = Request(); var p = q.Policies[1];
        p = change switch {
            "old-policy" => p with { Version = TowerProposalPolicies.CreationPolicyVersion },
            "missing-rule" => p with { CreationRemovalRule = null },
            "unknown-rule" => p with { CreationRemovalRule = "relax-on-failure" },
            "beam" => p with { ParentTickets = ["beam"] },
            "recombine" => p with { SecondWave = ["recombine", .. p.SecondWave.Skip(1)] },
            "unknown-affinity" => p with { CreatedDamageAffinityIds = [new string('f', 64)] },
            _ => p
        };
        q = q with { Policies = [q.Policies[0], p] };
        if (change == "old-export") q = q with { Version = TowerProposalPolicies.CreationExportVersion };
        if (change == "missing-inventory") q = q with { Context = q.Context with { DamageAffinityInventory = null } };
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Export(q));
    }

    [Fact]
    public async Task Existing_racing_contracts_do_not_admit_the_new_generator()
    {
        var q = Request(); var (racing, _) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        foreach (var version in new[] { TowerProposalPolicies.RacingVersion, TowerProposalPolicies.DamageRacingVersion,
            TowerProposalPolicies.CreationRacingVersion, TowerProposalPolicies.BenchmarkTieRacingVersion, TowerProposalPolicies.BenchmarkValidationRacingVersion })
        {
            var p = new TowerProposalRacingPlan(version, racing, q.Policies[1], q.Context.DamageAffinityInventory);
            if (version == TowerProposalPolicies.BenchmarkValidationRacingVersion)
                p = p with { Racing = racing with { Panels = TowerBenchmarkValidation.PanelRoles.Select((role, i) =>
                    new TowerRacingPanel(role, Enumerable.Range(10000 + 100 * i, i < 4 ? 8 : i == 4 ? 16 : 60).ToArray())).ToArray() },
                    SelectionPolicyVersion = TowerBenchmarkValidation.Version };
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(p,
                (_, _) => throw new InvalidOperationException("Must reject before evaluator dispatch")));
            Assert.Contains("generation-export only", error.Message);
        }
    }

    [Theory]
    [InlineData("protection")]
    [InlineData("eligible-edits")]
    [InlineData("second-wave")]
    public void Rehashed_export_tampering_is_rejected(string change)
    {
        var q = Request(); var output = Path.Combine(root, "export"); var pin = TowerProposalPolicies.WriteExport(q, output);
        Assert.Equal(HarnessJson.Hash(TowerProposalPolicies.Export(q)), HarnessJson.Hash(TowerProposalPolicies.VerifyExport(output, pin)));
        Assert.Throws<IOException>(() => TowerProposalPolicies.WriteExport(q, output));
        var path = Path.Combine(output, "batches.json"); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var arm = json["arms"]![1]!;
        if (change == "second-wave") arm["secondWave"]!["batch"]!["candidates"]!.AsArray().RemoveAt(0);
        else
        {
            var step = arm["batch"]!["proposals"]!.AsArray().First(p => p!["affinityCreation"] is not null)!["affinityCreation"]!["removalSelection"]!;
            if (change == "protection") step["protectedEssences"] = new JsonArray("forged");
            else step["eligibleEdits"] = 999;
        }
        File.WriteAllText(path, json.ToJsonString(HarnessJson.Options));
        File.Delete(Path.Combine(output, "files.json"));
        HarnessJson.WriteNew(Path.Combine(output, "files.json"), new Dictionary<string, string> {
            ["request.json"] = HarnessJson.FileHash(Path.Combine(output, "request.json")), ["batches.json"] = HarnessJson.FileHash(path) });
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.VerifyExport(output, HarnessJson.FileHash(Path.Combine(output, "files.json"))));
    }

    [Fact]
    public void Cancellation_prevents_publication()
    {
        using var source = new CancellationTokenSource(); source.Cancel(); var output = Path.Combine(root, "cancelled");
        Assert.Throws<OperationCanceledException>(() => TowerProposalPolicies.WriteExport(Request(), output, source.Token));
        Assert.False(Directory.Exists(output));
    }
}
