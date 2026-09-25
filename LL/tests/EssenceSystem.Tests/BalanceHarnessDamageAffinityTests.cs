using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessDamageAffinityTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-damage-affinity-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Affinity fixture entered combat.")).Activate();
    public BalanceHarnessDamageAffinityTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static readonly Lazy<TowerBossInventoryReport> RealInventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerMechanicNode Node<T>(string key, TowerMechanicNodeKind kind, string id, T value) =>
        new(key, kind, id, JsonSerializer.SerializeToElement(value, HarnessJson.Options), [], []);

    internal static (TowerAdaptiveRacingPlan Plan, TowerBossInventoryReport Inventory) Fixture(bool allSlots = false)
    {
        var plan = BalanceHarnessAdaptiveRacingTests.Plan(17, 5);
        var ids = plan.Scope.Starts.Single(s => s.ReferenceId == plan.BenchmarkReferenceId).Party.Builds[1];
        var nodes = new List<TowerMechanicNode>();
        var essences = plan.Mechanics.Essences.Select(e => {
            var producer = new AbilitySpec { Id = "ability." + e.Id + ".producer", OwningEssenceId = e.Id, Kind = AbilitySpecKind.Active };
            if (allSlots || e.Id == ids[0] || e.Id == ids[1]) producer.Effects.Add(new() {
                Id = "effect." + e.Id + ".poison", Operation = AbilityEffectOperation.ApplyCondition,
                Condition = StandardConditionType.Poison, Target = AbilityTargetSelector.CurrentTarget, BaseValue = 10 });
            var modifier = new AbilitySpec { Id = "ability." + e.Id + ".modifier", OwningEssenceId = e.Id, Kind = AbilitySpecKind.Passive };
            if (allSlots || e.Id == ids[2]) modifier.Effects.Add(new() {
                Id = "effect." + e.Id + ".modifier", Operation = AbilityEffectOperation.ModifyDamageDealt,
                DamageType = DamageType.Poison, Target = AbilityTargetSelector.Self, BaseValue = 7 });
            foreach (var ability in new[] { producer, modifier })
            {
                nodes.Add(Node("Ability:" + ability.Id, TowerMechanicNodeKind.Ability, ability.Id, ability));
                foreach (var effect in ability.Effects)
                    nodes.Add(Node("Effect:Ability:" + ability.Id + "/" + effect.Id, TowerMechanicNodeKind.Effect, effect.Id, effect));
            }
            return e with { AbilityIds = [producer.Id, modifier.Id] };
        }).ToArray();
        plan = plan with { Mechanics = plan.Mechanics with { Essences = essences } };
        return (plan, new(1, plan.Mechanics.SourceHashes, [], essences, nodes, [], plan.Mechanics.Interactions, [], []));
    }
    private static TowerProposalExportRequest Request(bool allSlots = false)
    {
        var (p, inventory) = Fixture(allSlots);
        var policy = TowerProposalPolicies.BenchmarkDamageEdits(TowerDamageSourceAffinities.Create(inventory).Affinities.Select(a => a.Id));
        return new(TowerProposalPolicies.DamageExportVersion,
            new(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed, inventory), [TowerProposalPolicies.BenchmarkSmallEdits(), policy]);
    }
    private static TowerBossInventoryReport Mutate(TowerBossInventoryReport inventory, string abilityId, Action<AbilitySpec> edit)
    {
        var key = "Ability:" + abilityId;
        var ability = inventory.Nodes.Single(n => n.Key == key).Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
        edit(ability);
        var nodes = inventory.Nodes.Where(n => n.Key != key && !n.Key.StartsWith("Effect:" + key + "/") && !n.Key.StartsWith("Trigger:" + key + "/")).ToList();
        nodes.Add(Node(key, TowerMechanicNodeKind.Ability, ability.Id, ability));
        foreach (var effect in ability.Effects) nodes.Add(Node("Effect:" + key + "/" + effect.Id, TowerMechanicNodeKind.Effect, effect.Id, effect));
        for (var i = 0; i < ability.Triggers.Count; i++) nodes.Add(Node("Trigger:" + key + "/" + i, TowerMechanicNodeKind.Trigger, i.ToString(), ability.Triggers[i]));
        return inventory with { Nodes = nodes };
    }

    [Fact]
    public void Reviewed_real_poison_paths_are_separate_from_the_original_target_predicates()
    {
        var inventory = RealInventory.Value; var before = HarnessJson.Hash(inventory);
        var report = TowerDamageSourceAffinities.Create(inventory);
        var pairs = report.Affinities.Where(a => a.ModifierEssenceId == "essence.viper"
            && a.ProducerEssenceId is "essence.spider_queen_royal_venom" or "essence.venomous_spiderling").ToArray();
        Assert.Equal(3, pairs.Length);
        Assert.Equal(2, pairs.Select(a => a.ProducerEssenceId).Distinct().Count());
        Assert.All(pairs, p => {
            Assert.Equal("same-poison-source-owner", p.SourceScope);
            Assert.Contains("potent_toxins", p.ModifierRoute.Last());
            Assert.DoesNotContain("piercing_fangs", p.ModifierRoute.Last());
        });
        Assert.Contains(pairs, p => p.ProducerRoute.Contains("Status:status.spider_queen.royal_venom"));
        Assert.Contains(pairs, p => p.ProducerRoute.Any(k => k.StartsWith("Trigger:Ability:ability.creature.venomous_spiderling.toxic_opportunity")));
        Assert.Equal(before, HarnessJson.Hash(inventory));
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(TowerDamageSourceAffinities.Create(inventory)));
    }

    [Theory]
    [InlineData("proxy")]
    [InlineData("summon")]
    [InlineData("recipient")]
    [InlineData("trigger")]
    [InlineData("modifier-target")]
    [InlineData("modifier-guard")]
    [InlineData("other-damage")]
    public void Unreviewed_source_or_modifier_routes_never_become_protected_affinities(string change)
    {
        var inventory = Fixture().Inventory; var pair = TowerDamageSourceAffinities.Create(inventory).Affinities[0];
        var modifier = change.StartsWith("modifier") || change == "other-damage";
        var abilityId = (modifier ? pair.ModifierRoute : pair.ProducerRoute)[0]["Ability:".Length..];
        inventory = Mutate(inventory, abilityId, a => {
            switch (change)
            {
                case "proxy": a.ConversionFlags.AllowSummonProxy = true; break;
                case "summon": a.Effects[0].Operation = AbilityEffectOperation.Summon; break;
                case "recipient": a.Effects[0].Target = AbilityTargetSelector.EventSource; break;
                case "trigger": a.Kind = AbilitySpecKind.Passive; a.Triggers = [new() { Event = AbilityTriggerEvent.OnEnemyDeath }]; break;
                case "modifier-target": a.Effects[0].Target = AbilityTargetSelector.AllAllies; break;
                case "modifier-guard": a.Effects[0].Conditions = [new() { Type = AbilityConditionType.EventSourceIsSelf }]; break;
                case "other-damage": a.Effects[0].DamageType = DamageType.Burn; break;
            }
        });
        Assert.DoesNotContain(TowerDamageSourceAffinities.Create(inventory).Affinities, a => a.Id == pair.Id);
    }

    [Fact]
    public void Missing_or_conflicting_effect_evidence_and_family_collisions_fail_closed()
    {
        var inventory = Fixture().Inventory; var pair = TowerDamageSourceAffinities.Create(inventory).Affinities[0];
        var key = pair.ProducerRoute.Last();
        Assert.Throws<InvalidDataException>(() => TowerDamageSourceAffinities.Create(inventory with { Nodes = inventory.Nodes.Where(n => n.Key != key).ToArray() }));
        Assert.Throws<InvalidDataException>(() => TowerDamageSourceAffinities.Create(inventory with { Nodes = inventory.Nodes.Concat([inventory.Nodes[0]]).ToArray() }));
        var modifier = inventory.Essences.Single(e => e.Id == pair.ModifierEssenceId);
        var collision = inventory with { Essences = inventory.Essences.Select(e => e.Id == pair.ProducerEssenceId ? e with { SourceMonsterId = modifier.SourceMonsterId } : e).ToArray() };
        Assert.DoesNotContain(TowerDamageSourceAffinities.Create(collision).Affinities, a => a.ProducerEssenceId == pair.ProducerEssenceId);
    }

    [Fact]
    public void Version_two_export_is_distinct_deterministic_and_preserves_the_union_on_original_owners()
    {
        var request = Request(); var before = HarnessJson.Hash(request);
        var result = TowerProposalPolicies.Export(request);
        Assert.Equal("Complete", result.Status); Assert.Empty(result.IdenticalFirstWaveRecipes);
        Assert.Equal(before, HarnessJson.Hash(request)); Assert.NotNull(result.DamageSourceAffinities);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(TowerProposalPolicies.Export(request)));
        var arm = result.Arms[1]; Assert.NotEmpty(arm.ProtectedDamageAffinities!);
        foreach (var candidate in arm.Batch.Candidates)
        foreach (var affinity in arm.ProtectedDamageAffinities!)
        {
            Assert.Contains(affinity.ProducerEssenceId, candidate.Builds[affinity.Owner]);
            Assert.Contains(affinity.ModifierEssenceId, candidate.Builds[affinity.Owner]);
        }
        Assert.All(arm.Teams, t => Assert.Empty(t.Scenario.Seeds));
        var control = request.Policies[1] with { Name = "v2-control", PreservedDamageAffinityIds = [] };
        var noProtection = TowerProposalPolicies.Export(request with { Policies = [request.Policies[0], control] });
        Assert.Single(noProtection.IdenticalFirstWaveRecipes);
        Assert.Empty(noProtection.Arms[1].ProtectedDamageAffinities!);
    }

    [Theory]
    [InlineData("old-export")]
    [InlineData("old-policy")]
    [InlineData("missing-inventory")]
    [InlineData("unknown-affinity")]
    [InlineData("duplicate-affinity")]
    [InlineData("changed-mechanics")]
    public void Invalid_version_or_evidence_bindings_reject_before_generation(string change)
    {
        var q = Request(); var policy = q.Policies[1];
        q = change switch {
            "old-export" => q with { Version = TowerProposalPolicies.ExportVersion },
            "missing-inventory" => q with { Context = q.Context with { DamageAffinityInventory = null } },
            "changed-mechanics" => q with { Context = q.Context with { Mechanics = q.Context.Mechanics with { Essences = [] } } },
            _ => q with { Policies = [q.Policies[0], change switch {
                "old-policy" => policy with { Version = TowerProposalPolicies.Version },
                "unknown-affinity" => policy with { PreservedDamageAffinityIds = [new string('f', 64)] },
                _ => policy with { PreservedDamageAffinityIds = [policy.PreservedDamageAffinityIds![0], policy.PreservedDamageAffinityIds[0]] }
            }] }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Export(q));
    }

    [Fact]
    public async Task Exhaustion_never_weakens_affinity_protection_or_calls_the_evaluator()
    {
        var q = Request(true); var (plan, _) = Fixture(true);
        var arm = TowerProposalPolicies.Export(q).Arms[1];
        Assert.Equal("Incomplete", arm.Status); Assert.Empty(arm.Batch.Candidates);
        Assert.Equal(128, arm.Batch.Proposals.Count);
        Assert.All(arm.Batch.Proposals, p => { Assert.Equal(32, p.ConstructionChecks); Assert.Equal("construction-exhausted", p.Rejection); });
        var calls = 0;
        var result = await TowerProposalPolicies.RunAsync(new(TowerProposalPolicies.DamageRacingVersion, plan, q.Policies[1], q.Context.DamageAffinityInventory),
            (_, _) => { calls++; throw new InvalidOperationException(); });
        Assert.Equal(0, calls); Assert.Equal("Incomplete", result.Evaluation.Status);
    }

    [Fact]
    public async Task Both_waves_use_real_feedback_and_reconstruct_with_bound_affinity_evidence()
    {
        var q = Request(); var (p, inventory) = Fixture();
        var policy = q.Policies[1] with { Name = "affinity-portfolio", FirstWave = TowerProposalPolicies.Legacy().FirstWave,
            SecondWave = TowerProposalPolicies.Legacy().SecondWave, ParentTickets = ["beam"] };
        var plan = new TowerProposalRacingPlan(TowerProposalPolicies.DamageRacingVersion, p, policy, inventory);
        var result = await TowerProposalPolicies.RunAsync(plan, (r, _) => Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(r),
            $"trial-{r.Ordinal:D6}", r.Seed, BattleOutcome.Victory, 50, 50, 1)));
        Assert.Equal("Complete", result.Evaluation.Status); Assert.Equal(528, result.Evaluation.ChargedEvaluations);
        Assert.Equal(152, result.Batches[1].AfterEvaluations);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(plan, result)));
        var all = p.Scope.Starts.Select(s => s.Party).Concat(result.Batches.SelectMany(b => b.Candidates)).ToDictionary(c => c.Id);
        foreach (var proposal in result.Batches.SelectMany(b => b.Proposals).Where(p => p.Rejection is null && p.Parents.Count > 0))
        foreach (var owner in all[proposal.Parents[0]].Builds)
        foreach (var affinity in TowerDamageSourceAffinities.Create(inventory).Affinities.Where(a => owner.Value.Contains(a.ProducerEssenceId) && owner.Value.Contains(a.ModifierEssenceId)))
        {
            Assert.Contains(affinity.ProducerEssenceId, proposal.Party!.Builds[owner.Key]);
            Assert.Contains(affinity.ModifierEssenceId, proposal.Party.Builds[owner.Key]);
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(plan with { Policy = policy with { PreservedDamageAffinityIds = [] } }, result));
    }

    [Fact]
    public void Version_two_archive_verifies_from_inventory_and_rejects_rehashed_affinity_tampering()
    {
        var q = Request(); var output = Path.Combine(root, "export");
        var pin = TowerProposalPolicies.WriteExport(q, output);
        Assert.Equal(HarnessJson.Hash(TowerProposalPolicies.Export(q)), HarnessJson.Hash(TowerProposalPolicies.VerifyExport(output, pin)));
        Assert.Throws<IOException>(() => TowerProposalPolicies.WriteExport(q, output));
        var requestPath = Path.Combine(output, "request.json");
        File.Delete(requestPath);
        HarnessJson.WriteNew(requestPath, q with { Policies = [q.Policies[0], q.Policies[1] with { PreservedDamageAffinityIds = [new string('f', 64)] }] });
        File.Delete(Path.Combine(output, "files.json"));
        HarnessJson.WriteNew(Path.Combine(output, "files.json"), new Dictionary<string, string> {
            ["request.json"] = HarnessJson.FileHash(requestPath), ["batches.json"] = HarnessJson.FileHash(Path.Combine(output, "batches.json")) });
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.VerifyExport(output, HarnessJson.FileHash(Path.Combine(output, "files.json"))));
    }
}
