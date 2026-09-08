using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Combat;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Items;
using Services.LL.PowerRatings;

namespace BalanceHarness;

public sealed record EntryBuildBudget(string BuildId, string SourceBuildId, string ArmorId, bool Fury,
    long CindersSpent, long CindersRemaining, int BlueprintsSpent, int BlueprintsRemaining);
public sealed record EntrySeedSet(string Id, int MasterSeed);
public sealed record EntryExperimentPlan(int SchemaVersion, IdleSuiteDefinition Suite,
    IReadOnlyList<EntryBuildBudget> Builds, IReadOnlyList<EntrySeedSet> SeedSets,
    IReadOnlyDictionary<string, string> SourceHashes, string FixtureHash, int PlannedBattles,
    string StoppingRule, string ReplaySelection);
public sealed record EntryCellFinding(string SeedSet, CellScorecard Cell,
    PairedEstimate? FuryClearRateChange, PairedEstimate ChangeFromPlainMediumMail);
public sealed record EntryExperimentReport(string Status, string Policy, int ValidBattles,
    IReadOnlyList<EntryCellFinding> Cells, IReadOnlyList<string> Replays,
    IReadOnlyDictionary<string, string> RunArtifactHashes);

/// <summary>A fixed, costed investigation using existing suite/replay/statistics contracts.</summary>
public static class BloodGroveEntryExperiment
{
    public const string SuiteId = "idle-blood-grove-entry-v1";
    public const string FuryStyle = "blueprint_fury";
    private static readonly string[] Sources =
    [
        "equipment/equipment-blueprints.v1.json", "equipment/equipment-ordinary.v1.json",
        "quests/onboarding/soul-archive.v3.json", "quests/region-01/into-lumo-ruins.v2.json"
    ];

    public static EntryExperimentPlan CreatePlan(string apiRoot, string sourceFixture, int samples)
    {
        if (samples is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(samples), "Use 1–100 samples per cell.");
        var source = HarnessJson.Read<IdleSuiteDefinition>(sourceFixture);
        var stage = source.Stages.Single(s => s.Id == "blood-grove");
        if (source.Id != "idle-first-hunt-v1" || source.SchemaVersion != 2 || stage.Builds.Count != 6
            || stage.Encounters.Count != 2 || stage.EssenceLevels is not null
            || stage.Builds.Any(b => b.CharacterLevel != 5 || b.Tier != 1 || b.Rank != 0
                || b.Equipment.Count != 2 || b.EssenceIds.Count != 1 || b.Quality != ItemQuality.Standard
                || b.AttributeRollMultiplier != 1))
            throw new InvalidDataException("Review the changed First Hunt entry contract before running this experiment.");
        var (threat, cadence) = RunBundle.ReadCombatSettings(apiRoot);
        var content = new OfflineContent(apiRoot, threat);
        var equipmentRoot = Path.Combine(apiRoot, "Data", "equipment");
        var acquisition = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(equipmentRoot, "equipment-ordinary.v1.json"));
        var blueprints = JsonEquipmentBlueprintCatalog.Load(Path.Combine(equipmentRoot, "equipment-blueprints.v1.json"), content.Equipment);
        var blueprint = blueprints.Find(FuryStyle) ?? throw new InvalidDataException("Fury blueprint is missing.");
        var rewards = HarnessJson.Read<JsonElement>(Path.Combine(apiRoot, "Data", "quests", "onboarding", "soul-archive.v3.json"))
            .GetProperty("rewards").EnumerateArray().ToArray();
        var cinders = rewards.Where(r => r.GetProperty("type").GetString() == "Cinders").Sum(r => r.GetProperty("quantity").GetInt64());
        var copies = rewards.Where(r => r.TryGetProperty("itemBaseId", out var id) && id.GetString() == blueprint.ItemId)
            .Sum(r => r.GetProperty("quantity").GetInt32());
        var armorGrant = HarnessJson.Read<JsonElement>(Path.Combine(apiRoot, "Data", "quests", "region-01", "into-lumo-ruins.v2.json"))
            .GetProperty("rewards").EnumerateArray().Single(r => r.GetProperty("itemBaseId").GetString() == RandomEquipmentBoxCatalog.ArmorChestItemBaseId);
        var reward = RandomEquipmentBoxCatalog.ArmorChest.RandomEquipment!;
        if (copies != 1 || cinders < blueprints.CindersPerTier || armorGrant.GetProperty("quantity").GetInt32() != 1
            || reward.Quantity != 1 || reward.Tier != 1 || reward.Rank != 0 || reward.Rarity != EquipmentRarity.Common)
            throw new InvalidDataException("The guaranteed entry reward budget changed; review the experiment.");
        // Exactly the production SelectionCrateService candidate filter. These are alternatives,
        // not nine items awarded to one character. Award defaults fix Standard quality / 1.0 rolls.
        var armors = acquisition.BaseDropDefinitions(reward.Rarity).Where(d => reward.EquipmentTypes!.Contains(
            content.Equipment.Evaluator.GetArchetype(d.ArchetypeId).EquipmentType)).ToArray();
        if (armors.Length != 9 || !armors.Any(a => a.Id == "plain.medium_mail")
            || !armors.Any(a => a.Id == "plain.heavy_breastplate"))
            throw new InvalidDataException("Armor Chest coverage changed; review the fixed matrix and replay selection.");
        var builds = new List<EquipmentReferenceBuildDefinition>();
        var budgets = new List<EntryBuildBudget>();
        foreach (var original in stage.Builds)
        foreach (var armor in armors)
        foreach (var fury in new[] { false, true })
        {
            var weapon = original.Equipment.Single(e => e.Slot == EquipmentSlotType.MainHand);
            var armorType = content.Equipment.Evaluator.GetArchetype(armor.ArchetypeId).EquipmentType;
            var slot = armorType switch
            {
                EquipmentType.Head => EquipmentSlotType.Head, EquipmentType.Chest => EquipmentSlotType.Chest,
                EquipmentType.Legs => EquipmentSlotType.Legs, _ => throw new InvalidDataException("Invalid Armor Chest slot.")
            };
            var id = $"{original.Id}-{armor.Id.Replace("plain.", "").Replace('_', '-')}-{(fury ? "fury" : "plain")}";
            builds.Add(original with { Id = id, Equipment =
                [weapon with { ActiveStyleId = fury ? FuryStyle : null, UseNativeStyle = false }, new(slot, armor.Id, UseNativeStyle: false)] });
            var cost = fury ? blueprints.CindersPerTier : 0;
            budgets.Add(new(id, original.Id, armor.Id, fury, cost, cinders - cost, fury ? 1 : 0, copies - (fury ? 1 : 0)));
        }
        var suite = source with
        {
            Id = SuiteId, SamplesPerCell = samples,
            Description = "Blood Grove entry: six First Hunt builds × nine alternative Armor Chest items × plain/Fury weapon. Fixed conditional outcomes, not a population estimate.",
            Stages = [stage with { Builds = builds, Assumptions =
            [
                "Character level 5; retain the First Hunt Essence at level 1, unascended and unevolved, and the chosen mace/wand.",
                "Exactly one common, Standard, tier-1 rank-0 Armor Chest item; every possible base item is a separate conditional build. Rolls use the production award default 1.0.",
                "Plain keeps the one Fury Blueprint and all starter Cinders. Fury consumes one guaranteed blueprint and its tier-1 Cinder cost on the weapon; see plan.json for the ledger.",
                "No reinforcement, extra equipment, regional drops, buffs, persistent bonuses or alternate Essence selection. The Lumo Token is retained.",
                "Fixed Raven pair and Raven/Blood Zombie pair; all build variants share encounter seed schedules. No gameplay goals are approved."
            ] }]
        };
        var schedule = IdleSuite.Resolve(suite, content, threat, cadence, 1337);
        var hashes = Sources.ToDictionary(p => p, p => HarnessJson.FileHash(Path.Combine(apiRoot, "Data", p)), StringComparer.Ordinal);
        hashes.Add("source-fixture.json", HarnessJson.FileHash(sourceFixture));
        return new(1, suite, budgets, [new("discovery", 1337), new("confirmation", 940031)], hashes,
            BalanceGoals.FixtureContractHash(suite), schedule.Cells.Sum(c => c.Trials.Count) * 2,
            "Run all 216 cells on both predeclared seed sets. Do not select variants or extend samples after seeing results.",
            "Confirmation trial index 0 for every starter/weapon/encounter with Heavy Breastplate, both plain and Fury: 24 detailed replays. No outcome-based selection.");
    }

    public static async Task<EntryExperimentReport> RunAsync(string apiRoot, string sourceFixture, string outputDirectory,
        int samples, CancellationToken cancellationToken, Action<string>? progress = null)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Output already exists: {output}");
        cancellationToken.ThrowIfCancellationRequested();
        var plan = CreatePlan(apiRoot, sourceFixture, samples);
        Directory.CreateDirectory(output);
        try
        {
            HarnessJson.WriteNew(Path.Combine(output, "plan.json"), plan);
            var recipe = Path.Combine(output, "suite.json");
            HarnessJson.WriteNew(recipe, plan.Suite);
            File.Copy(sourceFixture, Path.Combine(output, "source-fixture.json"));
            foreach (var path in Sources)
            {
                var target = Path.Combine(output, "sources", path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(apiRoot, "Data", path), target);
                if (HarnessJson.FileHash(target) != plan.SourceHashes[path]) throw new InvalidDataException("Source changed during plan capture.");
            }
            var findings = new List<EntryCellFinding>();
            var replays = new List<string>();
            var artifacts = new Dictionary<string, string>();
            SavedSuite? discovery = null;
            foreach (var set in plan.SeedSets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var run = Path.Combine(output, set.Id);
                var report = await SuiteBundle.CreateAsync(apiRoot, recipe, run, set.MasterSeed, null, cancellationToken,
                    (done, total) => { if (done == 0 || done == total || done % (samples * 24) == 0) progress?.Invoke($"{set.Id}: {done}/{total}"); });
                if (report.Status != "Complete")
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new InvalidDataException("Incomplete entry experiment; no subset is reported as complete.");
                }
                var saved = SavedSuite.Read(run, cancellationToken);
                if (discovery is not null)
                {
                    if (HarnessJson.Hash(discovery.Manifest.ContentHashes) != HarnessJson.Hash(saved.Manifest.ContentHashes)
                        || HarnessJson.Hash(discovery.Manifest.Execution) != HarnessJson.Hash(saved.Manifest.Execution))
                        throw new InvalidDataException("Combat content/execution changed between seed sets.");
                    foreach (var cell in saved.Input.Cells)
                        if (cell.Trials.Select(t => t.Seed).Intersect(discovery.Input.Cells.Single(c => c.Id == cell.Id).Trials.Select(t => t.Seed)).Any())
                            throw new InvalidDataException("Discovery and confirmation schedules overlap.");
                }
                else discovery = saved;
                artifacts.Add(set.Id, saved.ArtifactHash);
                var stage = plan.Suite.Stages.Single();
                foreach (var budget in plan.Builds)
                foreach (var encounter in stage.Encounters)
                {
                    var cellId = $"{stage.Id}.{budget.BuildId}.{encounter.Id}";
                    var plain = plan.Builds.Single(b => b.SourceBuildId == budget.SourceBuildId && b.ArmorId == budget.ArmorId && !b.Fury);
                    var anchor = plan.Builds.Single(b => b.SourceBuildId == budget.SourceBuildId && b.ArmorId == "plain.medium_mail" && !b.Fury);
                    findings.Add(new(set.Id, saved.Scorecard.Cells.Single(c => c.CellId == cellId),
                        budget.Fury ? PairedClearRate(saved, $"{stage.Id}.{plain.BuildId}.{encounter.Id}", cellId) : null,
                        PairedClearRate(saved, $"{stage.Id}.{anchor.BuildId}.{encounter.Id}", cellId)));
                    if (set.Id == "confirmation" && budget.ArmorId == "plain.heavy_breastplate")
                    {
                        var battleId = saved.Input.Cells.Single(c => c.Id == cellId).Trials[0].BattleId;
                        var replay = await SuiteBundle.ReplayAsync(run, battleId, true, cancellationToken);
                        var relative = "replays/" + battleId + ".json";
                        Directory.CreateDirectory(Path.Combine(output, "replays"));
                        HarnessJson.WriteNew(Path.Combine(output, relative), replay);
                        replays.Add(relative);
                    }
                }
            }
            var result = new EntryExperimentReport("Complete", "Advisory", findings.Sum(f => f.Cell.Valid), findings, replays, artifacts);
            HarnessJson.WriteNew(Path.Combine(output, "results.json"), result);
            await File.WriteAllTextAsync(Path.Combine(output, "summary.md"), Markdown(plan, result), cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            { Status = exception is OperationCanceledException ? "Cancelled" : "Invalid", ErrorType = exception.GetType().Name, exception.Message });
            throw;
        }
    }

    public static PairedEstimate PairedClearRate(SavedSuite saved, string beforeId, string afterId)
    {
        if (saved.Scorecard.Status != "Complete") throw new InvalidDataException("Pairing requires a complete verified suite.");
        var before = saved.Input.Cells.Single(c => c.Id == beforeId);
        var after = saved.Input.Cells.Single(c => c.Id == afterId);
        var left = before.Trials.OrderBy(t => t.Index).ToArray();
        var right = after.Trials.OrderBy(t => t.Index).ToArray();
        if (left.Length != right.Length || left.Zip(right).Any(p => p.First.Index != p.Second.Index || p.First.Seed != p.Second.Seed))
            throw new InvalidDataException("Controlled variants must share every trial index and seed.");
        var pairs = left.Zip(right).Select(p => (Before: saved.Observations[p.First.BattleId], After: saved.Observations[p.Second.BattleId])).ToArray();
        return PairedStatistics.ClearRate(
            pairs.Count(p => p.Before.Outcome != BattleOutcome.Victory && p.After.Outcome == BattleOutcome.Victory),
            pairs.Count(p => p.Before.Outcome == BattleOutcome.Victory && p.After.Outcome != BattleOutcome.Victory), pairs.Length);
    }

    private static string Markdown(EntryExperimentPlan plan, EntryExperimentReport result)
    {
        var text = new StringBuilder("# Blood Grove attainable-entry investigation\n\n");
        text.AppendLine($"{result.Status}; advisory only. {result.ValidBattles}/{plan.PlannedBattles} valid battles; {result.Replays.Count} preselected detailed replays.\n");
        text.AppendLine("One weapon and one alternative Armor Chest item per build. The plan records the guaranteed Cinders/Blueprint ledger. All ranks and Essence training remain at their entry defaults.\n");
        text.AppendLine("Each cell has a pointwise 95% Wilson clear-rate interval. Builds share seeds and must not be pooled as independent trials. Fury movement uses paired estimates against the same armor without Fury; JSON also compares against plain Medium Mail. Intervals do not provide simultaneous coverage across the matrix. These controlled substitutions do not relax regression-comparison compatibility or approve goals.\n");
        text.AppendLine("| Seed set | Build | Encounter | Wins / valid | Clear % [95% interval] | Fury change pp [95% interval] | Mean defeat/draw seconds |\n| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var row in result.Cells)
        {
            var c = row.Cell;
            var r = c.ClearRate!;
            var delta = row.FuryClearRateChange is { } d ? $"{Number(d.MeanChange)} [{Number(d.Lower)}, {Number(d.Upper)}]" : "—";
            text.AppendLine($"| {row.SeedSet} | {c.Build} | {c.Encounter} | {c.Wins}/{c.Valid} | {Number(r.Rate * 100)} [{Number(r.Lower * 100)}, {Number(r.Upper * 100)}] | {delta} | {Number(c.NonWinDurationSeconds.Mean)} |");
        }
        text.AppendLine("\nDefeat/draw duration is not kill time. Review per-run scorecards for winning pace and health. Retain plan.json, suite.json, sources, run bundles, results.json and replays together. No baseline promotion or goal evaluation occurs here.");
        return text.ToString();
    }

    private static string Number(double? number) => number?.ToString("0.0", CultureInfo.InvariantCulture) ?? "unavailable";
}
