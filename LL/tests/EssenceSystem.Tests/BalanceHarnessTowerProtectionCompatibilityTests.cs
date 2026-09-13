using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerProtectionCompatibilityTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.CompatibleDefenseVersion,
            Methods = TowerBossGeneration.CompatibleDefenseMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 }, Stages = d.Stages with { Shortlist = 8 } };
    }
    private static TowerMechanicNode Node<T>(string key, TowerMechanicNodeKind kind, T value)
        => new(key, kind, key, JsonSerializer.SerializeToElement(value, HarnessJson.Options), [], []);
    private static TowerBossInventoryReport WithEffect(AbilityEffectSpec effect, params TowerMechanicNode[] extra)
        => Inventory.Value with { Bosses = Inventory.Value.Bosses.Select(b => b with { AbilityIds = ["test"] }).ToArray(), References = [],
            Nodes = [Node("Ability:test", TowerMechanicNodeKind.Ability, new AbilitySpec { Id = "test", Effects = [effect] }),
                Node("Effect:Ability:test/" + effect.Id, TowerMechanicNodeKind.Effect, effect), .. extra] };

    [Fact]
    public void Kharad_audit_filters_only_two_explicit_incompatible_routes_without_changing_other_features()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var inventory = Inventory.Value; var hash = HarnessJson.Hash(inventory);
        var m = TowerBossPartyGenerator.FromInventory(input, inventory); var c = m.CompatibleDefense!;
        Assert.True(c.Threat.Complete, string.Join("; ", c.Threat.Limitations)); Assert.Empty(c.Threat.Limitations);
        Assert.Equal(new[] { DamageType.Physical, DamageType.Magical }.Order(), c.Threat.DamageTypes);
        Assert.Equal(2, c.Excluded.Count); Assert.Equal(14, c.Coverage.Count(f => f.Kind == "protection"));
        Assert.Equal(71, m.DefenseCoverage!.Count); Assert.Equal(69, c.Coverage.Count);
        Assert.All(c.Excluded, f => { Assert.Equal("protection", f.Kind); Assert.Contains(input.AllowedEssences, e => e.Id == f.EssenceId); });
        Assert.Equal(HarnessJson.Hash(m.DefenseCoverage.Where(f => f.Kind != "protection")), HarnessJson.Hash(c.Coverage.Where(f => f.Kind != "protection")));
        Assert.Equal(hash, HarnessJson.Hash(inventory));
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.DefenseVersion, Methods = TowerBossGeneration.DefenseMethods } };
        var old = TowerBossPartyGenerator.FromInventory(oldInput, inventory);
        Assert.Equal(HarnessJson.Hash(old), HarnessJson.Hash(m with { CompatibleDefense = null }));
        Assert.DoesNotContain("compatibleDefense", JsonSerializer.Serialize(old, HarnessJson.Options));
        Assert.Equal(HarnessJson.Hash(c), HarnessJson.Hash(TowerProtectionCompatibility.Create(input, inventory with { Nodes = inventory.Nodes.Reverse().ToArray(), References = inventory.References.Reverse().ToArray() })));
    }

    [Fact]
    public void Typed_filter_preserves_matching_generic_mixed_scaled_and_unknown_evidence()
    {
        var e = new AbilityEffectSpec { Id = "reduction", Operation = AbilityEffectOperation.ModifyDamageTaken, BaseValue = -12, DamageType = DamageType.Poison };
        var key = "Effect:Ability:test/reduction"; var feature = new BossCoverageFeature("test", "protection", [key]);
        var threat = new BossIncomingThreat(true, [DamageType.Physical, DamageType.Magical], ["test"], []);
        bool Exclude(AbilityEffectSpec effect, BossIncomingThreat? t = null) => TowerProtectionCompatibility.Exclude(feature, t ?? threat,
            new Dictionary<string, TowerMechanicNode>(StringComparer.OrdinalIgnoreCase) { [key] = Node(key, TowerMechanicNodeKind.Effect, effect) });
        Assert.True(Exclude(e)); Assert.False(Exclude(e, threat with { Complete = false, Limitations = ["unknown"] }));
        Assert.False(Exclude(e, threat with { DamageTypes = [] })); Assert.False(Exclude(e, threat with { DamageTypes = [DamageType.Poison] }));
        e.DamageType = DamageType.None; Assert.False(Exclude(e)); e.DamageType = DamageType.Magical; Assert.False(Exclude(e));
        e.DamageType = DamageType.Poison; e.EventMagnitudeCoefficient = 1; Assert.False(Exclude(e)); e.EventMagnitudeCoefficient = 0;
        e.InheritEventDamageType = true; Assert.False(Exclude(e)); e.InheritEventDamageType = false;
        var general = "Effect:Ability:test/barrier";
        var nodes = new Dictionary<string, TowerMechanicNode>(StringComparer.OrdinalIgnoreCase) {
            [key] = Node(key, TowerMechanicNodeKind.Effect, e),
            [general] = Node(general, TowerMechanicNodeKind.Effect, new AbilityEffectSpec { Id = "barrier", Operation = AbilityEffectOperation.GrantBarrier }) };
        Assert.False(TowerProtectionCompatibility.Exclude(feature with { EvidenceKeys = [key, general] }, threat, nodes));
        Assert.True(TowerProtectionCompatibility.Exclude(feature with { EvidenceKeys = [key.ToUpperInvariant()] }, threat, nodes));
        Assert.False(TowerProtectionCompatibility.Exclude(feature with { EvidenceKeys = ["Effect:missing"] }, threat, nodes));
    }

    [Theory]
    [InlineData(StandardConditionType.Poison, DamageType.Poison)]
    [InlineData(StandardConditionType.Burn, DamageType.Burn)]
    [InlineData(StandardConditionType.Bleed, DamageType.Bleed)]
    public void Threat_audit_includes_periodic_condition_damage(StandardConditionType condition, DamageType expected)
    {
        var report = WithEffect(new() { Id = "condition", Operation = AbilityEffectOperation.ApplyCondition, Condition = condition });
        var threat = TowerProtectionCompatibility.IncomingThreat(report, 5);
        Assert.True(threat.Complete); Assert.Contains(expected, threat.DamageTypes);
    }

    [Fact]
    public void Threat_audit_follows_nested_statuses_summons_and_preserves_unresolved_routes()
    {
        var damage = new AbilityEffectSpec { Id = "tick", Operation = AbilityEffectOperation.Damage, DamageType = DamageType.Shadow, IntervalTicks = 20, DurationTicks = 100 };
        var status = new StatusSpec { Id = "nested", Effects = [damage] };
        var r = WithEffect(new() { Id = "apply", Operation = AbilityEffectOperation.ApplyStatus, StatusId = status.Id },
            Node("Status:nested", TowerMechanicNodeKind.Status, status), Node("Effect:Status:nested/tick", TowerMechanicNodeKind.Effect, damage));
        var t = TowerProtectionCompatibility.IncomingThreat(r, 5); Assert.True(t.Complete); Assert.Contains(DamageType.Shadow, t.DamageTypes);
        Assert.False(TowerProtectionCompatibility.IncomingThreat(r with { Nodes = r.Nodes.Select(n => n.Kind == TowerMechanicNodeKind.Effect ? n with { Unknowns = ["Unresolved execution"] } : n).ToArray() }, 5).Complete);
        Assert.False(TowerProtectionCompatibility.IncomingThreat(r with { Nodes = r.Nodes.Where(n => n.Kind != TowerMechanicNodeKind.Status).ToArray() }, 5).Complete);
        var a = new AbilitySpec { Id = "summon-ability", Effects = [damage] };
        var summon = new SummonSpec { Id = "summon", CanBasicAttack = false, AbilityIds = [a.Id] };
        var s = WithEffect(new() { Id = "summon", Operation = AbilityEffectOperation.Summon, SummonId = summon.Id },
            Node("Summon:summon", TowerMechanicNodeKind.Summon, summon), Node("Ability:" + a.Id, TowerMechanicNodeKind.Ability, a),
            Node("Effect:Ability:" + a.Id + "/tick", TowerMechanicNodeKind.Effect, damage));
        Assert.True(TowerProtectionCompatibility.IncomingThreat(s, 5).Complete);
        summon.CanBasicAttack = true;
        s = s with { Nodes = s.Nodes.Select(n => n.Kind == TowerMechanicNodeKind.Summon ? Node(n.Key, n.Kind, summon) : n).ToArray() };
        Assert.Contains("Summon:summon", TowerProtectionCompatibility.IncomingThreat(s, 5).EvidenceKeys);
        foreach (var effect in new AbilityEffectSpec[] {
            new() { Id = "unknown", Operation = (AbilityEffectOperation)999 },
            new() { Id = "reflect", Operation = AbilityEffectOperation.Damage, DamageType = DamageType.Magical, InheritEventDamageType = true },
            new() { Id = "random", Operation = AbilityEffectOperation.ApplyRandomCondition },
            new() { Id = "delegated", Operation = AbilityEffectOperation.PerformBasicAttack },
            new() { Id = "thorns", Operation = AbilityEffectOperation.ApplyCondition, Condition = StandardConditionType.Thorns } })
            Assert.False(TowerProtectionCompatibility.IncomingThreat(WithEffect(effect), 5).Complete);
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(10, 6)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Construction_is_deterministic_legal_uniform_reachable_and_input_immutable(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var before = HarnessJson.Hash(m); var baseline = new TowerBossPartyGenerator(input, m, attributeDefense: true);
        var filtered = new TowerBossPartyGenerator(input, m, attributeDefense: true, compatibleDefense: true); var uniform = 0;
        for (var seed = 0; seed < 32; seed++)
        {
            var choice = filtered.FreshCoverage(new Random(seed)); Assert.Null(choice.Rejection); TowerBossDiscovery.ValidateParty(d, choice.Party!);
            Assert.Equal(HarnessJson.Hash(choice), HarnessJson.Hash(filtered.FreshCoverage(new Random(seed))));
            if (choice.Intent.Contains("uniform")) { uniform++; Assert.Equal(HarnessJson.Hash(choice), HarnessJson.Hash(baseline.FreshCoverage(new Random(seed)))); }
        }
        Assert.True(uniform > 0); Assert.Equal(before, HarnessJson.Hash(m));
        var unknown = m with { CompatibleDefense = m.CompatibleDefense! with { Threat = m.CompatibleDefense.Threat with { Complete = false, Limitations = ["unknown"] }, Coverage = m.DefenseCoverage!, Excluded = [] } };
        var fallback = new TowerBossPartyGenerator(input, unknown, attributeDefense: true, compatibleDefense: true);
        Assert.Equal(HarnessJson.Hash(baseline.FreshCoverage(new Random(2))), HarnessJson.Hash(fallback.FreshCoverage(new Random(2))));
    }

    [Fact]
    public async Task V9_preserves_v8_comparator_and_restricts_provenance()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var result = await TowerBossGeneration.RunAsync(input, m, Score); Assert.Equal("Complete", result.Status);
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.DefenseVersion, Methods = TowerBossGeneration.DefenseMethods } };
        var old = await TowerBossGeneration.RunAsync(oldInput, TowerBossPartyGenerator.FromInventory(oldInput, Inventory.Value), Score);
        Assert.Equal(HarnessJson.Hash(old.Arms.Single(a => a.Method == "defense-joint")), HarnessJson.Hash(result.Arms[0]));
        var provenance = result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray(); TowerBossDiscovery.ValidateProvenance(d, provenance);
        var fresh = result.Arms[1].Proposals.First(p => p.Provenance.Operator == "fresh-compatible-defense").Provenance;
        Assert.Empty(fresh.ParentIds); Assert.All(provenance, p => Assert.Empty(p.ReferenceIds));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, provenance.Select(p => p.Id == fresh.Id ? fresh with { Method = "defense-joint" } : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossGeneration.CompatibleDefenseMethods.Reverse().ToArray() } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { CompatibleDefense = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(oldInput, m));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { CompatibleDefense = m.CompatibleDefense! with { Coverage = [], Excluded = [] } }));
        var limited = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var generator = new TowerBossPartyGenerator(limited, m, attributeDefense: true, compatibleDefense: true); var accepted = 0;
        for (var seed = 0; seed < 24; seed++)
        {
            var c = generator.FreshCoverage(new Random(seed)); if (c.Rejection is not null) continue;
            accepted++; Assert.Null(generator.Invalid(c.Party!)); Assert.All(c.Party!.Builds.Values.SelectMany(x => x).GroupBy(x => x), copies => Assert.Single(copies));
        }
        Assert.True(accepted > 0);
    }
}
