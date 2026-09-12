using System.Text.Json;
using BalanceHarness;
using Domain.Models.WorldTower;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBossDiagnosticsTests
{
    private static readonly Lazy<TowerBossInventoryReport> Actual = new(() =>
        TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));

    [Fact]
    public void Feedback_quartet_matches_barriers_and_separately_withdraws_regeneration_without_touching_fixed_members()
    {
        var (d, inventory) = FeedbackFixture();
        var plan = TowerBossDiagnostics.Create(d, inventory);
        Assert.Equal("planned", plan.Status);
        Assert.Equal("prevention", plan.Enabler);
        Assert.Equal("pressure", plan.Consumer);
        Assert.Equal(d.Refinement!.AnchorId, plan.Parties[0].Id);
        Assert.NotEqual(d.Controls[0].Id, plan.Parties[0].Id);
        Assert.Equal(new[] { "baseline", "enabler-alone", "consumer-alone", "combination" }, plan.Parties.Select(p => p.Source));
        AssertQuartet(d, inventory, plan);
        Assert.Equal("prevention", plan.Parties[1].Builds[1][0]);
        Assert.Equal("regeneration", plan.Parties[1].Builds[2][0]);
        Assert.Equal("recovery", plan.Parties[2].Builds[1][0]);
        Assert.Equal("pressure", plan.Parties[2].Builds[2][0]);
        Assert.Contains("Enemy-healed feedback", plan.Note);
        Assert.Contains("regeneration-to-pressure", plan.Note);
        Assert.Contains("equipment/base regeneration", plan.Note);
        Assert.Contains("3:1=fixed-healer[direct-heal]", plan.Note);
        var reordered = inventory with { Nodes = inventory.Nodes.Reverse().ToArray(), References = inventory.References.Reverse().ToArray(),
            Essences = inventory.Essences.Reverse().ToArray() };
        Assert.Equal(HarnessJson.Hash(plan), HarnessJson.Hash(TowerBossDiagnostics.Create(d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() }, reordered)));
    }

    [Fact]
    public void Existing_healing_denial_is_withdrawn_instead_of_adding_an_already_enabled_bleed_pair()
    {
        var (d, inventory) = FeedbackFixture(false);
        var plan = TowerBossDiagnostics.Create(d, inventory);
        Assert.Equal("planned", plan.Status);
        Assert.Equal("pressure", plan.Enabler);
        Assert.Equal("prevention", plan.Consumer);
        Assert.Equal("pressure", plan.Parties[1].Builds[2][0]);
        Assert.DoesNotContain("denial", plan.Parties[1].Builds.Values.SelectMany(ids => ids));
        Assert.Contains("existing-healing-denial-withdrawal-to-pressure", plan.Note);
        Assert.Contains("2:1=denial", plan.Note);
        AssertQuartet(d, inventory, plan);
    }

    [Fact]
    public void Missing_regeneration_cohort_or_boss_intent_reports_unsupported_without_graph_fallback()
    {
        var (d, inventory) = FeedbackFixture();
        var missingRoute = inventory with { Nodes = inventory.Nodes.Select(n => n.Kind == TowerMechanicNodeKind.Effect
            && n.Key.Contains("regeneration/", StringComparison.Ordinal)
            ? n with { Definition = JsonSerializer.SerializeToElement(Effect("Damage", "CurrentTarget"), HarnessJson.Options) } : n).ToArray() };
        var plan = TowerBossDiagnostics.Create(d, missingRoute);
        Assert.Equal("unsupported", plan.Status);
        Assert.Empty(plan.Parties);
        Assert.Null(plan.Enabler);
        Assert.Contains("No fallback", plan.Note);
        var missingIntent = inventory with { Bosses = inventory.Bosses.Select(b => b with { CounterIntents = ["focused-damage"] }).ToArray() };
        Assert.Equal("unsupported", TowerBossDiagnostics.Create(d, missingIntent).Status);
        Assert.Throws<InvalidDataException>(() => TowerBossDiagnostics.Create(d with { SchemaVersion = 1 }, inventory));
        Assert.Throws<InvalidDataException>(() => TowerBossDiagnostics.Create(d with { Refinement = new("absent") }, inventory));
    }

    [Fact]
    public void Denial_on_a_fixed_member_is_not_misreported_as_absent_to_justify_a_redundant_supplier()
    {
        var (d, inventory) = FeedbackFixture(false);
        var original = d.Controls.Single(p => p.Id == d.Refinement!.AnchorId);
        var anchor = TowerPartySelection.Choice("control", original.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)["recovery", "pressure", "b", "c"] : p.Value));
        var plan = TowerBossDiagnostics.Create(d with { Controls = [anchor], MutablePartySlots = [1], Refinement = new(anchor.Id) }, inventory);
        Assert.Equal("unsupported", plan.Status);
        Assert.Empty(plan.Parties);
    }

    [Fact]
    public void Recovery_screening_follows_applied_statuses_but_does_not_treat_status_reads_as_production()
    {
        var (d, inventory) = FeedbackFixture();
        // Remove every ordinary clean protector; the read-only candidate is eligible despite a healing dependency.
        var onlyReader = d with { AllowedEssences = d.AllowedEssences.Where(id => id != "prevention").ToArray() };
        var plan = TowerBossDiagnostics.Create(onlyReader, inventory);
        Assert.Equal("planned", plan.Status);
        Assert.Equal("reader-protection", plan.Enabler);
        Assert.DoesNotContain(plan.Enabler, new[] { "renewal-protection", "lifesteal-protection", "amplifier-protection", "status-protection", "family-protection", "conditional-protection" });
        var noReader = onlyReader with { AllowedEssences = onlyReader.AllowedEssences.Where(id => id != "reader-protection").ToArray() };
        Assert.Equal("unsupported", TowerBossDiagnostics.Create(noReader, inventory).Status);
    }

    [Fact]
    public void Actual_eydis_retained_specialist_tests_existing_wound_and_a_matched_recovery_substitution()
    {
        var anchor = Recipe("forest_spirit web_weaver_spider elder_treant_thornstorm gnoll_shaman horned_wolf",
            "venomous_spiderling spider_queen_royal_venom nightshade_blossom web_weaver_spider blue_slime",
            "giant_worm web_weaver_spider bog_mite blood_harpy green_slime",
            "poisonous_rat nightshade_blossom alpha_wolf elder_treant_thornstorm green_slime",
            "enchanted_fairy frost_imp goblin giant_bat hollow_stag");
        Assert.StartsWith("e568dfa", anchor.Id, StringComparison.Ordinal);
        var d = Definition(anchor, Actual.Value, 7);
        var plan = TowerBossDiagnostics.Create(d, Actual.Value);
        Assert.Equal("planned", plan.Status);
        AssertQuartet(d, Actual.Value, plan);
        Assert.NotEqual("essence.bog_mite", plan.Parties[1].Builds[3][2]);
        Assert.Equal("essence.bog_mite", plan.Parties[2].Builds[3][2]);
        Assert.Contains("existing-healing-denial-withdrawal", plan.Note);
        Assert.Contains("ApplyCondition:CurrentTarget:Wound", plan.Note);
        Assert.NotEqual("essence.blood_harpy", plan.Enabler);
    }

    [Fact]
    public void Actual_nhalia_retained_progress_anchor_removes_direct_healing_and_regeneration_in_distinct_factors()
    {
        var anchor = Recipe("enchanted_fairy flame_harpy plague_ghoul blue_slime blood_harpy venomous_spiderling venomous_snake",
            "enchanted_fairy lumo_sentinel alpha_wolf ravenous_ghoul cinder_beetle web_weaver_spider ice_harpy",
            "blood_harpy goblin lumo_sentinel plague_ghoul shadow_harpy goblin_archer spider_queen",
            "gnoll_pack_leader flame_imp ravenous_ghoul poisonous_rat raven lumo_sentinel blood_zombie",
            "giant_worm goblin_warrior shadow_harpy frost_imp glade_panther cave_bat enchanted_fairy",
            "enchanted_fairy venomous_snake goblin lumo_sentinel poisonous_rat alpha_wolf rainbow_slime",
            "web_weaver_spider blue_slime hobgoblin_brutal_charge wandering_ghost hollow_stag green_slime giant_bat",
            "viper blue_slime dire_wolf treant_sapling grave_wisp transparent_slime undead",
            "forest_spirit viper grave_wisp goblin_warrior blackjaw_spider glade_panther vampire_bat",
            "elder_treant_thornstorm hobgoblin hollow_stag thornback_boar vampire_fledgeling goblin_archer treant_guardian");
        Assert.StartsWith("a4eaec8", anchor.Id, StringComparison.Ordinal);
        var d = Definition(anchor, Actual.Value, 13);
        var plan = TowerBossDiagnostics.Create(d, Actual.Value);
        Assert.Equal("planned", plan.Status);
        AssertQuartet(d, Actual.Value, plan);
        Assert.Equal("essence.treant_guardian", plan.Parties[1].Builds[10][6]);
        Assert.NotEqual("essence.treant_guardian", plan.Parties[2].Builds[10][6]);
        Assert.Contains("regeneration-to-pressure", plan.Note);
        Assert.Contains("lifesteal", plan.Note);
        Assert.Contains("healing-amplification", plan.Note);
        Assert.NotEqual("essence.wood_nymph", plan.Enabler); // Renewal is a regeneration route despite the inventory's protection intent.
        Assert.NotEqual("essence.blood_harpy", plan.Enabler);
    }

    private static void AssertQuartet(TowerBossSearchDefinition d, TowerBossInventoryReport inventory, BossDiagnosticPlan plan)
    {
        var anchor = d.Controls.Single(p => p.Id == d.Refinement!.AnchorId);
        var families = inventory.Essences.ToDictionary(e => e.Id, e => e.SourceMonsterId);
        Assert.Equal(4, plan.Parties.Select(p => p.Id).Distinct().Count());
        var differences = plan.Parties.Select(p => p.Builds.SelectMany(b => b.Value.Select((id, i) => (b.Key, Index: i, Id: id)))
            .Where(p => anchor.Builds[p.Key][p.Index] != p.Id).Select(p => (p.Key, p.Index)).ToHashSet()).ToArray();
        Assert.Empty(differences[0]); Assert.Single(differences[1]); Assert.Single(differences[2]);
        Assert.Equal(differences[1].Union(differences[2]).Order(), differences[3].Order());
        Assert.Equal(2, differences[3].Count);
        foreach (var party in plan.Parties)
        {
            Assert.Equal(anchor.Builds.Keys.Order(), party.Builds.Keys.Order());
            foreach (var (member, ids) in party.Builds)
            {
                Assert.Equal(anchor.Builds[member].Count, ids.Count);
                Assert.Equal(ids.Count, ids.Distinct().Count());
                Assert.Equal(ids.Count, ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count());
                Assert.All(ids, id => Assert.Contains(id, d.AllowedEssences));
                if (!d.MutablePartySlots.Contains(member)) Assert.Equal(anchor.Builds[member], ids);
            }
        }
    }

    private static PartyChoice Recipe(params string[] members) => TowerPartySelection.Choice("retained-specialist",
        members.Select((ids, i) => (Slot: i + 1, Ids: (IReadOnlyList<string>)ids.Split(' ').Select(id => "essence." + id).ToArray()))
            .ToDictionary(p => p.Slot, p => p.Ids));
    private static TowerBossSearchDefinition Definition(PartyChoice anchor, TowerBossInventoryReport inventory, int floor) =>
        new(2, "test-diagnostic", new(1, new Dictionary<int, double> { [floor] = 1 }, "worst-context-paired-gain", "any"),
            TowerPartyProgression.Budget(anchor.Builds.First().Value.Count) with { PriorityFloor = floor },
            [1], 2, 1, 1, 2, 3, 4, 1, 10000, [], inventory.Essences.Select(e => e.Id).ToArray(),
            anchor.Builds.Keys.Order().ToArray(), [anchor], 10, TowerBossSearch.Methods, ["base", "best"],
            Enumerable.Range(1, 15).ToDictionary(f => f, _ => 1), Refinement: new(anchor.Id));

    private static Dictionary<string, object?> Effect(string operation, string target = "Self", string? condition = null,
        double value = 0, string? attribute = null, double lifesteal = 0, object[]? conditions = null) => new()
        { ["operation"] = operation, ["target"] = target, ["condition"] = condition, ["baseValue"] = value,
            ["attribute"] = attribute, ["lifeStealPercentage"] = lifesteal, ["conditions"] = conditions ?? [],
            ["damageType"] = operation == "Damage" ? "Physical" : "None", ["chancePercent"] = 100 };

    private static (TowerBossSearchDefinition, TowerBossInventoryReport) FeedbackFixture(bool feedback = true)
    {
        var nodes = new List<TowerMechanicNode>(); var refs = new List<TowerMechanicReference>(); var essences = new List<TowerEssenceMechanics>();
        void Add(string id, Dictionary<string, object?>[] effects, string? family = null, string? trigger = null, bool essence = true)
        {
            var key = "Ability:" + id;
            var effectKeys = effects.Select((e, i) => "Effect:" + id + "/" + i).ToArray();
            for (var i = 0; i < effects.Length; i++)
            {
                effects[i]["id"] = effectKeys[i];
                nodes.Add(new(effectKeys[i], TowerMechanicNodeKind.Effect, effectKeys[i], JsonSerializer.SerializeToElement(effects[i], HarnessJson.Options), [], []));
                refs.Add(new(key, effectKeys[i], TowerMechanicRelation.ContainsEffect, "effects", true));
            }
            var triggers = trigger is null ? Array.Empty<object>() : [new { @event = trigger, effectIds = effectKeys, conditions = Array.Empty<object>() }];
            nodes.Add(new(key, TowerMechanicNodeKind.Ability, id, JsonSerializer.SerializeToElement(new { triggers }, HarnessJson.Options), [], []));
            if (trigger is not null)
            {
                var triggerKey = "Trigger:" + id;
                nodes.Add(new(triggerKey, TowerMechanicNodeKind.Trigger, triggerKey, JsonSerializer.SerializeToElement(triggers[0], HarnessJson.Options), [], []));
                refs.Add(new(key, triggerKey, TowerMechanicRelation.ContainsTrigger, "triggers", true));
                refs.AddRange(effectKeys.Select(k => new TowerMechanicReference(triggerKey, k, TowerMechanicRelation.TriggerSelectsEffect, "effectIds", true)));
            }
            if (essence) essences.Add(new(id, id, family ?? id, [id], [key], []));
        }
        Add("boss", [Effect("Heal")], trigger: feedback ? "OnEnemyHealed" : "OnInterval", essence: false);
        Add("recovery", [Effect("Heal", "AllAllies"), Effect("GrantBarrier", "AllAllies")]);
        Add(feedback ? "regeneration" : "denial", feedback ? [Effect("ModifyRegenerationRate", value: 2)] : [Effect("ApplyCondition", "CurrentTarget", "Wound")]);
        Add("prevention", [Effect("GrantBarrier", "AllAllies")]);
        Add("pressure", [Effect("Damage", "CurrentTarget")]);
        Add("fixed-healer", [Effect("Heal")]);
        foreach (var filler in new[] { "a", "b", "c", "d", "e", "f", "x", "y", "z" }) Add(filler, []);
        Add("renewal-protection", [Effect("GrantBarrier", "AllAllies"), Effect("ApplyCondition", condition: "Renewal")]);
        Add("lifesteal-protection", [Effect("GrantBarrier", "AllAllies"), Effect("Damage", "CurrentTarget", lifesteal: 5)]);
        Add("amplifier-protection", [Effect("GrantBarrier", "AllAllies"), Effect("ApplyCondition", condition: "Recovery")]);
        Add("family-protection", [Effect("GrantBarrier", "AllAllies")], family: "A");
        Add("conditional-protection", [Effect("GrantBarrier", "AllAllies", conditions: [new { type = "HasCondition", condition = "Bleed", subject = "Target" }])]);
        Add("conditional-pressure", [Effect("Damage", "CurrentTarget", conditions: [new { type = "HasCondition", condition = "Bleed", subject = "Target" }])]);
        Add("reader-protection", [Effect("GrantBarrier")]);
        Add("status-protection", [Effect("GrantBarrier", "AllAllies")]);
        Add("healing-status", [Effect("Heal")], essence: false);
        // The activation bit, not the broad dependency list, determines whether healing can be produced.
        refs.Add(new("Ability:reader-protection", "Ability:healing-status", TowerMechanicRelation.Reads, "statusId", false));
        refs.Add(new("Ability:status-protection", "Ability:healing-status", TowerMechanicRelation.Applies, "statusId", true));
        var boss = new TowerBossProfile(feedback ? 13 : 7, "Fixture", Guid.Empty, "boss", 2, ["boss"], new TowerFloorDefinition(),
            JsonSerializer.SerializeToElement(new { }), [], [], [], [], [], ["focused-damage", "protection", "denial"]);
        var inventory = new TowerBossInventoryReport(1, new Dictionary<string, string>(), [boss], essences, nodes, refs, [], [], []);
        var anchor = TowerPartySelection.Choice("retained-specialist", new Dictionary<int, IReadOnlyList<string>>
            { [1] = ["recovery", "a", "b", "c"], [2] = [feedback ? "regeneration" : "denial", "d", "e", "f"], [3] = ["fixed-healer", "x", "y", "z"] });
        var control = TowerPartySelection.Choice("control", anchor.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)["pressure", "a", "b", "c"] : p.Value));
        var d = Definition(anchor, inventory, boss.FloorNumber) with { MutablePartySlots = [1, 2], Controls = [control, anchor] };
        return (d, inventory);
    }
}
