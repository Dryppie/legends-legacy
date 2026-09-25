using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat.Abilities;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAlliedActionPreservationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-allied-action-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ =>
        throw new InvalidOperationException("Allied-action fixture entered combat or native preparation.")).Activate();
    public BalanceHarnessAlliedActionPreservationTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private static TowerProposalExportRequest Request(string[]? providers = null, string[]? producers = null,
        AbilityTargetSelector target = AbilityTargetSelector.NonSummonedAllies,
        AbilityEffectOperation operation = AbilityEffectOperation.PerformBasicAttack)
    {
        var (p, inventory) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        var nodes = inventory.Nodes.ToList();
        foreach (var id in providers ?? ["e02"])
        {
            var key = "Ability:ability." + id + ".producer";
            var node = nodes.Single(n => n.Key == key);
            var ability = node.Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
            // Deliberately conditional and shared-family ownership: neither names,
            // guaranteed activation nor OwningEssenceId determine equipped roots.
            ability.OwningEssenceId = "shared-base-family";
            var effect = new AbilityEffectSpec { Id = "authored-order", Operation = operation, Target = target, ChancePercent = 35 };
            ability.Effects.Add(effect);
            nodes[nodes.IndexOf(node)] = node with { Definition = JsonSerializer.SerializeToElement(ability, HarnessJson.Options) };
            nodes.Add(new("Effect:" + key + "/" + effect.Id, TowerMechanicNodeKind.Effect, effect.Id,
                JsonSerializer.SerializeToElement(effect, HarnessJson.Options), [], []));
        }
        inventory = inventory with { Nodes = nodes };
        var ids = TowerDamageSourceAffinities.Create(inventory).Affinities
            .Where(a => (producers ?? ["e00", "e01"]).Contains(a.ProducerEssenceId) && a.ModifierEssenceId == "e10")
            .Select(a => a.Id).ToArray();
        return new(TowerProposalPolicies.AlliedActionExportVersion,
            new(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed, inventory),
            [TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids), TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(ids)]);
    }

    private static PartyChoice Parent(TowerProposalExportRequest q) =>
        q.Context.Scope.Starts.Single(s => s.ReferenceId == q.Context.BenchmarkReferenceId).Party;
    private static TowerAffinityCreation Creation(TowerProposalExportRequest q, BossDiscoveryInputs? input = null) =>
        new(input ?? TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope),
            TowerProposalPolicies.SelectedAffinities(q.Context, q.Policies[1], creation: true), (_, _) => [], true,
            TowerAlliedActionProtection.Create(q.Context.DamageAffinityInventory!));
    private static TowerProposalArmExport Candidate(TowerProposalExport result) =>
        result.Arms.Single(a => a.Name == "benchmark-allied-action-affinity-creation-v5");

    [Fact]
    public void Classifies_direct_equipped_effects_and_retains_conditional_source_evidence_without_name_matching()
    {
        var inventory = Request().Context.DamageAffinityInventory!; var before = HarnessJson.Hash(inventory);
        var report = TowerAlliedActionProtection.Create(inventory); var provider = Assert.Single(report.Providers);
        Assert.Equal("e02", provider.EssenceId); Assert.Equal("PerformBasicAttack", provider.Operation);
        Assert.Equal("NonSummonedAllies", provider.Target);
        Assert.Equal(HarnessJson.Hash(inventory.Nodes.Single(n => n.Key == provider.EffectNodeKey).Definition), provider.EffectDefinitionHash);
        Assert.Equal(before, report.InventoryHash); Assert.Equal(before, HarnessJson.Hash(inventory));
        var reordered = inventory with { Nodes = inventory.Nodes.Reverse().ToArray(), Essences = inventory.Essences.Reverse().ToArray() };
        Assert.Equal(report.Providers, TowerAlliedActionProtection.Create(reordered).Providers);
    }

    [Theory]
    [InlineData(AbilityTargetSelector.Self, AbilityEffectOperation.PerformBasicAttack)]
    [InlineData(AbilityTargetSelector.CurrentTarget, AbilityEffectOperation.PerformBasicAttack)]
    [InlineData(AbilityTargetSelector.NonSummonedAllies, AbilityEffectOperation.Heal)]
    public void Matching_signals_do_not_override_operation_or_target(AbilityTargetSelector target, AbilityEffectOperation operation)
    {
        var inventory = Request(target: target, operation: operation).Context.DamageAffinityInventory!;
        inventory = inventory with { Essences = inventory.Essences.Select(e => e with {
            Signals = ["operation:PerformBasicAttack", "target:NonSummonedAllies"] }).ToArray() };
        Assert.Empty(TowerAlliedActionProtection.Create(inventory).Providers);
    }

    [Fact]
    public void Real_inventory_qualified_effect_identity_is_checked_by_key_and_full_definition()
    {
        var inventory = TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new());
        var report = TowerAlliedActionProtection.Create(inventory);
        Assert.NotEmpty(report.Providers);
        foreach (var provider in report.Providers)
        {
            var node = inventory.Nodes.Single(n => n.Key == provider.EffectNodeKey);
            var effect = node.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
            Assert.NotEqual(effect.Id, node.Id); // Graph IDs include their containing ability.
            Assert.Equal("Effect:" + node.Id, node.Key);
            Assert.Equal(AbilityEffectOperation.PerformBasicAttack, effect.Operation);
            Assert.Equal(AbilityTargetSelector.NonSummonedAllies, effect.Target);
            Assert.Equal(HarnessJson.Hash(effect), provider.EffectDefinitionHash);
        }
    }

    [Theory]
    [InlineData("missing-effect")]
    [InlineData("unknown-effect")]
    [InlineData("changed-effect")]
    [InlineData("missing-root")]
    [InlineData("unknown-root")]
    [InlineData("changed-root-id")]
    public void Missing_or_conflicting_evidence_is_rejected(string change)
    {
        var inventory = Request().Context.DamageAffinityInventory!;
        var key = change.Contains("root") ? "Ability:ability.e02.producer" : "Effect:Ability:ability.e02.producer/authored-order";
        inventory = inventory with { Nodes = inventory.Nodes.Where(n => n.Key != key || !change.StartsWith("missing"))
            .Select(n => n.Key != key ? n : change.StartsWith("unknown") ? n with { Unknowns = ["unresolved"] }
                : change == "changed-root-id" ? n with { Id = "forged" }
                : n with { Definition = JsonSerializer.SerializeToElement(new AbilityEffectSpec { Id = "forged" }, HarnessJson.Options) }).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerAlliedActionProtection.Create(inventory));
    }

    [Fact]
    public void Unattached_matching_effect_is_not_an_equipped_provider()
    {
        var inventory = Request(providers: []).Context.DamageAffinityInventory!;
        var effect = new AbilityEffectSpec { Id = "orphan", Operation = AbilityEffectOperation.PerformBasicAttack, Target = AbilityTargetSelector.NonSummonedAllies };
        inventory = inventory with { Nodes = [.. inventory.Nodes, new("Effect:orphan", TowerMechanicNodeKind.Effect, effect.Id,
            JsonSerializer.SerializeToElement(effect, HarnessJson.Options), [], [])] };
        Assert.Empty(TowerAlliedActionProtection.Create(inventory).Providers);
    }

    private sealed class ChoiceRandom(int pair, int edit) : Random
    {
        public List<int> Bounds { get; } = [];
        public override int Next(int maxValue) { Bounds.Add(maxValue); return Bounds.Count == 1 ? pair : edit; }
    }

    [Fact]
    public void Every_provider_is_retained_with_endpoints_and_uniform_pair_then_edit_sampling()
    {
        var q = Request(); var before = HarnessJson.Hash(q); var parent = Parent(q); var c = Creation(q);
        var rows = c.Opportunities(parent, 1, default);
        Assert.All(rows, r => {
            Assert.Equal(new[] { "e00", "e01", "e02" }, r.ProtectedEssences);
            Assert.Equal("e02", Assert.Single(r.AlliedActionProtections!).EssenceId);
            Assert.Equal("e09", Assert.Single(Assert.Single(r.LegalEdits).Removed));
        });
        for (var pair = 0; pair < rows.Length; pair++)
        {
            var random = new ChoiceRandom(pair, 0); var made = c.Create(parent, 1, random, default);
            Assert.Null(made.Rejection); Assert.Equal(new[] { 2, 1 }, random.Bounds);
            Assert.Equal(TowerProposalPolicies.AlliedActionRemovalRule, made.Step!.RemovalSelection!.Rule);
            Assert.Equal(rows[pair].AlliedActionProtections, made.Step.RemovalSelection.AlliedActionProtections);
            Assert.Equal(2, made.Step.NewlyActivatedAffinityIds.Count);
            TowerBossDiscovery.ValidateParty(q.Context.Scope, made.Party!);
            Assert.All(parent.Builds.Keys.Where(o => o != 1), o => Assert.Equal(parent.Builds[o], made.Party!.Builds[o]));
        }
        Assert.All(c.Opportunities(parent, 2, default), r => { Assert.Empty(r.AlliedActionProtections!); Assert.Equal(6, r.LegalEdits.Count); });
        Assert.Equal(before, HarnessJson.Hash(q));
        // Two providers on the same owner both stay, even though one would remain.
        Assert.All(Creation(Request(["e02", "e09"])).Opportunities(parent, 1, default), r => Assert.Empty(r.LegalEdits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Authored_protection_never_bypasses_family_or_copy_legality(bool copies)
    {
        var q = Request(); var input = TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope);
        input = copies ? input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e10" ? 0 : 5) }
            : input with { AllowedEssences = input.AllowedEssences.Select(e => e.Id == "e10" ? e with { Family = "FAMILY1" } : e).ToArray() };
        Assert.Equal("no-legal-allied-action-preserving-affinity-creation", Creation(q, input).Create(Parent(q), 1, new Random(17), default).Rejection);
    }

    [Fact]
    public void Two_waves_are_deterministic_and_old_arm_is_unchanged_including_omitted_metadata()
    {
        var q = Request(); var result = TowerProposalPolicies.Export(q); var arm = Candidate(result);
        Assert.Equal("Complete", arm.Status); Assert.Equal(9, arm.Batch.Candidates.Count); Assert.Equal(8, arm.SecondWave!.Batch.Candidates.Count);
        Assert.Equal(17, arm.Batch.Candidates.Concat(arm.SecondWave.Batch.Candidates).Select(p => p.Id).Distinct().Count());
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(TowerProposalPolicies.Export(q)));
        Assert.Equal(HarnessJson.Hash(arm), HarnessJson.Hash(Candidate(TowerProposalPolicies.Export(q with { Policies = q.Policies.Reverse().ToArray() }))));
        Assert.Equal(0, result.NewFights); Assert.Equal(0, result.NewReservedValues);
        Assert.All(arm.Teams, t => Assert.Empty(t.Scenario.Seeds));
        Assert.All(arm.Batch.Candidates, p => Assert.Contains(p.Id, arm.SecondWave.Batch.SeenBefore));
        foreach (var batch in new[] { arm.Batch, arm.SecondWave.Batch })
        {
            Assert.Equal(0, batch.AfterEvaluations); Assert.Empty(batch.FeedbackPanels); Assert.Empty(batch.BeamIds);
            Assert.All(batch.Candidates, p => TowerBossDiscovery.ValidateParty(q.Context.Scope, p));
            Assert.All(batch.Proposals.Where(p => p.Rejection is null), p => {
                Assert.Null(p.Fallback);
                Assert.DoesNotContain("e02", p.AffinityCreation!.Removed);
            });
        }
        var oldRequest = q with { Version = TowerProposalPolicies.PreservingCreationExportVersion,
            Policies = [q.Policies[0], TowerProposalPolicies.BenchmarkAffinityCreation(q.Policies[0].CreatedDamageAffinityIds!)] };
        var oldArm = TowerProposalPolicies.Export(oldRequest).Arms.Single(a => a.Name == q.Policies[0].Name);
        Assert.Equal(HarnessJson.Hash(oldArm), HarnessJson.Hash(result.Arms.Single(a => a.Name == oldArm.Name)));
        Assert.DoesNotContain("alliedAction", JsonSerializer.Serialize(oldArm, HarnessJson.Options));
    }

    [Fact]
    public void Full_exhaustion_is_explicit_and_cannot_relax_the_rule()
    {
        var q = Request(["e02", "e09", "e04", "e05", "e06", "e07"]); var arm = Candidate(TowerProposalPolicies.Export(q));
        Assert.Equal("Incomplete", arm.Status); Assert.Equal(0, arm.AffinityCreationCoverage!.EligiblePairOwners);
        foreach (var batch in new[] { arm.Batch, arm.SecondWave!.Batch })
        {
            Assert.Empty(batch.Candidates); Assert.Equal(128, batch.Proposals.Count);
            Assert.All(batch.Proposals, p => { Assert.Null(p.Fallback); Assert.Null(p.Party);
                Assert.Equal("no-legal-allied-action-preserving-affinity-creation", p.Rejection); });
        }
    }

    [Fact]
    public void A_complete_first_wave_cannot_hide_second_wave_duplicate_exhaustion()
    {
        var q = Request(producers: ["e00", "e04"]);
        q = q with { Context = q.Context with { Scope = q.Context.Scope with { OwnedCopies =
            q.Context.Scope.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e00" ? 1 : e.Id == "e04" ? 4 : 5) } } };
        var arm = Candidate(TowerProposalPolicies.Export(q));
        Assert.Equal(9, arm.Batch.Candidates.Count); Assert.Equal(5, arm.SecondWave!.Batch.Candidates.Count);
        Assert.Equal("Incomplete", arm.Status); Assert.Equal(128, arm.SecondWave.Batch.Proposals.Count);
        Assert.All(arm.SecondWave.Batch.Proposals.Where(p => p.Rejection is not null), p => Assert.Equal("duplicate-recipe", p.Rejection));
    }

    [Theory]
    [InlineData("old-export")]
    [InlineData("old-policy")]
    [InlineData("missing-rule")]
    [InlineData("old-rule")]
    [InlineData("beam")]
    [InlineData("recombine")]
    [InlineData("missing-inventory")]
    public void Invalid_opt_in_bindings_reject_before_generation(string change)
    {
        var q = Request(); var p = q.Policies[1];
        p = change switch {
            "old-policy" => p with { Version = TowerProposalPolicies.PreservingCreationPolicyVersion },
            "missing-rule" => p with { CreationRemovalRule = null },
            "old-rule" => p with { CreationRemovalRule = TowerProposalPolicies.CompletableAffinityRemovalRule },
            "beam" => p with { ParentTickets = ["beam"] },
            "recombine" => p with { SecondWave = ["recombine", .. p.SecondWave.Skip(1)] }, _ => p
        };
        q = q with { Policies = [q.Policies[0], p] };
        if (change == "old-export") q = q with { Version = TowerProposalPolicies.PreservingCreationExportVersion };
        if (change == "missing-inventory") q = q with { Context = q.Context with { DamageAffinityInventory = null } };
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Export(q));
    }

    [Fact]
    public async Task Every_existing_racing_contract_rejects_the_policy_before_dispatch()
    {
        var q = Request(); var (racing, _) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        for (var i = 1; i <= 6; i++)
        {
            var p = new TowerProposalRacingPlan("tower-proposal-racing-v" + i, racing, q.Policies[1], q.Context.DamageAffinityInventory);
            if (i >= 5) p = p with { Racing = racing with { Panels = TowerBenchmarkValidation.PanelRoles.Select((role, n) =>
                new TowerRacingPanel(role, Enumerable.Range(10000 + 100 * n, n < 4 ? 8 : n == 4 ? 16 : 60).ToArray())).ToArray() },
                SelectionPolicyVersion = TowerBenchmarkValidation.Version };
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(p,
                (_, _) => throw new InvalidOperationException("Must reject before dispatch")));
            Assert.Contains("generation-export only", error.Message);
        }
    }

    [Theory]
    [InlineData("reason")]
    [InlineData("coverage")]
    [InlineData("eligibility")]
    public void Rehashed_protection_evidence_tampering_fails_reconstruction(string change)
    {
        var q = Request(); var output = Path.Combine(root, "export"); var pin = TowerProposalPolicies.WriteExport(q, output);
        Assert.Equal(HarnessJson.Hash(TowerProposalPolicies.Export(q)), HarnessJson.Hash(TowerProposalPolicies.VerifyExport(output, pin)));
        var path = Path.Combine(output, "batches.json"); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var arm = json["arms"]!.AsArray().Single(a => a!["name"]!.GetValue<string>() == q.Policies[1].Name)!;
        if (change == "coverage") arm["affinityCreationCoverage"]!["alliedActionProtection"]!["providers"]!.AsArray().Clear();
        else
        {
            var row = arm["affinityCreationCoverage"]!["opportunities"]!.AsArray()
                .First(r => r!["alliedActionProtections"]!.AsArray().Count != 0)!;
            if (change == "reason") row["alliedActionProtections"]![0]!["effectNodeKey"] = "forged";
            else row["legalEdits"]!.AsArray().Clear();
        }
        File.WriteAllText(path, json.ToJsonString(HarnessJson.Options)); File.Delete(Path.Combine(output, "files.json"));
        HarnessJson.WriteNew(Path.Combine(output, "files.json"), new Dictionary<string, string> {
            ["request.json"] = HarnessJson.FileHash(Path.Combine(output, "request.json")), ["batches.json"] = HarnessJson.FileHash(path) });
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.VerifyExport(output, HarnessJson.FileHash(Path.Combine(output, "files.json"))));
    }
}
