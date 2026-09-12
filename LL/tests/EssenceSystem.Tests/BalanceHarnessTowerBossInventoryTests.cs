using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossInventoryTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(Root, new()));

    [Fact]
    public void All_fifteen_profiles_match_production_creature_selection_and_every_typed_reference_resolves()
    {
        var report = Inventory.Value;
        Assert.Equal(Enumerable.Range(1, 15), report.Bosses.Select(b => b.FloorNumber));
        var nodes = report.Nodes.ToDictionary(n => n.Key, StringComparer.OrdinalIgnoreCase);
        Assert.All(report.References, r => { Assert.True(nodes.ContainsKey(r.SourceKey), r.SourceKey); Assert.True(nodes.ContainsKey(r.TargetKey), r.TargetKey); });
        foreach (var boss in report.Bosses)
        {
            Assert.Equal(boss.AbilityProfileId, CreatureEssenceSource.GetMonsterDefinitionId(boss.Creature.GetProperty("name").GetString()!));
            Assert.Equal(boss.Definition.GuardianCreatureId, boss.GuardianCreatureId);
            Assert.NotEmpty(boss.AuthoredFacts); Assert.NotEmpty(boss.CounterHypotheses); Assert.NotEmpty(boss.Unknowns);
            Assert.All(boss.AbilityIds, id => Assert.Contains("Ability:" + id, boss.Dependencies));
            Assert.All(boss.Dependencies, key => Assert.Contains(key, nodes.Keys));
            Assert.Equal(boss.Dependencies.Count, boss.Dependencies.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
        Assert.Equal(TowerBossInventory.SourceFiles.Count, report.SourceHashes.Count);
        Assert.All(report.SourceHashes, source => Assert.Equal(HarnessJson.FileHash(Path.Combine(Root, "Data", source.Key)), source.Value));
        Assert.Contains("unconfirmed", TowerBossInventory.Markdown(report));
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(TowerBossInventory.Create(Root, new())));
    }

    [Fact]
    public void Summons_statuses_triggers_targets_and_scaling_are_transitive_and_preserve_restrictions()
    {
        var report = Inventory.Value;
        var brood = report.Bosses.Single(b => b.FloorNumber == 3);
        var nodes = report.Nodes.ToDictionary(n => n.Key, StringComparer.OrdinalIgnoreCase);
        var summons = brood.Dependencies.Select(k => nodes[k]).Where(n => n.Kind == TowerMechanicNodeKind.Summon).ToArray();
        Assert.NotEmpty(summons);
        foreach (var summon in summons)
        {
            var definition = summon.Definition.Deserialize<SummonSpec>(HarnessJson.Options)!;
            // HasReachedSummonCap treats zero as uncapped; preserve the authored value.
            Assert.Equal(0, definition.MaxActive);
            Assert.All(definition.AbilityIds, id => Assert.Contains("Ability:" + id, brood.Dependencies));
        }
        Assert.Contains("add-clearing", brood.CounterIntents);
        var copies = nodes["Summon:niCopy"].Definition.Deserialize<SummonSpec>(HarnessJson.Options)!;
        var copyEffect = nodes.Values.Where(n => n.Kind == TowerMechanicNodeKind.Effect)
            .Select(n => n.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!)
            .Single(e => e.Id == "effect.creature.ni.ninefold.summon");
        Assert.Equal(9, copies.MaxActive);
        Assert.Equal(9, copyEffect.RepeatCount);
        Assert.Equal(AbilityTargetSelector.Self, copyEffect.Target);
        var kodoku = report.Bosses.Single(b => b.FloorNumber == 8);
        var suppression = kodoku.Dependencies.Select(k => nodes[k]).Where(n => n.Kind == TowerMechanicNodeKind.Effect)
            .Select(n => n.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!)
            .Where(e => e.Operation is AbilityEffectOperation.ModifyHealingReceived or AbilityEffectOperation.ModifyRegenerationRate).ToArray();
        Assert.Contains(suppression, e => e.Operation == AbilityEffectOperation.ModifyHealingReceived && e.BaseValue == -80 && e.DurationTicks == 150);
        Assert.Contains(suppression, e => e.Operation == AbilityEffectOperation.ModifyRegenerationRate && e.BaseValue == -80 && e.DurationTicks == 150);
        var nhalia = report.Bosses.Single(b => b.FloorNumber == 13);
        Assert.Contains("trigger:OnEnemyHealed", nhalia.Signals);
        Assert.Contains(nhalia.Dependencies.Select(k => nodes[k]), n => n.Kind == TowerMechanicNodeKind.Effect
            && n.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options) is { Operation: AbilityEffectOperation.Heal, EventMagnitudeCoefficient: 0.2f });
        var serath = report.Bosses.Single(b => b.FloorNumber == 15);
        Assert.Contains(serath.Signals, s => s.Contains("HealthSpread", StringComparison.Ordinal));
        Assert.Contains(report.References, r => r.Relation == TowerMechanicRelation.EventFilter && nodes[r.TargetKey].Kind == TowerMechanicNodeKind.Effect);
        Assert.Contains(report.References, r => r.Relation == TowerMechanicRelation.LinkedEffect);
        Assert.Contains(report.References, r => r.Relation == TowerMechanicRelation.ResetsCooldown);
        Assert.Contains(report.References, r => r.Relation == TowerMechanicRelation.Emits && r.TargetKey.EndsWith("OnEnemyHealed", StringComparison.Ordinal));
        foreach (var emitter in report.References.Where(r => r.Relation == TowerMechanicRelation.Emits && r.TargetKey.EndsWith("trigger:OnHeal", StringComparison.Ordinal)))
        {
            var effect = nodes[emitter.SourceKey].Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
            Assert.False(effect.DurationTicks > 0 && effect.IntervalTicks > 0);
        }
        Assert.Contains(report.TelemetryAudit, a => a.Metric == "Control and target selection" && a.Semantics.Contains("stagger", StringComparison.Ordinal));
        Assert.All(report.TelemetryAudit, a => { Assert.Equal("source-reviewed", a.EvidenceLevel); Assert.NotEmpty(a.Unknowns); });
    }

    [Fact]
    public void Interaction_pairs_are_auditable_hypotheses_and_all_essences_remain_in_the_inventory()
    {
        var report = Inventory.Value;
        var content = new OfflineContent(Root, new());
        Assert.Equal(content.Essences.GetAll().Select(e => e.Id).Order(StringComparer.Ordinal), report.Essences.Select(e => e.Id));
        Assert.NotEmpty(report.EnablerConsumerPairs);
        Assert.DoesNotContain(report.EnablerConsumerPairs, p => p.Mechanism.EndsWith("OnEnemyHealed", StringComparison.Ordinal));
        foreach (var pair in report.EnablerConsumerPairs)
        {
            var enabler = report.Essences.Single(e => e.Id == pair.EnablerEssenceId);
            var consumer = report.Essences.Single(e => e.Id == pair.ConsumerEssenceId);
            Assert.Contains(pair.ProducerNodeKey, enabler.Dependencies);
            Assert.Contains(pair.ConsumerNodeKey, consumer.Dependencies);
            Assert.NotEmpty(pair.Unknowns);
            Assert.Contains(pair.Compatibility, new[] { "same-owner-or-explicit-recipient-required", "recipient-and-trigger-scope-unverified" });
        }
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("creature")]
    [InlineData("scaling-status")]
    [InlineData("event-id")]
    public void Missing_or_mismatched_references_cannot_be_silently_ignored(string defect)
    {
        using var copy = new ContentCopy();
        var path = Path.Combine(copy.Path, "Data", defect is "profile" or "creature" ? "world-tower/tower-floors.json" : "combat/abilities.json");
        var document = JsonNode.Parse(File.ReadAllText(path))!;
        if (defect == "profile") document["floors"]![0]!["guardianAbilityProfileId"] = "monster.missing";
        else if (defect == "creature") document["floors"]![0]!["guardianCreatureId"] = Guid.NewGuid().ToString();
        else if (defect == "scaling-status") document.AsArray().First(a => a!["effects"]!.AsArray().Count > 0)!["effects"]![0]!["scalingStatusId"] = "status.missing";
        else
        {
            var trigger = document.AsArray().SelectMany(a => a!["triggers"]?.AsArray() ?? [])
                .First(t => t!["conditions"]?.AsArray().Any(c => c!["type"]!.GetValue<string>() == "EventIdIs") == true)!;
            trigger["conditions"]!.AsArray().First(c => c!["type"]!.GetValue<string>() == "EventIdIs")!["statusId"] = "effect.missing";
        }
        File.WriteAllText(path, document.ToJsonString());
        Assert.ThrowsAny<Exception>(() => TowerBossInventory.Create(copy.Path, new()));
    }

    [Fact]
    public void Reading_a_status_does_not_claim_to_activate_its_effects_and_cycles_terminate()
    {
        using var copy = new ContentCopy();
        var statusPath = Path.Combine(copy.Path, "Data/combat/statuses.json");
        var statuses = JsonNode.Parse(File.ReadAllText(statusPath))!.AsArray();
        statuses.Add(JsonSerializer.SerializeToNode(new StatusSpec
        {
            Id = "status.inventory.read_only", Name = "Inventory read-only fixture", MaxStacks = 1,
            Effects = [new() { Id = "effect.inventory.read_only.dispel", Operation = AbilityEffectOperation.Dispel,
                Target = AbilityTargetSelector.AllEnemies, BaseValue = 1 }]
        }, HarnessJson.Options));
        File.WriteAllText(statusPath, statuses.ToJsonString());
        var abilityPath = Path.Combine(copy.Path, "Data/combat/abilities.json");
        var abilities = JsonNode.Parse(File.ReadAllText(abilityPath))!.AsArray();
        var essence = Inventory.Value.Essences.First(e => !e.Signals.Contains("operation:Dispel"));
        var ability = abilities.Single(a => a!["id"]!.GetValue<string>() == essence.AbilityIds[0])!;
        var effect = ability["effects"]!.AsArray().First()!;
        effect["conditions"] ??= new JsonArray();
        effect["conditions"]!.AsArray().Add(JsonSerializer.SerializeToNode(new AbilityConditionSpec
        { Type = AbilityConditionType.HasStatus, StatusId = "status.inventory.read_only", Subject = AbilityConditionSubject.Source }, HarnessJson.Options));
        File.WriteAllText(abilityPath, abilities.ToJsonString());
        var report = TowerBossInventory.Create(copy.Path, new());
        var entry = report.Essences.Single(e => e.Id == essence.Id);
        Assert.Contains("Status:status.inventory.read_only", entry.Dependencies);
        Assert.DoesNotContain("operation:Dispel", entry.Signals);
        // Existing status-trigger/event-filter links form cycles in the real catalog; no recursive expansion is required.
        Assert.All(report.Essences, e => Assert.Equal(e.Dependencies.Count, e.Dependencies.Distinct(StringComparer.OrdinalIgnoreCase).Count()));
    }

    private sealed class ContentCopy : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-boss-inventory-" + Guid.NewGuid().ToString("N"));
        public ContentCopy()
        {
            foreach (var file in OfflineContent.Files.Concat(TowerBossInventory.SourceFiles).Distinct(StringComparer.Ordinal))
            {
                var target = System.IO.Path.Combine(Path, "Data", file);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
                File.Copy(System.IO.Path.Combine(Root, "Data", file), target);
            }
        }
        public void Dispose() => Directory.Delete(Path, true);
    }
}
