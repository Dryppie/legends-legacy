using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCoverageDiagnosticsTests
{
    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value, HarnessJson.Options);
    private static AbilityEffectSpec Effect(string id = "effect", StandardConditionType? condition = null) => new() {
        Id = id, Operation = condition == null ? AbilityEffectOperation.Heal : AbilityEffectOperation.ApplyCondition,
        Target = AbilityTargetSelector.Self, Condition = condition, ChancePercent = 65, Uses = 2 };
    private static AbilitySpec Ability(string id = "ability", string name = "Ability", params AbilityEffectSpec[] effects) => new() {
        Id = id, Name = name, Kind = AbilitySpecKind.Active, CooldownTicks = 200, Effects = effects.ToList() };
    private static TowerBossInventoryReport Inventory(params AbilitySpec[] abilities) => new(1, new Dictionary<string, string>(), [],
        [new("essence", "Essence", "monster", abilities.Select(a => a.Id).ToArray(), [], [])],
        abilities.SelectMany(a => new[] { new TowerMechanicNode("Ability:" + a.Id, TowerMechanicNodeKind.Ability, a.Id, Json(a), [], []) }
            .Concat(a.Effects.Select(e => new TowerMechanicNode("Effect:Ability:" + a.Id + "/" + e.Id, TowerMechanicNodeKind.Effect, e.Id, Json(e), [], [])))).ToArray(),
        [], [], [], []);
    private static CoverageLogRoute Route(string owner = "owner", string ability = "ability", string name = "Ability", AbilityEffectSpec? effect = null)
        => new(owner + "/Effect:Ability:" + ability + "/" + (effect?.Id ?? "effect"), owner, ability, name, effect ?? Effect());
    private static CombatLogItem Event(EventType type, string source, string owner = "owner", string target = "owner", int tick = 0, int magnitude = 1, string name = "Ability")
        => new() { ActorId = owner, TargetId = target, EventType = type, Source = source, StatsSource = name, Timestamp = tick, Magnitude = magnitude };
    private static JsonElement Participants() => Json(new object[] {
        new { slot = new { slotId = "owner", side = "Friendly" }, nativeAbilityIds = Array.Empty<string>(), essences = new[] { new { essenceDefinitionId = "essence" } } },
        new { slot = new { slotId = "ally", side = "Friendly" }, nativeAbilityIds = Array.Empty<string>(), essences = Array.Empty<object>() }
    });
    private static TowerBattleReport Report(IReadOnlyList<CombatLogItem>? events, IReadOnlyList<EntityStats>? stats = null, int duration = 500)
        => new(new(1, "fixture", 17, 10, Participants(), new(BattleOutcome.Defeat, BattleOutcome.Defeat, "Defeat", duration, duration / 10d,
            [], [], stats ?? [new("owner", "Owner", [], Team: "Friendly"), new("ally", "Ally", [], Team: "Friendly")], null!), events), false, 80, duration / 10);
    private static IReadOnlyList<CoverageRoute> Coverage(TowerBossInventoryReport inventory)
    {
        var features = new[] { new BossCoverageFeature("essence", "recovery", ["Effect:Ability:ability/effect"]) };
        return TowerCoverageDiagnosticMechanics.Routes(inventory, features, features);
    }

    [Fact]
    public void Typed_routes_preserve_defaults_selected_triggers_predicates_values_and_exclusions()
    {
        var effect = Effect(condition: StandardConditionType.Stun); effect.StaggerPower = 40;
        effect.Conditions.Add(new() { Type = AbilityConditionType.HealthBelowPercent, Value = 50 });
        var ability = Ability(effects: [effect]); var inventory = Inventory(ability);
        var route = Assert.Single(Coverage(inventory));
        Assert.True(Assert.Single(route.SelectedTriggers).Implicit);
        Assert.Equal(AbilityTriggerEvent.OnAbilityUsed, route.SelectedTriggers[0].Definition.Event);
        Assert.Equal(65, route.AuthoredChancePercent); Assert.Equal(80, route.SeparateRuntimeControlChancePercent);
        Assert.Equal(2, route.Effect.Uses); Assert.Equal(40, route.Effect.StaggerPower);
        Assert.Single(route.Effect.Conditions); Assert.True(route.SelfOnly); Assert.False(route.DeathEventOnly);
        ability.Kind = AbilitySpecKind.Passive;
        Assert.Equal(AbilityTriggerEvent.OnCombatStart, Assert.Single(TowerCoverageDiagnosticMechanics.Triggers(ability, effect.Id)).Definition.Event);
        ability.Triggers = [new() { Event = AbilityTriggerEvent.OnDeath, EffectIds = [effect.Id], InitialDelayTicks = 7, InternalCooldownTicks = 19,
            Conditions = [new() { Type = AbilityConditionType.HealthBelowPercent, Value = 20 }] },
            new() { Event = AbilityTriggerEvent.OnAbilityUsed, EffectIds = ["other"] }];
        var features = new[] { new BossCoverageFeature("essence", "recovery", ["Effect:Ability:ability/effect"]) };
        var selected = Assert.Single(TowerCoverageDiagnosticMechanics.Routes(Inventory(ability), features, []));
        Assert.False(selected.Compatible); Assert.True(selected.DeathEventOnly);
        Assert.False(Assert.Single(selected.SelectedTriggers).Implicit); Assert.Equal(7, selected.SelectedTriggers[0].Definition.InitialDelayTicks);
        Assert.Equal(19, selected.SelectedTriggers[0].Definition.InternalCooldownTicks); Assert.Single(selected.SelectedTriggers[0].Definition.Conditions);
        effect.GuaranteedConditionApplication = true;
        Assert.Null(Assert.Single(Coverage(Inventory(Ability(effects: [effect])))).SeparateRuntimeControlChancePercent);
        Assert.Empty(TowerCoverageDiagnosticMechanics.Triggers(ability, "unselected"));
    }

    [Fact]
    public void Route_output_is_deterministic_when_inventory_and_feature_order_changes()
    {
        var a = Ability("a", "A", Effect("x")); var b = Ability("b", "B", Effect("y"));
        var features = new[] { new BossCoverageFeature("essence", "protection", ["Effect:Ability:b/y"]),
            new BossCoverageFeature("essence", "recovery", ["Effect:Ability:a/x"]) };
        Assert.Equal(HarnessJson.Hash(TowerCoverageDiagnosticMechanics.Routes(Inventory(a, b), features, features)),
            HarnessJson.Hash(TowerCoverageDiagnosticMechanics.Routes(Inventory(b, a), features.Reverse().ToArray(), features)));
    }

    [Theory]
    [InlineData(EventType.Heal, "effect", "Ability", "OwnerEffectId")]
    [InlineData(EventType.StatusEffect, "condition.stun", "Ability", "OwnerAbilityCondition")]
    [InlineData(EventType.StaggerApplied, "Ability", "Ability", "OwnerAbilityStagger")]
    [InlineData(EventType.StaggerBroken, "Ability", "Ability", "OwnerAbilityStagger")]
    public void Matches_operation_specific_sources_and_preserves_recipient(EventType type, string source, string name, string mechanism)
    {
        var m = TowerCoverageDiagnosticMechanics.Match(4, Event(type, source, target: "recipient", name: name), [Route(effect: Effect(condition: StandardConditionType.Stun)), Route("different")]);
        Assert.Equal("Unique", m.Status); Assert.Equal(mechanism, m.Mechanism); Assert.Equal("recipient", m.TargetId); Assert.Equal(4, m.EventIndex);
    }

    [Theory]
    [InlineData(EventType.StaggerRecovered, "Ability", "Ability", "owner")]
    [InlineData(EventType.StatusEffect, "condition.freeze", "Ability", "owner")]
    [InlineData(EventType.StatusEffect, "condition.stun", "Wrong ability", "owner")]
    [InlineData(EventType.StaggerApplied, "Ability", "Ability", "different")]
    public void Does_not_assign_recovery_wrong_condition_wrong_name_or_wrong_owner(EventType type, string source, string name, string owner)
        => Assert.Equal("Unmatched", TowerCoverageDiagnosticMechanics.Match(0, Event(type, source, owner, name: name), [Route(effect: Effect(condition: StandardConditionType.Stun))]).Status);

    [Fact]
    public void Ambiguous_names_effect_ids_and_control_routes_are_not_double_credited()
    {
        var stun = Effect(condition: StandardConditionType.Stun); var freeze = Effect("freeze", StandardConditionType.Freeze);
        var routes = new[] { Route(effect: stun), Route(effect: freeze), Route(ability: "other", effect: stun) };
        var m = TowerCoverageDiagnosticMechanics.Match(0, Event(EventType.StaggerApplied, "Ability"), routes);
        Assert.Equal("Ambiguous", m.Status); Assert.Equal(3, m.CandidateKeys.Count); Assert.Null(m.Mechanism);
        Assert.Equal("Ambiguous", TowerCoverageDiagnosticMechanics.Match(1, Event(EventType.Heal, "effect"), routes).Status);
        Assert.Equal(HarnessJson.Hash(m), HarnessJson.Hash(TowerCoverageDiagnosticMechanics.Match(0, Event(EventType.StaggerApplied, "Ability"), routes.Reverse().ToArray())));
    }

    [Fact]
    public void Effects_outside_coverage_also_compete_for_attribution()
    {
        var inventory = Inventory(Ability(effects: [Effect(), Effect("unclassified", StandardConditionType.Freeze)]),
            Ability("other", "Ability", Effect(condition: StandardConditionType.Stun)));
        var routes = TowerCoverageDiagnosticMechanics.LogRoutes(Participants(), inventory);
        Assert.Equal(3, routes.Count);
        Assert.Equal("Ambiguous", TowerCoverageDiagnosticMechanics.Match(0, Event(EventType.StaggerApplied, "Ability"), routes).Status);
    }

    [Fact]
    public void Vulnerable_uses_the_runtime_vulnerability_condition_identity()
    {
        var routes = new[] { Route(effect: Effect(condition: StandardConditionType.Vulnerable)) };
        Assert.Equal("Unique", TowerCoverageDiagnosticMechanics.Match(0, Event(EventType.StatusEffect, "condition.vulnerability"), routes).Status);
        Assert.Equal("Unmatched", TowerCoverageDiagnosticMechanics.Match(0, Event(EventType.StatusEffect, "condition.vulnerable"), routes).Status);
    }

    [Fact]
    public void Healing_regeneration_windows_and_first_death_reconcile_by_actual_recipient()
    {
        var events = new[] { Event(EventType.AbilityUse, "Ability", target: null!), Event(EventType.Heal, "effect", target: "ally", tick: 99, magnitude: 30),
            Event(EventType.HealthRegeneration, "regen", target: "ally", tick: 100, magnitude: 20),
            Event(EventType.Heal, "effect", target: "ally", tick: 400, magnitude: 10), Event(EventType.Death, "death", target: "ally", tick: 400, magnitude: 0) };
        var stats = new[] { new EntityStats("owner", "Owner", [], Team: "Friendly"), new EntityStats("ally", "Ally", [], HealingReceived: 40, HealthRegenerated: 20, Team: "Friendly") { FirstDeathTick = 400 } };
        var inventory = Inventory(Ability(effects: [Effect()])); var report = Report(events, stats);
        var result = Json(TowerCoverageDiagnosticReplay.Analyze(report, inventory, Coverage(inventory), 10));
        Assert.Equal(40, result.GetProperty("restoredHealth").GetInt32()); Assert.Equal(20, result.GetProperty("regeneratedHealth").GetInt32());
        Assert.Equal(30, result.GetProperty("restoredHealthBefore40Seconds").GetInt32()); Assert.Equal(30, result.GetProperty("restoredHealthBeforeFirstDeath").GetInt32());
        Assert.Equal(30, result.GetProperty("windows")[0].GetProperty("restoredHealth").GetInt32());
        Assert.Equal(20, result.GetProperty("windows")[1].GetProperty("regeneratedHealth").GetInt32());
        Assert.Equal("ally", result.GetProperty("instances")[0].GetProperty("recipients")[0].GetString());
        Assert.Equal(40, TowerCoverageDiagnosticReplay.Metrics(report).FirstDeathSeconds);
        var wrong = report with { Battle = report.Battle with { Summary = report.Battle.Summary with { Statistics = [stats[0] with { HealingReceived = 40 }, stats[1] with { HealingReceived = 0 }] } } };
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnosticReplay.Analyze(wrong, inventory, Coverage(inventory), 10));
    }

    [Fact]
    public void Missing_detail_and_no_observed_application_are_distinct_and_unmatched_events_remain_visible()
    {
        var inventory = Inventory(Ability(effects: [Effect()])); var routes = Coverage(inventory);
        Assert.Equal("MissingDetailedEvidence", Json(TowerCoverageDiagnosticReplay.Analyze(Report(null), inventory, routes, 10)).GetProperty("status").GetString());
        var e = Event(EventType.Buff, "unknown", magnitude: 12);
        var result = Json(TowerCoverageDiagnosticReplay.Analyze(Report([e]), inventory, routes, 10));
        Assert.Equal("NoObservedApplication", result.GetProperty("instances")[0].GetProperty("status").GetString());
        Assert.Equal(1, result.GetProperty("unmatchedApplicationCount").GetInt32());
        Assert.Null(result.GetProperty("firstDeathTick").Deserialize<int?>());
    }

    [Fact]
    public void Stagger_contribution_break_recovery_threshold_change_and_denied_ticks_remain_separate()
    {
        var effect = Effect(condition: StandardConditionType.Stun); var inventory = Inventory(Ability(effects: [effect]));
        var events = new[] { Event(EventType.StaggerApplied, "Ability", target: "boss", tick: 200, magnitude: 250),
            Event(EventType.StaggerBroken, "Ability", target: "boss", tick: 200), Event(EventType.StaggerRecovered, "stagger", target: "boss", tick: 229, magnitude: 0) };
        events[0].CombatEntity = new() { CurrentStagger = 250, MaxStagger = 250 };
        events[2].CombatEntity = new() { CurrentStagger = 0, MaxStagger = 338, IsStaggerRecovering = true };
        var result = Json(TowerCoverageDiagnosticReplay.Analyze(Report(events), inventory, Coverage(inventory), 10));
        var sequence = result.GetProperty("staggerEvents"); Assert.Equal(3, sequence.GetArrayLength());
        Assert.Equal(338, sequence[2].GetProperty("combatEntity").GetProperty("maxStagger").GetInt32());
        Assert.Equal(2, result.GetProperty("instances")[0].GetProperty("applicationEventCount").GetInt32());
        Assert.All(result.GetProperty("denialStatistics").EnumerateArray(), s => Assert.Equal(0, s.GetProperty("actionDeniedTicks").GetInt32()));
    }

    [Fact]
    public void Replay_must_match_the_complete_saved_summary_preparation_and_seed()
    {
        var saved = Report(null); var replay = Report([Event(EventType.Buff, "effect")]);
        TowerCoverageDiagnosticReplay.VerifyReplay(saved, replay);
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnosticReplay.VerifyReplay(saved, saved));
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnosticReplay.VerifyReplay(saved, replay with { Battle = replay.Battle with { Seed = 18 } }));
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnosticReplay.VerifyReplay(saved, replay with { GuardianHealthRemainingPercent = 81 }));
    }

    [Fact]
    public void Historical_harness_change_is_explicit_and_game_runtime_platform_mismatches_are_rejected()
    {
        var current = ExecutionIdentity.Current(); var hashes = current.AssemblyHashes.ToDictionary(p => p.Key, p => p.Value);
        hashes["BalanceHarness"] = new string('a', 64); var prior = current with { AssemblyHashes = hashes }; var expected = HarnessJson.Hash(prior);
        TowerCoverageDiagnostics.VerifyExecution(prior, current, expected, TowerCoverageDiagnostics.HistoricalCompatibility);
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.VerifyExecution(prior, current, expected, "automatic"));
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.VerifyExecution(prior, current, new string('b', 64), TowerCoverageDiagnostics.HistoricalCompatibility));
        foreach (var changed in new[] { current with { Runtime = "other" }, current with { OperatingSystem = "other" }, current with { Architecture = "other" } })
            Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.VerifyExecution(prior, changed, expected, TowerCoverageDiagnostics.HistoricalCompatibility));
        hashes["Domain"] = new string('c', 64);
        Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.VerifyExecution(prior, current, HarnessJson.Hash(prior), TowerCoverageDiagnostics.HistoricalCompatibility));
    }

    [Fact]
    public void Changed_hashes_and_escaping_paths_fail_closed()
    {
        var directory = Path.Combine(Path.GetTempPath(), "coverage-diagnostic-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "input.json"); File.WriteAllText(path, "{}"); var hash = HarnessJson.FileHash(path);
            TowerCoverageDiagnostics.VerifyFile(new(path, hash)); File.WriteAllText(path, "[]");
            Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.VerifyFile(new(path, hash)));
            Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.SafeChild(directory, "../outside"));
            Assert.Throws<InvalidDataException>(() => TowerCoverageDiagnostics.SafeChild(directory, path));
            Assert.Equal(path, TowerCoverageDiagnostics.SafeChild(directory, "input.json"));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void Existing_output_is_rejected_before_any_archive_or_source_read()
    {
        var directory = Path.Combine(Path.GetTempPath(), "coverage-diagnostic-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var definition = new TowerCoverageDiagnosticsDefinition(1, [new("archive", "missing", "", "", TowerCoverageDiagnostics.HistoricalCompatibility)],
                [], new("missing", ""), directory, new Dictionary<string, string> { ["missing"] = "" }, HarnessJson.Hash(ExecutionIdentity.Current()));
            var path = Path.Combine(directory, "definition.json"); HarnessJson.WriteNew(path, definition);
            Assert.Throws<IOException>(() => TowerCoverageDiagnostics.Run(path, directory)); Assert.Single(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
