using System.IO.Compression;
using System.Text.Json;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerActivityPlan(string Version, IReadOnlyList<int> Checkpoints, int Paths, int MaximumDungeonAttempts,
    IReadOnlyList<string> IdleOutcomes, string Selection, string Assumptions);
public sealed record TowerActivityIdentity(string Recipe, int Path, int Seed);
public sealed record TowerActivityRequest(string Version, string ApiRoot, string Fixtures, string Output,
    IReadOnlyDictionary<string, string> InputHashes, IReadOnlyList<TowerActivityIdentity> Identities, IReadOnlyList<TowerBootstrapPanel> Panels,
    string? SourceArchive = null);
public sealed record TowerActivityAttempt(int Ordinal, int Encounter, string File, FixtureCharacter Before,
    DungeonRunStatus Status, EquipmentData? Award, int SigilsBefore, int SigilsAfter, double CombatSeconds,
    string Dungeon = "goblin_mines", int SourceOrdinal = 0, TowerDungeonLootResult? DungeonLoot = null);
public sealed record TowerActivityCheckpoint(int Encounter, int Victories, int OrdinaryItems, int SupplyItems,
    IReadOnlyDictionary<string, int> Sigils, FixtureCharacter Character);
public sealed record TowerSourcePolicyPlan(string Version, IReadOnlyList<string> Policies, string PanelIndex,
    string ActivityVersion, string Assumptions);

public static class TowerActivityStudy
{
    public const int MaximumFights = 30000;
    public const string SourcePolicyVersion = "tower-two-source-v1";
    public static TowerSourcePolicyPlan ReadSourcePolicy(string fixtures)
    {
        var p = TowerContractJson.Read<TowerSourcePolicyPlan>(Path.Combine(fixtures, "tower-source-policy.json"));
        if (p.Version != SourcePolicyVersion || p.ActivityVersion != TowerActivityInventory.Version
            || !p.Policies.SequenceEqual(new[] { "mines-only", "mines-first-either" })
            || p.PanelIndex != "family-attempt-ordinal" || string.IsNullOrWhiteSpace(p.Assumptions))
            throw new InvalidDataException("Changed source policy requires a new declaration.");
        return p;
    }
    public static string? ChooseSource(string policy, IReadOnlyDictionary<string, int> stock)
    {
        if (policy is not ("mines-only" or "mines-first-either")) throw new InvalidDataException("Unknown source policy.");
        if (stock.GetValueOrDefault("sigil_goblin_mines") > 0) return "goblin_mines";
        return policy == "mines-first-either" && stock.GetValueOrDefault("sigil_forgotten_catacombs") > 0
            ? "forgotten_catacombs" : null;
    }
    public static TowerActivityPlan Read(string fixtures)
    {
        var p = TowerContractJson.Read<TowerActivityPlan>(Path.Combine(fixtures, "tower-activity.json"));
        if (p.Version != TowerActivityInventory.Version || p.Paths != 4 || p.MaximumDungeonAttempts != 12
            || !p.Checkpoints.SequenceEqual(new[] { 2160, 8640, 25920, 86400 })
            || !p.IdleOutcomes.SequenceEqual(new[] { "perfect", "four-of-five" })
            || p.Selection != "highest-materialized-stat-budget-v1" || string.IsNullOrWhiteSpace(p.Assumptions))
            throw new InvalidDataException("Changed activity plan requires a new declaration.");
        return p;
    }
    public static Task RunAsync(TowerActivityRequest request, CancellationToken token) => RunCoreAsync(request, token, null);
    public static Task RunSourcePolicyAsync(TowerActivityRequest request, CancellationToken token) =>
        RunCoreAsync(request, token, ReadSourcePolicy(request.Fixtures));
    public static Task RunEntryReadinessAsync(TowerActivityRequest request, CancellationToken token) =>
        RunCoreAsync(request, token, TowerEntryReadiness.Read(request.Fixtures));
    public static Task RunDungeonLootAsync(TowerActivityRequest request, CancellationToken token) =>
        RunCoreAsync(request, token, TowerDungeonLoot.Read(request.Fixtures));
    public static Task RunProphecyOffersAsync(TowerActivityRequest request, CancellationToken token) =>
        RunCoreAsync(request, token, TowerProphecyOffers.ReadCombat(request.Fixtures));
    public const string JourneyVersion = "tower-growing-activity-v1";
    public static Task RunGrowingActivityAsync(TowerActivityRequest request, CancellationToken token)
    {
        var comparison = TowerContractJson.Read<TowerSourcePolicyPlan>(Path.Combine(request.Fixtures,"tower-growing-activity.json"));
        if (comparison.Version != JourneyVersion || comparison.ActivityVersion != TowerActivityInventory.Version
            || !comparison.Policies.SequenceEqual(new[] {"fixed-progression","earned-progression"})
            || comparison.PanelIndex != "family-attempt-ordinal" || string.IsNullOrWhiteSpace(comparison.Assumptions))
            throw new InvalidDataException("Changed growing activity declaration.");
        TowerGrowthStudy.Read(request.Fixtures);
        return RunCoreAsync(request,token,comparison);
    }
    private static async Task RunCoreAsync(TowerActivityRequest request, CancellationToken token, TowerSourcePolicyPlan? comparison)
    {
        var plan = Read(request.Fixtures);
        var growingStudy = comparison?.Version == JourneyVersion;
        var prophecyStudy = comparison?.Version == TowerProphecyOffers.Version;
        var lootStudy = growingStudy || prophecyStudy || comparison?.Version == TowerDungeonLoot.Version;
        var readiness = lootStudy || comparison?.Version == TowerEntryReadiness.Version;
        var families = comparison is null ? new[] { "goblin_mines" } : new[] { "goblin_mines", "forgotten_catacombs" };
        var policies = comparison?.Policies ?? new[] { "mines-only" };
        var fightLimit = comparison is null ? MaximumFights : 64000;
        if (request.Version != (comparison?.Version ?? plan.Version) || Path.Exists(request.Output) || request.Identities.Count != 16 || request.Panels.Count != 48 * families.Length
            || request.Identities.Select(i => (i.Recipe, i.Path)).Distinct().Count() != 16
            || request.Panels.Any(p => !families.Contains(p.Run.Dungeon) || p.Run.RoomSeeds.Count != 64 || p.Path is < 0 or > 3 || p.Attempt is < 0 or > 11)
            || request.Panels.Select(p => (p.Run.Dungeon, p.Path, p.Attempt)).Distinct().Count() != 48 * families.Length)
            throw new InvalidDataException("Fresh output and complete declared matrix required.");
        // Comparison deliberately reuses the pinned owners/armor seeds; only dungeon panels are fresh.
        var reservations = (comparison is null ? request.Identities.Select(i => i.Seed) : [])
            .Concat(request.Panels.SelectMany(p => p.Run.RoomSeeds.Prepend(p.Run.LayoutSeed))).ToArray();
        var reservationCount = comparison is null ? 3136 : 6240;
        if (reservations.Length != reservationCount || reservations.Distinct().Count() != reservationCount) throw new InvalidDataException("Invalid reservations.");
        void Verify() { foreach (var pair in request.InputHashes) if (HarnessJson.FileHash(pair.Key) != pair.Value) throw new InvalidDataException("Frozen input changed: " + pair.Key); }
        Verify();
        var content = OfflineContent.ForTower(request.ApiRoot, TowerBundle.ReadSettings(request.ApiRoot));
        var inventory = new TowerActivityInventory(request.ApiRoot, content);
        var configuration = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Path.Combine(request.ApiRoot, "appsettings.json"))).Build();
        var cadence = configuration.GetValue<int>("Combat:IdleProgression:EncounterCadenceSeconds");
        if (cadence <= 0) throw new InvalidDataException("Missing idle cadence.");
        var switches = configuration.GetSection("EquipmentProgression").Get<Application.Interfaces.Services.LL.Items.EquipmentProgressionOptions>() ?? new();
        if (!switches.OrdinaryAcquisitionEnabled || !switches.ProtectedAcquisitionEnabled || !switches.TowerSupplyAcquisitionEnabled)
            throw new InvalidDataException("Required acquisition is disabled.");
        foreach (var (file, area) in new[] { ("restless-dead.v4.json", "region_01_area_04"), ("between-day-and-night.v4.json", "region_01_area_06") })
        {
            var quest = HarnessJson.Read<JsonElement>(Path.Combine(request.ApiRoot, "Data/quests/region-01", file));
            var objective = quest.GetProperty("objectives").EnumerateArray().Single(o => o.GetProperty("type").GetString() == "CombatEncounterCompleted");
            if (objective.GetProperty("requiredAmount").GetInt32() != 5 || objective.GetProperty("filters").GetProperty("areaId").GetString() != area
                || !objective.GetProperty("filters").GetProperty("requiresVictory").GetBoolean()) throw new InvalidDataException("Changed pre-dungeon quest activity.");
        }
        var bootstrap = TowerBootstrapCohorts.Read(Path.Combine(request.Fixtures, "tower-bootstrap.json"));
        var recipes = TowerBootstrapCohorts.Create(request.ApiRoot, request.Fixtures, bootstrap, content)
            .Where(c => c.Gear == "common" && c.EssenceLevel == 1).ToDictionary(c => c.Recipe);
        var supplies = JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(request.ApiRoot, "Data/equipment/tower-equipment-supplies.v1.json"), content.Equipment);
        var supply = supplies.Candidates(1, 30).Single(s => s.RequiredClearedFloor == 0);
        var runner = new DungeonAcquisitionRunner(request.ApiRoot, content);
        var fights = 0; var replays = 0; var controls = 0; var attempts = 0; var replayedRewardWindows = 0; long bytes = 0;
        Directory.CreateDirectory(request.Output);
        void Save(string name, object value, bool compress = false)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options);
            if (compress) { using var buffer = new MemoryStream(); using (var zip = new GZipStream(buffer, CompressionLevel.Fastest, true)) zip.Write(data); data = buffer.ToArray(); }
            if ((bytes += data.Length) > 256 * 1048576L) throw new InvalidDataException("Output limit exceeded.");
            using var file = new FileStream(Path.Combine(request.Output, name), FileMode.CreateNew); file.Write(data);
        }
        void Count() { token.ThrowIfCancellationRequested(); if (++fights > fightLimit) throw new InvalidDataException("Fight bound exceeded."); }
        var scores = content.Equipment.Evaluator.Definitions.SelectMany(d => content.Equipment.Evaluator.Evaluate(d.Id, 1, 0, null).Stats.Keys)
            .Distinct().ToDictionary(a => a, a => content.Equipment.Evaluator.Balance.GetMaterializedCostPerPoint(a, 1));
        Save("rules.json", new { plan, comparison, cadence, scores, ordinary = inventory.Catalog.FindRegion(1),
            armorPool = inventory.Catalog.BaseDropDefinitions(EquipmentRarity.Common).Where(d =>
                new[] { Domain.Models.Items.Equipments.EquipmentType.Head, Domain.Models.Items.Equipments.EquipmentType.Chest, Domain.Models.Items.Equipments.EquipmentType.Legs }
                .Contains(content.Equipment.Evaluator.GetArchetype(d.ArchetypeId).EquipmentType)).Select(d => d.Id).ToArray() });
        var summaries = new List<object>();
        var controlOutcomes = new Dictionary<(string Key, string Source), bool>();
        foreach (var identity in request.Identities)
        foreach (var outcome in plan.IdleOutcomes)
        foreach (var policy in policies)
        {
            token.ThrowIfCancellationRequested();
            var key = $"{identity.Recipe}--{identity.Path}--{outcome}";
            var outputKey = comparison is null ? key : $"{key}--{policy}";
            var owner = StableRandom.Guid(plan.Version, identity.Recipe, identity.Path.ToString(), identity.Seed.ToString());
            var reference = recipes[identity.Recipe];
            var starting = new[] { inventory.QuestMace(owner), inventory.QuestArmor(owner, identity.Seed) };
            var owned = starting.ToList();
            var dungeonLoot = lootStudy ? new TowerDungeonLoot(request.ApiRoot, content) : null;
            var dungeonItems = 0;
            var prophecySchedule = prophecyStudy ? await TowerProphecyOffers.Schedule(request.ApiRoot,owner,outcome,plan.Checkpoints.Last(),TowerProphecyOffers.Read(request.Fixtures),token) : null;
            var prophecyCheckpoints = new List<TowerEntrySourceCheckpoint>();
            var journey = growingStudy ? new TowerJourneyProgression(request.ApiRoot,content,reference.Character with { Id = owner },policy == "earned-progression") : null;
            var journeyEntries = new List<object>(); var journeyCheckpoints = new List<object>();
            var targets = bootstrap.PurchaseOrder.Select(slot => reference.Target.Single(t => t.Slot == slot)).Select((t, i) =>
                supplies.Award(supply.ItemBaseId, t.Data.State.DefinitionId, owner, StableRandom.Guid(plan.Version, key, "control", i.ToString()), "already-owned-control")).ToArray();
            var sigils = new Dictionary<string, int> { ["sigil_goblin_mines"] = 0, ["sigil_forgotten_catacombs"] = 1 };
            var windows = new List<TowerActivityRewards>(); var steps = new List<TowerActivityAttempt>();
            var decisions = new List<TowerEntryDecision>();
            var checkpoints = new List<TowerActivityCheckpoint>();
            int encounter = 0, victories = 0, firstDrop = 0, moonlitAfterDrop = 0, twilightVictories = 0, questAt = 0, earned = 0;
            Guid? gateEquippedItem = null;
            FixtureCharacter Character()
            {
                var character = reference.Character with { Id = owner, Name = key, Equipment = inventory.Select(owned),
                    Essences = questAt > 0 ? reference.Character.Essences : reference.Character.Essences.Take(3).ToArray() };
                return journey?.Enabled == true ? journey.Growth.Snapshot(character) : character;
            }
            var control = reference.Character with { Id = owner, Name = key, Equipment = inventory.Select(targets) };
            if (policy == policies[0])
            foreach (var family in families)
            {
                var controlPanel = request.Panels.Single(p => p.Run.Dungeon == family && p.Path == identity.Path && p.Attempt == 0).Run;
                var controlRun = await runner.RunAsync(control, controlPanel, Count, token);
                var controlKey = comparison is null ? key : $"{key}--{family}";
                Save(controlKey + "--control.json.gz", controlRun, true); controls++;
                controlOutcomes.Add((key, family), controlRun.Status == DungeonRunStatus.Completed);
                if (identity.Path == 0)
                {
                    var replay = await runner.RunAsync(control, controlPanel, Count, token);
                    Save(controlKey + "--control-replay.json.gz", replay, true);
                    if (HarnessJson.Hash(replay) != HarnessJson.Hash(controlRun)) throw new InvalidDataException("Control replay mismatch.");
                    replays++;
                }
            }
            foreach (var checkpoint in plan.Checkpoints)
            {
                while (encounter < checkpoint)
                {
                    var area = firstDrop > 0 && moonlitAfterDrop >= 5 ? "region_01_area_06" : "region_01_area_04";
                    var until = questAt == 0 ? encounter + 1 : checkpoint;
                    var rewards = await inventory.Rewards(owner, outcome, area, encounter, until, cadence, token);
                    if (journey is not null)
                        for (var ordinal = encounter + 1; ordinal <= until; ordinal++)
                            await journey.Idle(area,TowerActivityInventory.Victory(outcome,ordinal),cadence,token);
                    // A separate production replay split at a different batch boundary guards joint reward retention.
                    if (until - encounter > 1)
                    {
                        var middle = (encounter + until) / 2;
                        var left = await inventory.Rewards(owner, outcome, area, encounter, middle, cadence, token);
                        var right = await inventory.Rewards(owner, outcome, area, middle, until, cadence, token);
                        var combined = left.Sigils.Concat(right.Sigils).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));
                        if (HarnessJson.Hash(rewards.Equipment) != HarnessJson.Hash(left.Equipment.Concat(right.Equipment).ToArray())
                            || rewards.Sigils.Any(p => combined.GetValueOrDefault(p.Key) != p.Value) || combined.Count != rewards.Sigils.Count)
                            throw new InvalidDataException("Reward batching changed inventory.");
                        replayedRewardWindows++;
                    }
                    windows.Add(rewards); owned.AddRange(rewards.Equipment); victories += rewards.Victories;
                    foreach (var pair in rewards.Sigils) sigils[pair.Key] += pair.Value;
                    if (firstDrop == 0 && rewards.Equipment.Count > 0)
                    {
                        firstDrop = until; gateEquippedItem = rewards.Equipment[0].State.Id;
                        // All tier-1 types are legal with the quest mace; a two-handed gate item temporarily replaces it.
                    }
                    else if (firstDrop > 0 && moonlitAfterDrop < 5) moonlitAfterDrop += rewards.Victories;
                    else if (questAt == 0 && moonlitAfterDrop >= 5)
                    {
                        twilightVictories += rewards.Victories;
                        if (twilightVictories == 5) { questAt = until; sigils["sigil_goblin_mines"]++; journey?.Growth.Attune(4); }
                    }
                    encounter = until;
                }
                if (prophecySchedule is not null)
                {
                    var source = prophecySchedule.Checkpoints.Single(c => c.Encounter == encounter);
                    prophecyCheckpoints.Add(source);
                    if (policy == TowerProphecyOffers.Policy) sigils["sigil_goblin_mines"] += source.AssembledNow;
                }
                if (journey is not null)
                {
                    var assembled = await journey.Assemble(token);
                    sigils["sigil_goblin_mines"] += assembled;
                    journeyCheckpoints.Add(new { encounter, assembled, state = journey.State() });
                }
                while (true)
                {
                    var before = Character();
                    string? family;
                    if (readiness)
                    {
                        var decision = TowerEntryReadiness.Decide(lootStudy ? "full-slot-ready" : policy, before.Equipment, sigils, encounter, questAt > 0, steps.Count, earned);
                        decisions.Add(decision);
                        if (decision.Reason != "Enter") break;
                        family = decision.Dungeon!;
                    }
                    else
                    {
                        family = ChooseSource(policy, sigils);
                        if (questAt == 0 || family is null || steps.Count >= plan.MaximumDungeonAttempts || earned >= 7) break;
                    }
                    var sigil = "sigil_" + family;
                    var stock = sigils[sigil];
                    sigils[sigil]--;
                    var sourceOrdinal = steps.Count(s => s.Dungeon == family);
                    var panel = request.Panels.Single(p => p.Run.Dungeon == family && p.Path == identity.Path && p.Attempt == sourceOrdinal).Run;
                    var masteryAtEntry = journey?.Enabled == true ? (await journey.Mastery.GetMasteryByDungeonAsync(owner,[family],token))[family].Level : 0;
                    var journeyBefore = journey?.State();
                    var dungeonProgression = journey is null ? null : new TowerJourneyDungeon(request.ApiRoot,journey);
                    var run = await runner.RunAsync(before, panel, Count, token,dungeonProgression,masteryAtEntry);
                    if (journey is not null) journeyEntries.Add(new { ordinal = steps.Count, before = journeyBefore, receipt = dungeonProgression!.Receipt, after = journey.State() });
                    var file = $"{outputKey}--attempt-{steps.Count:00}.json.gz"; Save(file, run, true);
                    if (sourceOrdinal == 0)
                    {
                        var replay = await runner.RunAsync(before, panel, Count, token,masteryLevel:masteryAtEntry);
                        Save((comparison is null ? key : $"{outputKey}--{family}") + "--replay.json.gz", replay, true);
                        if (HarnessJson.Hash(run) != HarnessJson.Hash(replay)) throw new InvalidDataException("Acquisition replay mismatch.");
                        replays++;
                    }
                    EquipmentData? award = null;
                    TowerDungeonLootResult? loot = null;
                    if (dungeonLoot is not null)
                    {
                        var replayState = dungeonLoot.Fork();
                        loot = await dungeonLoot.ApplyAsync(owner, run, token,masteryAtEntry,growingStudy);
                        var replayLoot = await replayState.ApplyAsync(owner, run, token,masteryAtEntry,growingStudy);
                        if (HarnessJson.Hash(loot) != HarnessJson.Hash(replayLoot)) throw new InvalidDataException("Dungeon loot replay mismatch.");
                        if (growingStudy || prophecyStudy || policy == "include-dungeon-loot") { owned.AddRange(loot.Equipment); dungeonItems += loot.Equipment.Count; }
                    }
                    if (run.Status == DungeonRunStatus.Completed)
                    {
                        award = supplies.Award(supply.ItemBaseId, targets[earned].State.DefinitionId, owner,
                            StableRandom.Guid(plan.Version, key, "earned", earned.ToString()), $"{key}/{earned + 1}");
                        owned.Add(award); earned++;
                    }
                    steps.Add(new(steps.Count, encounter, file, before, run.Status, award, stock, sigils[sigil], run.CombatSeconds, family, sourceOrdinal, loot)); attempts++;
                }
                checkpoints.Add(new(encounter, victories, owned.Count - 2 - earned, earned, new Dictionary<string, int>(sigils), Character()));
                if (earned == 7 || steps.Count == plan.MaximumDungeonAttempts) break;
            }
            var final = Character();
            var setup = content.CreateSetup(final.Materialize(content.Equipment), final.MaterializeEssences());
            var preparation = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content, setup), setup);
            var prepared = await preparation.PrepareAsync(CombatContentType.Dungeon,
                [new(new("player", owner, CombatSide.Friendly, 1), new SnapshotCombatantPreparationSource(TowerBattleRunner.ToSnapshot(final, content)))], token);
            var finalPrepared = prepared.Select(p => new { p.Combatant.Level, p.Combatant.CombatAttributes,
                Equipment = p.Combatant.Equipment.Select(e => e.ProgressionData).ToArray(),
                Essences = p.Combatant.EquippedEssences.Select(e => new { e.EssenceDefinitionId, e.Level, e.AscensionTier }).ToArray() }).ToArray();
            var summary = new { key = outputKey, historyKey = key, policy, identity.Recipe, identity.Path, outcome, encounters = encounter, victories, idleCadenceSeconds = (long)encounter * cadence,
                attempts = steps.Count, successes = earned, completed = earned == 7, ordinaryItems = owned.Count - 2 - earned, dungeonItems,
                firstDrop, questAt, sigils, combatSeconds = steps.Sum(s => s.CombatSeconds), controlSucceeded = controlOutcomes[(key, "goblin_mines")],
                stop = earned == 7 ? "SevenSupplyItemsEarned" : steps.Count == 12 ? "AttemptCap" : "ActivityCap" };
            // Preserve older output contracts. Source-only horizons after an early completion are never credited.
            if (journey is not null)
                Save(outputKey + "--history.json", new { summary, starting, targets, gateEquippedItem, windows, steps, decisions, checkpoints, owned, final, finalPrepared,
                    progression = new { journey.Days, journey.Sources.Claims, journey.Sources.DuplicateClaimsRejected, journey.DungeonEvents,
                        checkpoints = journeyCheckpoints, entries = journeyEntries, final = journey.State() } });
            else if (prophecySchedule is null)
                Save(outputKey + "--history.json", new { summary, starting, targets, gateEquippedItem, windows, steps, decisions, checkpoints, owned, final, finalPrepared });
            else
            {
                var prophecy = encounter == plan.Checkpoints.Last() ? prophecySchedule : await TowerProphecyOffers.Schedule(
                    request.ApiRoot,owner,outcome,encounter,TowerProphecyOffers.Read(request.Fixtures),token);
                if (HarnessJson.Hash(prophecy.Checkpoints) != HarnessJson.Hash(prophecyCheckpoints))
                    throw new InvalidDataException("Stopping the offer schedule changed prior source checkpoints.");
                Save(outputKey + "--history.json", new { summary, starting, targets, gateEquippedItem, windows, steps, decisions, checkpoints, owned, final, finalPrepared, prophecy });
            }
            summaries.Add(summary);
        }
        Verify();
        Save("result.json", new { version = comparison?.Version ?? plan.Version,
            status = growingStudy ? "GrowingActivityComparisonComplete" : prophecyStudy ? "NativeOfferComparisonComplete" : lootStudy ? "ConditionalDungeonLootComparisonComplete" : readiness ? "ConditionalEntryReadinessComparisonComplete" : comparison is null ? "ConditionalJointActivityCompleteNotPlayerPace" : "ConditionalSourcePolicyComparisonComplete",
            fights, replays, controls, attempts, replayedRewardWindows, histories = summaries, measuredPlayerSamples = 0,
            assumptions = comparison?.Assumptions ?? plan.Assumptions });
        Save("files.json", Directory.GetFiles(request.Output).Order().ToDictionary(p => Path.GetFileName(p), HarnessJson.FileHash));
    }
}
