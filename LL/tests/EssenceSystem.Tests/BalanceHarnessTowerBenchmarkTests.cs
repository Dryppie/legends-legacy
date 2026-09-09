using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBenchmarkTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalog => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures/tower-benchmark.json"));
    private static TowerBenchmarkDefinition Definition => HarnessJson.Read<TowerBenchmarkDefinition>(Catalog);

    [Fact]
    public void Generator_is_bounded_legal_stable_and_fills_multiple_parties()
    {
        var definition = Definition;
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        var released = new Services.LL.WorldTower.JsonWorldTowerDefinitionProvider(
            Path.Combine(Root, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options).GetFloors();
        Assert.Equal(released.Select(f => f.FloorNumber), definition.Floors);
        Assert.Equal(30, scenarios.Count);
        Assert.Equal(600, scenarios.Sum(s => s.Seeds.Count));
        var reordered = TowerBenchmark.Expand(definition with
        {
            Floors = definition.Floors.Reverse().ToArray(), Profiles = definition.Profiles.Reverse().ToArray(),
            Parties = definition.Parties.Reverse().ToArray()
        }, Root, 1337);
        Assert.Equal(HarnessJson.Hash(scenarios), HarnessJson.Hash(reordered));
        var content = new OfflineContent(Root, new ThreatAndTankingOptions());
        var runner = new TowerBattleRunner(Root, content);
        foreach (var scenario in scenarios)
        {
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.Equal(input.Party.Count, input.Party.Select(p => p.Character.Id).Distinct().Count());
            Assert.Equal(Enumerable.Range(1, input.Floor.RequiredSlots).Select(Domain.Models.WorldTower.WorldTowerPartyRules.GetPartyNumber),
                input.Party.Select(p => p.PartyNumber));
        }
        Assert.Equal(scenarios[0].Seeds, scenarios[1].Seeds);
        Assert.NotEqual(scenarios[0].Seeds, scenarios[2].Seeds);
        var large = TowerBenchmark.Expand(definition, Root, 1337, 21);
        Assert.Equal(scenarios[0].Seeds, large[0].Seeds.Take(20));
    }

    [Theory]
    [InlineData("budget")]
    [InlineData("unknown-profile")]
    [InlineData("party-size")]
    [InlineData("unsafe-id")]
    [InlineData("floor")]
    public void Invalid_catalogs_are_rejected_before_execution(string defect)
    {
        var definition = Definition;
        definition = defect switch
        {
            "budget" => definition with { SamplesPerCell = 1001 },
            "unknown-profile" => definition with { Parties = [definition.Parties[0] with { CellProfiles = ["missing", "starter", "starter", "starter", "starter"] }] },
            "party-size" => definition with { Parties = [definition.Parties[0] with { CellProfiles = ["starter"] }] },
            "unsafe-id" => definition with { Parties = [definition.Parties[0] with { Id = "../escape" }] },
            _ => definition with { Floors = [999] }
        };
        Assert.Throws<InvalidDataException>(() => TowerBenchmark.Expand(definition, Root, 1337));
    }

    [Fact]
    public async Task Workflow_compares_replays_and_preserves_reference()
    {
        using var temp = new Temp();
        var before = Path.Combine(temp.Path, "before");
        var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(Root, Catalog, before, samples: 1);
        var referenceHash = HarnessJson.FileHash(Path.Combine(before, "benchmark.json"));
        await TowerBenchmark.RunAsync(Root, Catalog, after, samples: 1, reference: before);
        var report = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", report.Status);
        Assert.Equal(30, report.Cells.Sum(c => c.Pairs));
        Assert.All(report.Cells, c => Assert.Equal(0, c.GameplayChanges));
        Assert.Equal(referenceHash, HarnessJson.FileHash(Path.Combine(before, "benchmark.json")));
        var replay = await TowerBenchmark.ReplayAsync(after, "floor-5.mixed/tower.0001", true, default);
        Assert.NotEmpty(replay.Battle.EventLog!);
        Assert.Equal(10, replay.Battle.Summary.Friendly.Count);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBenchmark.ReplayAsync(after, "../tower.0001", false, default));
        await Assert.ThrowsAsync<IOException>(() => TowerBenchmark.RunAsync(Root, Catalog, after, samples: 1));
        Assert.Throws<IOException>(() => TowerBenchmarkComparison.Create(before, after, Path.Combine(after, "comparison")));
        var different = Path.Combine(temp.Path, "different-seeds");
        await TowerBenchmark.RunAsync(Root, Catalog, different, masterSeed: 42, samples: 1);
        Assert.Equal("Incompatible", TowerBenchmarkComparison.Create(before, different, Path.Combine(temp.Path, "comparison")).Status);
        var alteredCatalog = Path.Combine(temp.Path, "altered.json");
        HarnessJson.WriteNew(alteredCatalog, Definition with { Profiles = Definition.Profiles.Select(p => p.Id == "starter"
            ? p with { Build = p.Build with { Rank = 1 } } : p).ToArray() });
        var altered = Path.Combine(temp.Path, "altered");
        await TowerBenchmark.RunAsync(Root, alteredCatalog, altered, samples: 1);
        var recipeComparison = TowerBenchmarkComparison.Create(before, altered, Path.Combine(temp.Path, "recipe-comparison"));
        Assert.Equal("Incompatible", recipeComparison.Status);
        Assert.Equal(15, recipeComparison.Cells.Count(c => c.Status == "Incompatible" && c.Reason!.Contains("recipe")));
        var scorePath = Path.Combine(after, "benchmark.json");
        var scoreText = File.ReadAllText(scorePath);
        var scoreJson = JsonNode.Parse(scoreText)!;
        scoreJson["validBattles"] = 99;
        File.WriteAllText(scorePath, scoreJson.ToJsonString(HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerBenchmark.ReadSaved(after));
        File.WriteAllText(scorePath, scoreText);
        File.AppendAllText(Path.Combine(after, "cells/floor-5.mixed/battles/tower.0001.json"), " ");
        Assert.Throws<InvalidDataException>(() => TowerBenchmark.ReadSaved(after));
    }

    [Theory]
    [InlineData("equipment")]
    [InlineData("essence")]
    public async Task Fresh_runs_pick_up_balance_edits_without_changing_recipes(string kind)
    {
        using var temp = new Temp();
        var catalog = Path.Combine(temp.Path, "catalog.json");
        HarnessJson.WriteNew(catalog, Definition with { Floors = [1], Parties = [Definition.Parties.Single(p => p.Id == "starter")] });
        var before = Path.Combine(temp.Path, "before");
        await TowerBenchmark.RunAsync(Root, catalog, before, samples: 2);
        // Reuse the captured content and selected settings, never production content.
        var source = Path.Combine(temp.Path, "source");
        CopyDirectory(Path.Combine(before, "content"), source);
        var saved = TowerBenchmark.ReadSaved(before);
        HarnessJson.WriteNew(Path.Combine(source, "appsettings.json"), new Dictionary<string, object>
        {
            ["Combat"] = new Dictionary<string, object>
            {
                ["ThreatAndTanking"] = saved.Input.Settings.Threat,
                ["IdleProgression"] = new Dictionary<string, object> { ["EncounterCadenceSeconds"] = 1 }
            },
            ["WorldTower"] = new Dictionary<string, object> { ["CombatTicksPerFrame"] = saved.Input.Settings.CheckpointIntervalTicks }
        });
        if (kind == "equipment")
        {
            var path = Path.Combine(source, "Data/equipment/equipment-starters.v1.json");
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            json["baseTierBudget"] = 10000;
            File.WriteAllText(path, json.ToJsonString(HarnessJson.Options));
        }
        else
        {
            var path = Path.Combine(source, "Data/combat/abilities.json");
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
            foreach (var ability in json.Where(a => a!["id"]!.GetValue<string>().Contains(".goblin_warrior.")))
                foreach (var effect in ability!["effects"]!.AsArray().Where(e => e!["operation"]!.GetValue<string>() == "Damage"))
                    effect!["scalingCoefficient"] = 100;
            File.WriteAllText(path, json.ToJsonString(HarnessJson.Options));
        }
        var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(source, catalog, after, samples: 2, reference: before);
        var report = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", report.Status);
        Assert.True(report.Cells.Single().GameplayChanges > 0);
        Assert.Contains(report.EvidenceChanges, c => c.Kind == "Content");
        Assert.Equal(HarnessJson.Hash(saved.Input.Scenarios), HarnessJson.Hash(TowerBenchmark.ReadSaved(after).Input.Scenarios));
        if (kind == "equipment") Assert.Contains(report.EvidenceChanges, c => c.Kind == "ResolvedInput");
    }

    [Fact]
    public async Task Partial_cancelled_benchmarks_cannot_be_used_as_complete_references()
    {
        using var temp = new Temp();
        using var cancellation = new CancellationTokenSource();
        var run = Path.Combine(temp.Path, "run");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerBenchmark.RunAsync(Root, Catalog, run,
            samples: 2, token: cancellation.Token, progress: _ => cancellation.Cancel()));
        var saved = TowerBenchmark.ReadSaved(run);
        Assert.Equal("Cancelled", saved.Report.Status);
        Assert.Equal(1, saved.Report.ValidBattles);
        Assert.Equal("Incomplete", TowerBenchmarkComparison.Compare(saved, saved).Status);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBenchmark.RunAsync(Root, Catalog,
            Path.Combine(temp.Path, "candidate"), samples: 2, reference: run));
    }

    [Fact]
    public void Progression_presets_cover_released_floors_with_explicit_legal_budgets_and_paired_seeds()
    {
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-progression.json"));
        Assert.Equal(Definition.Floors, definition.Floors);
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(45, scenarios.Count);
        Assert.Equal(900, scenarios.Sum(s => s.Seeds.Count));
        var budgets = new Dictionary<string, (int Level, int Tier, int Rank, int Essences)>
            { ["early"] = (20, 1, 0, 3), ["mid"] = (50, 2, 3, 6), ["late"] = (90, 2, 5, 10) };
        var content = new OfflineContent(Root, new ThreatAndTankingOptions());
        var runner = new TowerBattleRunner(Root, content);
        foreach (var scenario in scenarios)
        {
            var budget = budgets[scenario.Id.Split('.')[1]];
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.All(input.Party, member =>
            {
                Assert.Equal(budget.Level, member.Character.Level);
                Assert.Equal(7, member.Character.Equipment.Count);
                Assert.All(member.Character.Equipment, e => { Assert.Equal(budget.Tier, e.Data.State.Tier); Assert.Equal(budget.Rank, e.Data.State.Rank); });
                Assert.Equal(budget.Essences, member.Character.Essences.Count);
                Assert.All(member.Character.Essences, e => { Assert.Equal(1, e.Level); Assert.Equal(0, e.AscensionTier); Assert.False(e.IsEvolved); });
            });
        }
        foreach (var floor in definition.Floors)
        {
            var cells = scenarios.Where(s => s.FloorNumber == floor).ToArray();
            Assert.All(cells, c => Assert.Equal(cells[0].Seeds, c.Seeds));
            Assert.Equal(TowerBenchmark.Expand(Definition, Root, 1337).First(s => s.FloorNumber == floor).Seeds, cells[0].Seeds);
        }
    }

    [Fact]
    public async Task Progression_workflow_preserves_recipes_compares_and_replays_each_preset()
    {
        using var temp = new Temp();
        var catalog = Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-progression.json");
        var before = Path.Combine(temp.Path, "before");
        var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(Root, catalog, before, samples: 1);
        await TowerBenchmark.RunAsync(Root, catalog, after, samples: 1, reference: before);
        var comparison = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", comparison.Status);
        Assert.Equal(45, comparison.Cells.Sum(c => c.Pairs));
        Assert.All(comparison.Cells, c => Assert.Equal(0, c.GameplayChanges));
        foreach (var cell in new[] { "floor-1.early", "floor-5.mid", "floor-15.late" })
            Assert.NotEmpty((await TowerBenchmark.ReplayAsync(after, cell + "/tower.0001", true, default)).Battle.EventLog!);
    }

    [Fact]
    public void Factor_presets_change_only_the_declared_axis_and_materialize_every_floor()
    {
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-factors.json"));
        Assert.Equal(Definition.Floors, definition.Floors);
        var parents = new Dictionary<string, (string Parent, string Axis)>
        {
            ["level-70"] = ("control", "level"), ["level-90"] = ("control", "level"),
            ["rank-4"] = ("control", "rank"), ["rank-5"] = ("control", "rank"),
            ["essences-8"] = ("level-90", "essences"), ["essences-10"] = ("level-90", "essences"),
            ["late"] = ("essences-10", "rank")
        };
        foreach (var (child, relation) in parents)
        foreach (var role in new[] { "guardian", "restorer", "striker", "controller" })
        {
            var parent = definition.Profiles.Single(p => p.Id == $"{relation.Parent}-{role}").Build;
            var build = definition.Profiles.Single(p => p.Id == $"{child}-{role}").Build with { Id = parent.Id };
            Assert.NotEqual(HarnessJson.Hash(parent), HarnessJson.Hash(build));
            var normalized = relation.Axis switch
            {
                "level" => build with { CharacterLevel = parent.CharacterLevel },
                "rank" => build with { Rank = parent.Rank },
                _ => build with { EssenceIds = parent.EssenceIds }
            };
            Assert.Equal(HarnessJson.Hash(parent), HarnessJson.Hash(normalized));
            Assert.Equal(parent.EssenceIds, build.EssenceIds.Take(parent.EssenceIds.Count));
        }
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(120, scenarios.Count);
        Assert.Equal(2400, scenarios.Sum(s => s.Seeds.Count));
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new ThreatAndTankingOptions()));
        foreach (var scenario in scenarios)
        {
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.Equal(scenarios.First(s => s.FloorNumber == scenario.FloorNumber).Seeds, scenario.Seeds);
        }
    }

    [Fact]
    public void Essence_slot_catalog_covers_four_through_ten_without_changing_other_build_factors()
    {
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-essence-slots.json"));
        Assert.Equal(Definition.Floors, definition.Floors);
        Assert.Equal(Enumerable.Range(4, 7).Select(n => $"essences-{n}"), definition.Parties.Select(p => p.Id));
        foreach (var role in new[] { "guardian", "restorer", "striker", "controller" })
        {
            var full = definition.Profiles.Single(p => p.Id == $"essences-10-{role}").Build;
            Assert.Equal(90, full.CharacterLevel); Assert.Equal(2, full.Tier); Assert.Equal(3, full.Rank);
            foreach (var count in Enumerable.Range(4, 7))
            {
                var build = definition.Profiles.Single(p => p.Id == $"essences-{count}-{role}").Build;
                Assert.Equal(count, build.EssenceIds.Count);
                Assert.Equal(full.EssenceIds.Take(count), build.EssenceIds);
                Assert.Equal(HarnessJson.Hash(full), HarnessJson.Hash(build with { Id = full.Id, EssenceIds = full.EssenceIds }));
            }
        }
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(105, scenarios.Count); Assert.Equal(2100, scenarios.Sum(s => s.Seeds.Count));
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new ThreatAndTankingOptions()));
        foreach (var scenario in scenarios)
        {
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            var count = int.Parse(scenario.Id.Split('-').Last());
            Assert.All(input.Party, p => Assert.Equal(count, p.Character.Essences.Count));
            Assert.Equal(scenarios.First(s => s.FloorNumber == scenario.FloorNumber).Seeds, scenario.Seeds);
        }
    }

    [Fact]
    public async Task Every_essence_slot_count_repeats_compares_and_replays()
    {
        using var temp = new Temp();
        var catalog = Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-essence-slots.json");
        var before = Path.Combine(temp.Path, "before"); var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(Root, catalog, before, samples: 1);
        await TowerBenchmark.RunAsync(Root, catalog, after, samples: 1, reference: before);
        var comparison = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", comparison.Status); Assert.Equal(105, comparison.Cells.Sum(c => c.Pairs));
        Assert.All(comparison.Cells, c => Assert.Equal(0, c.GameplayChanges));
        foreach (var count in Enumerable.Range(4, 7))
        {
            var replay = await TowerBundle.ReplayAsync(Path.Combine(after, "cells", $"floor-11.essences-{count}"), "tower.0001", true, default);
            Assert.NotEmpty(replay.Battle.EventLog!);
        }
    }

    [Fact]
    public void User_slot_anchors_use_first_unlock_levels_and_legal_full_parties()
    {
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-anchors.json"));
        Assert.Equal(Definition.Floors, definition.Floors);
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(30, scenarios.Count); Assert.Equal(600, scenarios.Sum(s => s.Seeds.Count));
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new ThreatAndTankingOptions()));
        foreach (var scenario in scenarios)
        {
            var slots = scenario.Id.EndsWith(".entry-4", StringComparison.Ordinal) ? 4 : 6;
            var level = (slots - 1) * 10;
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.Equal(slots, Domain.Models.Essences.EssenceSlotProgression.GetUnlockedSlotCount(level));
            Assert.Equal(slots - 1, Domain.Models.Essences.EssenceSlotProgression.GetUnlockedSlotCount(level - 1));
            Assert.All(input.Party, p => { Assert.Equal(level, p.Character.Level); Assert.Equal(slots, p.Character.Essences.Count); });
        }
    }

    [Fact]
    public void Entry_rank_investigation_isolates_reinforcement_and_preserves_the_six_slot_checkpoint()
    {
        var directory = Path.GetDirectoryName(Catalog)!;
        var anchors = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(directory, "tower-anchors.json"));
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(directory, "tower-entry-ranks.json"));
        Assert.Equal(Definition.Floors, definition.Floors);
        foreach (var rank in Enumerable.Range(0, 4))
        foreach (var role in new[] { "guardian", "restorer", "striker", "controller" })
        {
            var control = anchors.Profiles.Single(p => p.Id == $"entry-4-{role}").Build;
            var build = definition.Profiles.Single(p => p.Id == $"rank-{rank}-{role}").Build;
            Assert.Equal(rank, build.Rank);
            Assert.Equal(HarnessJson.Hash(control), HarnessJson.Hash(build with { Id = control.Id, Rank = control.Rank }));
        }
        Assert.Equal(HarnessJson.Hash(anchors.Profiles.Where(p => p.Id.StartsWith("floor-10-6-"))),
            HarnessJson.Hash(definition.Profiles.Where(p => p.Id.StartsWith("floor-10-6-"))));
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(75, scenarios.Count); Assert.Equal(1500, scenarios.Sum(s => s.Seeds.Count));
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new ThreatAndTankingOptions()));
        foreach (var scenario in scenarios)
        {
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.Equal(scenarios.First(s => s.FloorNumber == scenario.FloorNumber).Seeds, scenario.Seeds);
            if (scenario.Id.Contains(".rank-", StringComparison.Ordinal))
                Assert.All(input.Party, p => { Assert.Equal(30, p.Character.Level); Assert.Equal(4, p.Character.Essences.Count); });
        }
        var confirmation = TowerBenchmark.Expand(definition, Root, 8675309, 100);
        Assert.Empty(scenarios.SelectMany(s => s.Seeds).Intersect(confirmation.SelectMany(s => s.Seeds)));
    }

    [Fact]
    public async Task Entry_rank_workflow_repeats_and_replays_each_budget()
    {
        using var temp = new Temp();
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-entry-ranks.json"));
        var catalog = Path.Combine(temp.Path, "catalog.json");
        HarnessJson.WriteNew(catalog, definition with { Floors = [1] });
        var before = Path.Combine(temp.Path, "before"); var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(Root, catalog, before, samples: 1);
        await TowerBenchmark.RunAsync(Root, catalog, after, samples: 1, reference: before);
        var comparison = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", comparison.Status); Assert.Equal(5, comparison.Cells.Sum(c => c.Pairs));
        Assert.All(comparison.Cells, c => Assert.Equal(0, c.GameplayChanges));
        foreach (var rank in Enumerable.Range(0, 4))
            Assert.NotEmpty((await TowerBenchmark.ReplayAsync(after, $"floor-1.rank-{rank}/tower.0001", true, default)).Battle.EventLog!);
    }

    [Fact]
    public void User_uncommon_entry_budget_resolves_real_rarity_quality_rank_and_four_essences()
    {
        var directory = Path.GetDirectoryName(Catalog)!;
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(directory, "tower-entry-uncommon.json"));
        var anchors = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(directory, "tower-anchors.json"));
        Assert.Equal(Definition.Floors, definition.Floors);
        var content = new OfflineContent(Root, new ThreatAndTankingOptions());
        foreach (var quality in new[] { Domain.Models.Items.ItemQuality.Standard, Domain.Models.Items.ItemQuality.Fine })
        foreach (var rank in new[] { 1, 2 })
        foreach (var role in new[] { "guardian", "restorer", "striker", "controller" })
        {
            var control = anchors.Profiles.Single(p => p.Id == $"entry-4-{role}").Build;
            var recipe = definition.Profiles.Single(p => p.Id == $"{quality.ToString().ToLowerInvariant()}-rank-{rank}-{role}").Build;
            Assert.Equal(quality, recipe.Quality); Assert.Equal(rank, recipe.Rank);
            Assert.Equal(control.Equipment.Select(e => e.DefinitionId + ".rarity.uncommon"), recipe.Equipment.Select(e => e.DefinitionId));
            Assert.Equal(HarnessJson.Hash(control), HarnessJson.Hash(recipe with
                { Id = control.Id, Rank = control.Rank, Quality = control.Quality,
                  Equipment = recipe.Equipment.Select(e => e with { DefinitionId = e.DefinitionId.Replace(".rarity.uncommon", "") }).ToArray() }));
            var build = content.CreateBuild(recipe);
            Assert.All(build.Equipment, e => Assert.Equal("Uncommon", e.Rarity.ToString()));
        }
        Assert.Equal(HarnessJson.Hash(anchors.Profiles.Where(p => p.Id.StartsWith("floor-10-6-"))),
            HarnessJson.Hash(definition.Profiles.Where(p => p.Id.StartsWith("floor-10-6-"))));
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(75, scenarios.Count); Assert.Equal(1500, scenarios.Sum(s => s.Seeds.Count));
        var runner = new TowerBattleRunner(Root, content);
        foreach (var scenario in scenarios)
        {
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.Equal(scenarios.First(s => s.FloorNumber == scenario.FloorNumber).Seeds, scenario.Seeds);
            if (scenario.Id.EndsWith(".floor-10-6", StringComparison.Ordinal)) continue;
            var quality = scenario.Id.Contains(".fine-", StringComparison.Ordinal) ? "Fine" : "Standard";
            var rank = int.Parse(scenario.Id.Split('-').Last());
            Assert.All(input.Party, p =>
            {
                Assert.Equal(30, p.Character.Level); Assert.Equal(4, p.Character.Essences.Count);
                Assert.All(p.Character.Equipment, e =>
                {
                    Assert.Equal("Uncommon", e.Data.Rarity.ToString()); Assert.Equal(quality, e.Data.Quality.ToString());
                    Assert.Equal(1, e.Data.State.Tier); Assert.Equal(rank, e.Data.State.Rank);
                });
            });
        }
        var confirmation = TowerBenchmark.Expand(definition with { Floors = [1, 10] }, Root, 20260909, 100);
        Assert.Empty(scenarios.SelectMany(s => s.Seeds).Intersect(confirmation.SelectMany(s => s.Seeds)));
    }

    [Fact]
    public async Task Uncommon_entry_workflow_repeats_and_replays_both_qualities()
    {
        using var temp = new Temp();
        var definition = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-entry-uncommon.json"));
        var catalog = Path.Combine(temp.Path, "catalog.json");
        HarnessJson.WriteNew(catalog, definition with { Floors = [1] });
        var before = Path.Combine(temp.Path, "before"); var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(Root, catalog, before, samples: 1);
        await TowerBenchmark.RunAsync(Root, catalog, after, samples: 1, reference: before);
        var comparison = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", comparison.Status); Assert.Equal(5, comparison.Cells.Sum(c => c.Pairs));
        Assert.All(comparison.Cells, c => Assert.Equal(0, c.GameplayChanges));
        foreach (var party in definition.Parties)
            Assert.NotEmpty((await TowerBenchmark.ReplayAsync(after, $"floor-1.{party.Id}/tower.0001", true, default)).Battle.EventLog!);
    }

    private static TowerBenchmarkDefinition Curve => HarnessJson.Read<TowerBenchmarkDefinition>(
        Path.Combine(Path.GetDirectoryName(Catalog)!, "tower-curve.json"));

    [Fact]
    public void Curve_covers_every_floor_with_legal_progression_and_three_compositions()
    {
        var definition = Curve;
        Assert.Equal(2, definition.SchemaVersion); Assert.Equal(Definition.Floors, definition.Floors);
        Assert.Equal(new[] { "balanced", "support", "pressure" }, definition.Parties.Select(p => p.Id));
        var scenarios = TowerBenchmark.Expand(definition, Root, 1337);
        Assert.Equal(45, scenarios.Count); Assert.Equal(900, scenarios.Sum(s => s.Seeds.Count));
        var slots = new[] { 4, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 8, 9, 10, 10 };
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new ThreatAndTankingOptions()));
        foreach (var scenario in scenarios)
        {
            var count = slots[scenario.FloorNumber - 1];
            var input = runner.CreateInput(scenario, scenario.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            Assert.Equal(scenarios.First(s => s.FloorNumber == scenario.FloorNumber).Seeds, scenario.Seeds);
            Assert.All(input.Party, p =>
            {
                Assert.Equal((count - 1) * 10, p.Character.Level); Assert.Equal(count, p.Character.Essences.Count);
                Assert.All(p.Character.Equipment, e => Assert.Equal("Uncommon", e.Data.Rarity.ToString()));
            });
            var party = definition.Parties.Single(p => scenario.Id.EndsWith("." + p.Id, StringComparison.Ordinal));
            var cell = party.FloorCellProfiles![scenario.FloorNumber];
            Assert.Equal(party.Id == "support" ? 2 : 1, cell.Count(p => p.EndsWith("-restorer", StringComparison.Ordinal)));
            Assert.Equal(party.Id == "pressure" ? 3 : party.Id == "balanced" ? 2 : 1,
                cell.Count(p => p.EndsWith("-striker", StringComparison.Ordinal)));
        }
        // Dashboard floor selection preserves the full map but must use the selected floor's cell.
        var selected = TowerBenchmark.Expand(definition with { Floors = [10] }, Root, 1337);
        Assert.All(selected, s => Assert.All(s.Party, p => Assert.Equal(6, p.Build.EssenceIds.Count)));
        // Optional schema-2 metadata must not alter existing schema-1 manifest hashes.
        var legacy = new TowerBenchmarkParty("legacy", "Fixed budget", ["a", "a", "a", "a", "a"]);
        using var json = JsonDocument.Parse("""{"id":"legacy","assumptions":"Fixed budget","cellProfiles":["a","a","a","a","a"]}""");
        Assert.Equal(HarnessJson.Hash(json.RootElement), HarnessJson.Hash(legacy));
    }

    [Theory]
    [InlineData("missing-floor")]
    [InlineData("unknown-floor")]
    [InlineData("bad-profile")]
    [InlineData("schema-1")]
    public void Curve_rejects_incomplete_or_invalid_floor_maps(string defect)
    {
        var definition = Curve;
        var party = definition.Parties[0];
        var map = party.FloorCellProfiles!.ToDictionary(p => p.Key, p => p.Value);
        if (defect == "missing-floor") map.Remove(10);
        if (defect == "unknown-floor") map[999] = party.CellProfiles;
        if (defect == "bad-profile") map[10] = ["missing", "missing", "missing", "missing", "missing"];
        definition = definition with { SchemaVersion = defect == "schema-1" ? 1 : 2,
            Parties = [party with { FloorCellProfiles = map }] };
        Assert.Throws<InvalidDataException>(() => TowerBenchmark.Expand(definition, Root, 1337));
    }

    [Fact]
    public async Task Curve_workflow_freezes_each_floor_budget_compares_and_replays()
    {
        using var temp = new Temp();
        var catalog = Path.Combine(temp.Path, "curve.json");
        HarnessJson.WriteNew(catalog, Curve with { Floors = [1, 10, 15] });
        var before = Path.Combine(temp.Path, "before"); var after = Path.Combine(temp.Path, "after");
        await TowerBenchmark.RunAsync(Root, catalog, before, samples: 1);
        await TowerBenchmark.RunAsync(Root, catalog, after, samples: 1, reference: before);
        var comparison = HarnessJson.Read<TowerBenchmarkComparisonReport>(Path.Combine(after, "comparison/comparison.json"));
        Assert.Equal("Compared", comparison.Status); Assert.Equal(9, comparison.Cells.Sum(c => c.Pairs));
        Assert.All(comparison.Cells, c => Assert.Equal(0, c.GameplayChanges));
        foreach (var cell in new[] { "floor-1.balanced", "floor-10.support", "floor-15.pressure" })
            Assert.NotEmpty((await TowerBenchmark.ReplayAsync(after, cell + "/tower.0001", true, default)).Battle.EventLog!);
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var path in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
        }
    }
    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-benchmark-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
