using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Essences;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record HandoffBudget(long StartingCinders, long FuryCinders, long CindersAfterFury,
    int FuryBlueprints, int LumoTokens, int JewelryChests, long FirstRankCinders, int FirstRankParts);
public sealed record HandoffPlan(string Id, IdleSuiteDefinition Suite, string FixtureHash,
    IReadOnlyList<EntrySeedSet> SeedSets, int PlannedBattles, HandoffBudget Budget,
    IReadOnlyDictionary<string, string> SourceHashes, string SettingsHash, string ExecutionHash,
    string StoppingRule, string ReplaySelection);
public sealed record HandoffFinding(string SeedSet, CellScorecard Cell, string? PreviousBuild,
    PairedEstimate? ClearRateChange);
public sealed record HandoffReport(string Status, string Policy, int ValidBattles,
    IReadOnlyList<HandoffFinding> Cells, IReadOnlyList<string> Replays,
    IReadOnlyDictionary<string, string> RunArtifactHashes);

/// <summary>Costed level-10 preparation steps, using existing suite, evidence and paired-statistics contracts.</summary>
public static class CrystalCreekHandoff
{
    public const string Fixture = "idle-crystal-creek-starter.json";
    private static readonly string[] Builds = ["level-only", "jewelry", "quest-rewards", "fury"];
    private static readonly string[] EconomicSources =
    [
        "quests/onboarding/soul-archive.v3.json", "quests/region-01/into-lumo-ruins.v2.json",
        "quests/region-01/trial-of-lumo.v4.json", "quests/region-01/blood-in-the-grove.v4.json",
        "equipment/equipment-blueprints.v1.json", "equipment/equipment-upgrades.v1.json",
        "equipment/equipment-ordinary.v1.json", "dungeons/dungeons.json"
    ];

    public static HandoffPlan CreatePlan(string root, string fixture, int samples)
    {
        if (samples is < 1 or > 500) throw new ArgumentOutOfRangeException(nameof(samples));
        var suite = HarnessJson.Read<IdleSuiteDefinition>(fixture) with { SamplesPerCell = samples };
        if (suite.Id != "idle-crystal-creek-starter-v1" || suite.Stages.Count != 2
            || !suite.Stages.Select(s => s.AreaId).SequenceEqual(new[] { "region_01_area_02", "region_01_area_03" })
            || suite.Stages.Any(s => s.Encounters.Count != 2 || s.EssenceLevels is not null || s.CombatStyles is not null
                || !s.Builds.Select(b => b.Id).SequenceEqual(Builds)
                || s.Builds.Any(b => b.CharacterLevel != 10 || b.Tier != 1 || b.Rank != 0)))
            throw new InvalidDataException("The level-10 checkpoint contract changed; review the protocol.");
        if (HarnessJson.Hash(suite.Stages[0].Builds) != HarnessJson.Hash(suite.Stages[1].Builds))
            throw new InvalidDataException("Both areas must use identical preparation steps.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(root);
        var content = new OfflineContent(root, threat);
        var budget = ReadBudget(root, content);
        var sets = samples == 500
            ? new[] { new EntrySeedSet("reference", 618091), new EntrySeedSet("confirmation", 618092) }
            : new[] { new EntrySeedSet("reference", 618093), new EntrySeedSet("confirmation", 618094) };
        // Resolve, but never execute, the reserved and development schedules. Builds intentionally share seeds.
        var schedules = new[] { 618091, 618092, 618093, 618094 }.Select(seed =>
            IdleSuite.Resolve(suite with { SamplesPerCell = 500 }, content, threat, cadence, seed)
                .Cells.SelectMany(c => c.Trials.Select(t => t.Seed)).ToHashSet()).ToArray();
        for (var i = 0; i < schedules.Length; i++)
        for (var j = i + 1; j < schedules.Length; j++)
            if (schedules[i].Overlaps(schedules[j])) throw new InvalidDataException("Reserved schedules overlap.");
        return new("crystal-creek-handoff-v1", suite, BalanceGoals.FixtureContractHash(suite), sets, samples * 32,
            budget, OfflineContent.Files.Concat(EconomicSources).Distinct().ToDictionary(f => f,
                f => HarnessJson.FileHash(Path.Combine(root, "Data", f))), HarnessJson.Hash(new { threat, cadence }),
            HarnessJson.Hash(ExecutionIdentity.Current()),
            "Run every level-10 cell on both fixed seed sets once; no selection, tuning, pooling, sample extension, goal evaluation or baseline promotion. Do not rerun the level-5 reference.",
            "Confirmation trial index 0 for quest-rewards and fury in both areas and both encounters: eight detailed replays, independent of outcomes.");
    }

    private static HandoffBudget ReadBudget(string root, OfflineContent content)
    {
        JsonElement Read(string file) => HarnessJson.Read<JsonElement>(Path.Combine(root, "Data", file));
        int Grant(string file, string item) => Read(file).GetProperty("rewards").EnumerateArray()
            .Where(r => r.TryGetProperty("itemBaseId", out var id) && id.GetString() == item)
            .Sum(r => r.GetProperty("quantity").GetInt32());
        var cinders = Read(EconomicSources[0]).GetProperty("rewards").EnumerateArray()
            .Where(r => r.GetProperty("type").GetString() == "Cinders").Sum(r => r.GetProperty("quantity").GetInt64());
        var blueprints = JsonEquipmentBlueprintCatalog.Load(Path.Combine(root, "Data", EconomicSources[4]), content.Equipment);
        var blueprint = blueprints.Find(BloodGroveEntryExperiment.FuryStyle) ?? throw new InvalidDataException("Missing Fury.");
        var jewelry = RandomEquipmentBoxCatalog.JewelryChest.RandomEquipment!;
        var acquisition = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(root, "Data", EconomicSources[6]));
        var amulet = acquisition.BaseDropDefinitions(jewelry.Rarity).Single(x => x.Id == "plain.amulet");
        if (!jewelry.EquipmentTypes!.Contains(content.Equipment.Evaluator.GetArchetype(amulet.ArchetypeId).EquipmentType)
            || jewelry.Quantity != 1 || jewelry.Tier != 1 || jewelry.Rank != 0
            || EssenceSlotProgression.GetUnlockedSlotCount(10) != 2
            || !SelectionContainerCatalog.Find("item.essence_token.lumo_ruins")!.Options.Any(o => o.ItemId == "item.essence.goblin"))
            throw new InvalidDataException("The selected rewards or second Essence slot are unavailable.");
        var rank = Read(EconomicSources[5]).GetProperty("tiers").EnumerateArray().Single(t => t.GetProperty("tier").GetInt32() == 1);
        var budget = new HandoffBudget(cinders, blueprints.CindersPerTier, cinders - blueprints.CindersPerTier,
            Grant(EconomicSources[0], blueprint.ItemId), Grant(EconomicSources[2], "item.essence_token.lumo_ruins"),
            Grant(EconomicSources[3], RandomEquipmentBoxCatalog.JewelryChestItemBaseId),
            rank.GetProperty("rankCinderCosts")[0].GetInt64(), rank.GetProperty("rankPartCosts")[0].GetInt32());
        if (budget.FuryBlueprints != 1 || budget.LumoTokens != 1 || budget.JewelryChests != 1
            || budget.CindersAfterFury < 0 || Grant(EconomicSources[1], RandomEquipmentBoxCatalog.ArmorChestItemBaseId) != 1)
            throw new InvalidDataException("The guaranteed reward budget no longer supports the checkpoint.");
        return budget;
    }

    public static async Task<HandoffReport> RunAsync(string root, string fixture, string outputDirectory,
        int samples, CancellationToken token, Action<string>? progress = null)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Output already exists: {output}");
        token.ThrowIfCancellationRequested();
        var plan = CreatePlan(root, fixture, samples);
        var fixtureFileHash = HarnessJson.FileHash(fixture);
        Directory.CreateDirectory(output);
        try
        {
            HarnessJson.WriteNew(Path.Combine(output, "plan.json"), plan);
            var recipe = Path.Combine(output, "suite.json");
            HarnessJson.WriteNew(recipe, plan.Suite);
            File.Copy(fixture, Path.Combine(output, "source-fixture.json"));
            foreach (var source in plan.SourceHashes)
            {
                var target = Path.Combine(output, "sources", source.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(root, "Data", source.Key), target);
                if (HarnessJson.FileHash(target) != source.Value) throw new InvalidDataException("Source changed during capture.");
            }
            var findings = new List<HandoffFinding>();
            var replays = new List<string>();
            var hashes = new Dictionary<string, string>();
            foreach (var set in plan.SeedSets)
            {
                token.ThrowIfCancellationRequested();
                Verify();
                progress?.Invoke($"{set.Id}: {16 * samples} battles");
                var run = Path.Combine(output, set.Id);
                var report = await SuiteBundle.CreateAsync(root, recipe, run, set.MasterSeed, null, token);
                if (report.Status != "Complete")
                {
                    token.ThrowIfCancellationRequested();
                    throw new InvalidDataException("Incomplete checkpoint run.");
                }
                var saved = SavedSuite.Read(run, token);
                if (HarnessJson.Hash(saved.Manifest.Execution) != plan.ExecutionHash
                    || OfflineContent.Files.Any(f => saved.Manifest.ContentHashes[f] != plan.SourceHashes[f]))
                    throw new InvalidDataException("Combat content or execution changed.");
                hashes.Add(set.Id, saved.ArtifactHash);
                foreach (var stage in plan.Suite.Stages)
                foreach (var encounter in stage.Encounters)
                for (var index = 0; index < Builds.Length; index++)
                {
                    var id = $"{stage.Id}.{Builds[index]}.{encounter.Id}";
                    var previous = index == 0 ? null : Builds[index - 1];
                    findings.Add(new(set.Id, saved.Scorecard.Cells.Single(c => c.CellId == id), previous,
                        previous is null ? null : BloodGroveEntryExperiment.PairedClearRate(saved,
                            $"{stage.Id}.{previous}.{encounter.Id}", id)));
                    if (set.Id == "confirmation" && index >= 2)
                    {
                        var battle = saved.Input.Cells.Single(c => c.Id == id).Trials[0].BattleId;
                        var replay = await SuiteBundle.ReplayAsync(run, battle, true, token);
                        var relative = $"replays/{battle}.json";
                        Directory.CreateDirectory(Path.Combine(output, "replays"));
                        HarnessJson.WriteNew(Path.Combine(output, relative), replay);
                        replays.Add(relative);
                    }
                }
                Verify();
            }
            var result = new HandoffReport("Complete", "Advisory", findings.Sum(f => f.Cell.Valid), findings, replays, hashes);
            if (result.ValidBattles != plan.PlannedBattles || replays.Count != 8) throw new InvalidDataException("Incomplete budget or replay coverage.");
            HarnessJson.WriteNew(Path.Combine(output, "results.json"), result);
            await File.WriteAllTextAsync(Path.Combine(output, "summary.md"), Markdown(result), token);
            return result;

            void Verify()
            {
                var (threat, cadence) = RunBundle.ReadCombatSettings(root);
                if (HarnessJson.FileHash(fixture) != fixtureFileHash
                    || HarnessJson.FileHash(Path.Combine(output, "source-fixture.json")) != fixtureFileHash
                    || HarnessJson.Hash(HarnessJson.Read<IdleSuiteDefinition>(recipe)) != HarnessJson.Hash(plan.Suite)
                    || HarnessJson.Hash(new { threat, cadence }) != plan.SettingsHash
                    || HarnessJson.Hash(ExecutionIdentity.Current()) != plan.ExecutionHash
                    || plan.SourceHashes.Any(p => HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) != p.Value
                        || HarnessJson.FileHash(Path.Combine(output, "sources", p.Key)) != p.Value))
                    throw new InvalidDataException("Fixture, sources, settings or execution changed during the investigation.");
            }
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            { Status = exception is OperationCanceledException ? "Cancelled" : "Invalid", ErrorType = exception.GetType().Name, exception.Message });
            throw;
        }
    }

    private static string Markdown(HandoffReport report)
    {
        static string N(double? n) => n?.ToString("0.00", CultureInfo.InvariantCulture) ?? "—";
        var text = new StringBuilder("# Blood Grove → Crystal Creek: level-10 starter\n\n");
        text.AppendLine($"{report.Status}; **advisory only**. {report.ValidBattles} valid battles; eight preselected replays matched. No gameplay target or baseline was accepted.\n");
        text.AppendLine("quest-rewards is the primary conditional checkpoint. Other builds isolate preparation steps. Intervals are pointwise, without simultaneous correction. Keep both seed sets separate. Adjacent build differences are paired within each encounter; different areas/species are descriptive comparisons, not compatible regression substitutions.\n");
        text.AppendLine("| Set | Area | Build | Encounter | W/L/D | Win % [95% interval] | Median win / non-win seconds | Previous step | Paired win change pp [95% interval] |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var row in report.Cells)
        {
            var c = row.Cell;
            var rate = c.ClearRate!;
            var delta = row.ClearRateChange is { } d ? $"{N(d.MeanChange)} [{N(d.Lower)}, {N(d.Upper)}]" : "—";
            text.AppendLine($"| {row.SeedSet} | {c.Stage} | {c.Build} | {c.Encounter} | {c.Wins}/{c.Losses}/{c.Draws} | {N(rate.Rate * 100)} [{N(rate.Lower * 100)}, {N(rate.Upper * 100)}] | {N(c.WinDurationSeconds.Median)} / {N(c.NonWinDurationSeconds.Median)} | {row.PreviousBuild ?? "—"} | {delta} |");
        }
        text.AppendLine("\nNon-win duration is not kill time. Original level-5 evidence is not rerun, pooled or reclassified here. Read the tracked handoff review for acquisition limits and the human playtest checklist. Retain plan.json, sources, suite.json, both runs, results.json and replays together.");
        return text.ToString();
    }
}
