using System.Text.Json;
using Application.Interfaces.Services.LL.Items;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.Dungeons;
using Services.LL.Items;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Rewards;
using Services.LL.WorldTower;

namespace BalanceHarness;

public sealed record TowerAcquisitionPlan(string Version, string Assumptions,
    IReadOnlyList<TowerAcquisitionPace> Paces);
public sealed record TowerAcquisitionFloor(int Floor, int PartySize, int NewItems, int RetainedItems,
    int CumulativeEarnedItems, IReadOnlyList<TowerEquipmentSupply> AvailableSupplies,
    IReadOnlyList<TowerEarnedMember> Members, string PreparedParticipantsHash);
public sealed record TowerAcquisitionReference(string Archive, string ManifestPin, string CellId);
public sealed record TowerAcquisitionReferenceCheck(TowerAcquisitionReference Reference, int Items,
    int NewItemsForExactChoices, int ReusedItems, bool ExactCombatEquipmentDescriptors,
    string HistoricalRecipeHash, string CurrentPreparedParticipantsHash);

/// <summary>File-only acquisition ledger and production preparation; deliberately cannot execute fights.</summary>
public static class TowerAcquisitionStudy
{
    public const string Version = "tower-acquisition-study-v1";

    public static async Task<object> CreateAsync(string root, string fixtures, string planPath,
        IReadOnlyList<TowerAcquisitionReference> references, CancellationToken token = default)
    {
        var repo = Path.GetFullPath(Path.Combine(root, "../../../.."));
        var paths = new SortedSet<string>(StringComparer.Ordinal) { Path.GetFullPath(planPath),
            Path.Combine(fixtures, TowerProgressionPreview.CycleFixture), Path.Combine(fixtures, "tower-curve.json"),
            Path.Combine(root, "appsettings.json") };
        foreach (var f in TowerBundle.Files) paths.Add(Path.Combine(root, "Data", f));
        foreach (var directory in new[] { "equipment", "dungeons", "prophecies", "rewards", "quests", "market", "guilds" })
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Data", directory), "*.json", SearchOption.AllDirectories)) paths.Add(f);
        paths.Add(Path.Combine(root, "Data/items/items.json"));
        // Capture the actual implementation used, without relabeling old archive hashes.
        foreach (var directory in new[] { "LL/tools/BalanceHarness", "LL/src/Core/Domain/Models/Items/Equipments/Progression",
            "LL/src/Infrastructure/Service/Services.LL/Dungeons", "LL/src/Infrastructure/Service/Services.LL/Items",
            "LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle",
            "LL/src/Infrastructure/Service/Services.LL/JsonDefinitions/Dungeons" })
            foreach (var f in Directory.GetFiles(Path.Combine(repo, directory), "*.cs")) paths.Add(f);
        foreach (var file in new[] { "LL/src/Core/Application/Interfaces/Services/LL/Items/IStarterEquipmentService.cs",
            "LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs",
            "LL/src/Infrastructure/Service/Services.LL/JsonDefinitions/JsonDungeonDefinitions.cs" }) paths.Add(Path.Combine(repo, file));
        var normalizedPaths = paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
        var hashes = normalizedPaths.ToDictionary(p => Path.GetRelativePath(repo, p).Replace('\\', '/'), HarnessJson.FileHash);
        var plan = TowerContractJson.Read<TowerAcquisitionPlan>(planPath);
        if (plan.Version != Version || string.IsNullOrWhiteSpace(plan.Assumptions) || plan.Paces.Count is < 1 or > 12
            || plan.Paces.Select(p => p.Id).Distinct().Count() != plan.Paces.Count)
            throw new InvalidDataException("A named, bounded acquisition sensitivity plan is required.");
        var draft = TowerContractJson.Read<TowerProgressionDraft>(Path.Combine(fixtures, TowerProgressionPreview.CycleFixture));
        TowerProgressionPreview.Validate(draft);
        var settings = TowerBundle.ReadSettings(root);
        var content = OfflineContent.ForTower(root, settings);
        var supply = JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(root, "Data/equipment/tower-equipment-supplies.v1.json"), content.Equipment);
        var ordinary = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(root, "Data/equipment/equipment-ordinary.v1.json"));
        var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(root, "Data/equipment/equipment-upgrades.v1.json"));
        var assembly = HarnessJson.Read<DungeonSigilAssemblySettings>(Path.Combine(root, "Data/dungeons/sigil-assembly.json"));
        var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Path.Combine(root, "appsettings.json"))).Build();
        var switches = config.GetSection(EquipmentProgressionOptions.SectionName).Get<EquipmentProgressionOptions>() ?? new();
        if (!switches.ProtectedAcquisitionEnabled || !switches.TowerSupplyAcquisitionEnabled || !switches.OrdinaryAcquisitionEnabled)
            throw new InvalidDataException("This model requires enabled supply and ordinary sigil acquisition in the captured local configuration.");
        var cadence = config.GetValue<int>("Combat:IdleProgression:EncounterCadenceSeconds");
        var rewards = new JsonRewardTableDefinitionProvider(config, root, HarnessJson.Options, new RewardTableDefinitionValidator());
        var dungeons = new JsonDungeonDefinitions(new JsonDocumentReader<DungeonCatalogDocument>(root,
            "Data/dungeons/dungeons.json", HarnessJson.Options), new(new()), new DungeonDefinitionValidator(), rewards).GetAll();
        var floors = TowerContentProviders.Floors(Path.Combine(root, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options);
        var prophecy = HarnessJson.Read<JsonElement>(Path.Combine(root, "Data/prophecies/rewards.json"));
        var inventory = new TowerAcquisitionInventory(content, supply);
        var rows = new List<TowerAcquisitionFloor>();
        foreach (var budget in draft.Budgets)
        {
            token.ThrowIfCancellationRequested();
            var scenario = TowerProgressionEquipment.Apply(TowerPartyProgression.Scenarios(root, fixtures, budget)
                .Single(s => s.FloorNumber == budget.PriorityFloor), draft.EquipmentCycle!, content);
            var available = Available(supply, dungeons, budget.PriorityFloor - 1, budget.CharacterLevel,
                f => floors.GetFloor(f) is not null);
            var members = scenario.Party.OrderBy(p => p.PartySlot).Select(p => inventory.Equip(
                $"cohort-{p.PartySlot:00}", scenario.FloorNumber, p.Build, available)).ToArray();
            var prepared = await PrepareAsync(root, content, settings, scenario, members.Select(m => m.Character).ToArray(), token);
            rows.Add(new(scenario.FloorNumber, members.Length, members.Sum(m => m.NewItems),
                members.Sum(m => m.EquippedItemIds.Count - m.NewItems), inventory.Items.Count, available, members, prepared));
        }
        var newcomerScenario = TowerProgressionEquipment.Apply(TowerPartyProgression.Scenarios(root, fixtures, draft.Budgets[10])
            .Single(s => s.FloorNumber == 11), draft.EquipmentCycle!, content);
        var newcomerInventory = new TowerAcquisitionInventory(content, supply);
        var newcomers = newcomerScenario.Party.Select(p => newcomerInventory.Equip($"newcomer-{p.PartySlot:00}", 11,
            p.Build, rows[10].AvailableSupplies)).ToArray();
        var newcomersPrepared = await PrepareAsync(root, content, settings, newcomerScenario, newcomers.Select(m => m.Character).ToArray(), token);

        var referenceChecks = new List<TowerAcquisitionReferenceCheck>();
        var referencePath = new List<TowerAcquisitionFloor>();
        var referenceInventory = new TowerAcquisitionInventory(content, supply);
        var lastReferenceFloor = 0;
        foreach (var reference in references)
        {
            // Authenticate consumed bytes against an independently supplied manifest pin.
            var manifestPath = Path.Combine(reference.Archive, "files.json");
            if (!string.Equals(HarnessJson.FileHash(manifestPath), reference.ManifestPin, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Reference manifest pin mismatch.");
            var manifest = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
            var cellsPath = Path.Combine(reference.Archive, "cells.json");
            if (HarnessJson.FileHash(cellsPath) != manifest["cells.json"]) throw new InvalidDataException("Reference cells changed.");
            var cells = HarnessJson.Read<JsonElement>(cellsPath);
            var historical = cells.EnumerateArray().Single(c => ReferenceCellId(c) == reference.CellId)
                .GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!;
            var row = rows.Single(r => r.Floor == historical.FloorNumber);
            var branch = new TowerAcquisitionInventory(content, supply);
            // Rebuild the same individually earned history, then pay for every extra exact choice.
            foreach (var b in draft.Budgets.Where(b => b.PriorityFloor <= historical.FloorNumber))
            {
                var authored = TowerProgressionEquipment.Apply(TowerPartyProgression.Scenarios(root, fixtures, b)
                    .Single(s => s.FloorNumber == b.PriorityFloor), draft.EquipmentCycle!, content);
                foreach (var p in authored.Party) branch.Equip($"cohort-{p.PartySlot:00}", b.PriorityFloor, p.Build,
                    rows.Single(r => r.Floor == b.PriorityFloor).AvailableSupplies);
            }
            var exact = historical.Party.OrderBy(p => p.PartySlot).Select(p => branch.Equip($"cohort-{p.PartySlot:00}",
                historical.FloorNumber, p.Build, row.AvailableSupplies)).ToArray();
            foreach (var (member, recipe) in exact.Zip(historical.Party.OrderBy(p => p.PartySlot)))
            {
                var expected = FixtureCharacter.From(content.CreateBuild(recipe.Build));
                if (member.Character.Equipment.Count != expected.Equipment.Count || !member.Character.Equipment.Zip(expected.Equipment)
                    .All(pair => pair.First.Slot == pair.Second.Slot && HarnessJson.Hash(TowerAcquisitionInventory.CombatDescriptor(pair.First.Data))
                        == HarnessJson.Hash(TowerAcquisitionInventory.CombatDescriptor(pair.Second.Data))))
                    throw new InvalidDataException("Earned reference gear differs; a bounded combat study is necessary.");
            }
            _ = await PrepareAsync(root, content, settings, historical, exact.Select(e => e.Character).ToArray(), token);
            // Preserve every historical actor/item/Essence identity, position and ordered recipe in the comparison.
            var runner = new TowerBattleRunner(root, content);
            var prepared = await runner.PrepareAsync(runner.CreateInput(historical with { Seeds = [1] }, 1,
                settings.Threat, settings.CheckpointIntervalTicks), token);
            referenceChecks.Add(new(reference, exact.Sum(m => m.EquippedItemIds.Count), exact.Sum(m => m.NewItems),
                exact.Sum(m => m.EquippedItemIds.Count - m.NewItems), true, HarnessJson.Hash(historical),
                HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(prepared))));
            if (historical.FloorNumber <= lastReferenceFloor)
                throw new InvalidDataException("Reference path must list distinct increasing floors; each is a declared choice, not a search selection.");
            // A second explicit path chooses the known reference before buying its band.
            // It must retain its exact floor-10 items when moving to the floor-11 reference.
            foreach (var b in draft.Budgets.Where(b => b.PriorityFloor > lastReferenceFloor && b.PriorityFloor < historical.FloorNumber))
            {
                var authored = TowerProgressionEquipment.Apply(TowerPartyProgression.Scenarios(root, fixtures, b)
                    .Single(s => s.FloorNumber == b.PriorityFloor), draft.EquipmentCycle!, content);
                foreach (var p in authored.Party) referenceInventory.Equip($"cohort-{p.PartySlot:00}", b.PriorityFloor, p.Build,
                    rows.Single(r => r.Floor == b.PriorityFloor).AvailableSupplies);
            }
            var pathMembers = historical.Party.OrderBy(p => p.PartySlot).Select(p => referenceInventory.Equip($"cohort-{p.PartySlot:00}",
                historical.FloorNumber, p.Build, row.AvailableSupplies)).ToArray();
            var pathPrepared = await PrepareAsync(root, content, settings, historical, pathMembers.Select(m => m.Character).ToArray(), token);
            referencePath.Add(new(historical.FloorNumber, pathMembers.Length, pathMembers.Sum(m => m.NewItems),
                pathMembers.Sum(m => m.EquippedItemIds.Count - m.NewItems), referenceInventory.Items.Count,
                row.AvailableSupplies, pathMembers, pathPrepared));
            lastReferenceFloor = historical.FloorNumber;
        }
        var novice = dungeons.First(d => d.Region == 1 && d.Grade == DungeonGrade.GradeI);
        var pool = ordinary.FindRegion(1)!;
        var regionOne = dungeons.Where(d => d.Region == 1 && d.Grade == DungeonGrade.GradeI).ToArray();
        // Using every sigil is an explicit scenario, legal only if every pool sigil has an ungated grade-I source.
        var allUsable = pool.Sigils.All(s => regionOne.Any(d => d.SigilItemId == s.ItemBaseId
            && d.RequiredTowerFloor is null && d.RequiredPreviousDungeonId is null
            && d.EntryCosts.Count == 1 && d.EntryCosts[0].Amount == 1));
        if (!allUsable) throw new InvalidDataException("All-regional-sigil assumption is no longer supported by production entry rules.");
        var paceRows = plan.Paces.Select(p => {
            var fragments = p.DailyProphecyProfile is null ? 0 : prophecy.GetProperty("profiles").EnumerateArray()
                .Single(x => x.GetProperty("id").GetString() == p.DailyProphecyProfile && x.GetProperty("scope").GetString() == "Daily")
                .GetProperty("flatReward").GetProperty("sigilFragments").GetInt32();
            return new { assumptions = p, fragmentsPerClaim = fragments,
                requirements = new[] { 7, 8 }.Select(n => new {
                    allRegionalSigils = TowerAcquisitionEconomy.Calculate(n, p, novice, pool, assembly, cadence, fragments, true),
                    goblinOnly = TowerAcquisitionEconomy.Calculate(n, p, novice, pool, assembly, cadence, fragments, false) }).ToArray(),
                perOwnerLifetime = inventory.Items.GroupBy(i => i.OwnerKey).Select(g => new { owner = g.Key,
                    effort = TowerAcquisitionEconomy.Calculate(g.Count(), p, novice, pool, assembly, cadence, fragments, true) }).ToArray(),
                perOwnerPreselectedReferencePath = referenceInventory.Items.GroupBy(i => i.OwnerKey).Select(g => new { owner = g.Key,
                    effort = TowerAcquisitionEconomy.Calculate(g.Count(), p, novice, pool, assembly, cadence, fragments, true) }).ToArray() };
        }).ToArray();
        var equippedAtEnd = rows[10].Members.SelectMany(m => m.EquippedItemIds).ToHashSet();
        // Include equipment held by temporarily absent characters; do not label it disposable.
        var unequipped = inventory.Items.Where(i => !equippedAtEnd.Contains(i.Data.State.Id)).ToArray();
        foreach (var path in normalizedPaths)
            if (hashes[Path.GetRelativePath(repo, path).Replace('\\', '/')] != HarnessJson.FileHash(path))
                throw new InvalidDataException("Acquisition inputs changed during preparation.");
        return new { version = Version, status = "ConditionalAcquisitionPreparedNotPaceAcceptance", plan,
            execution = ExecutionIdentity.Current(), sourceHashes = hashes, fights = 0, reservedSeeds = 0,
            measuredPlayerSamples = 0, settings, declaredBudget = draft,
            rules = new { switches, cadenceSeconds = cadence, assembly, regionalSigilChancePerVictory = pool.SigilDropChance,
                regionalSigils = pool.Sigils, dungeonSources = dungeons.Select(d => new { d.Id, d.Region, d.Grade,
                    d.RequiredTowerFloor, d.RequiredPreviousDungeonId, d.EntryCosts, d.MinRooms, d.MaxRooms,
                    d.RestSiteCount, d.TreasuryCount, d.VigorFeasibilityMasteryLevel,
                    firstClear = DungeonRewardCatalog.GetFirstCompletionGrants(d),
                    monsterCores = DungeonRewardCatalog.GetMonsterCoreRewardGrants(d.Grade),
                    d.RewardTable, d.CompletionRewardTableIds }),
                ordinaryCompletionDropChanceAtMasteryZero = pool.DungeonEquipment.DropChanceAtMastery(0),
                ordinaryCompletionDropChanceAtMasteryTen = pool.DungeonEquipment.DropChanceAtMastery(10),
                failure = "Entry consumed; no supply; pending loot lost. Retreat may retain pending loot but awards no supply. Run expires after 48h, not a 48h completion timer.",
                gradeAccess = "Grade I needs no previous dungeon clear; II requires I, III requires II. Region 2 requires server floor 10. Grade accessibility is not proof of beatability.",
                time = "Dungeon routes require sequential actions and victory at the boss. Min/max rooms are production route lengths, not seconds; all successful/failed run minutes are explicit assumptions." },
            floors = rows, earnedInventory = inventory.Items,
            floor11AllNewcomers = new { members = newcomers, inventory = newcomerInventory.Items, preparedHash = newcomersPrepared },
            economy = paceRows,
            dismantling = new { actuallyDismantled = 0, creditedParts = 0,
                retainedUnequippedItems = unequipped.Length, retainedUnequippedPotentialParts = unequipped.Sum(i => TowerAcquisitionInventory.DismantleParts(i, prices)),
                repeatFarm = supply.Supplies.Select(s => new { s.TargetFloor, s.SourceRegion, s.Tier, s.Band,
                    oneSlotPartsPerChest = prices.GetDismantleParts(s.Tier, s.Band.Rank),
                    twoHandedPartsPerChest = 2 * prices.GetDismantleParts(s.Tier, s.Band.Rank) }),
                note = "Potential only; no retained item is spent or dismantled. One chest can select a two-handed item returning twice the one-slot Parts; no Cinders refund. Absent cohort members still own their gear." },
            references = referenceChecks,
            preselectedReferencePath = new { floors = referencePath, inventory = referenceInventory.Items,
                interpretation = "Separate conditional path: exact reference gear chosen before buying that band. Reference list order is declared input. Adapt-after-authored extra costs above are a different path and must not be added to this one. Ordered Essence recipes are supplied, not earned." },
            limitations = new[] { "Cohort identity is explicitly tied to absolute party slot. Absent members retain inventory; new slots join without gear or sigil stock. Levels and ordered level-1 Essences are supplied assumptions, not earned by this model.",
                "Ledger completion ordinals are personal successful awards; analytic failures and resource/time demand are conditional on the scenario, not observed attempts.",
                "Quest sigils are one-time, and prophecy completion, caches, guild/market spending, tournaments and events require other activity. Only the selected daily flat reward is credited; no double counting of extra channels.",
                "Old-region grade-I farming offers the strongest unlocked supply at the same chest cadence. Choosing it does not prove its success rate or subjective effort advantage.",
                "Historical references authenticate consumed cells only, not a new full archive audit. Exact equipment parity shows obtainable descriptors; it does not transfer historical RNG outcomes to the new cohort identities or requalify a changed executable. No combat acceptance is claimed." } };
    }

    public static IReadOnlyList<TowerEquipmentSupply> Available(TowerEquipmentSupplyCatalog catalog,
        IReadOnlyList<DungeonDefinition> dungeons, int clearedFloor, int level, Func<int, bool> released) =>
        dungeons.Where(d => d.Grade == DungeonGrade.GradeI && (d.RequiredTowerFloor ?? 0) <= clearedFloor
                && d.RequiredPreviousDungeonId is null).Select(d => d.Region).Distinct()
            .Select(region => catalog.Candidates(region, level).FirstOrDefault(s => released(s.TargetFloor)
                && s.RequiredClearedFloor <= clearedFloor)).OfType<TowerEquipmentSupply>().ToArray();

    internal static string ReferenceCellId(JsonElement cell) => cell.TryGetProperty("id", out var id)
        ? id.GetString()! : cell.GetProperty("case").GetString() + "/" + cell.GetProperty("profile").GetString();

    private static async Task<string> PrepareAsync(string root, OfflineContent content, TowerSettings settings,
        TowerScenario scenario, IReadOnlyList<FixtureCharacter> characters, CancellationToken token)
    {
        var input = new TowerBattleRunner(root, content).CreateInput(scenario with { Seeds = [1] }, 1,
            settings.Threat, settings.CheckpointIntervalTicks);
        var first = characters[0];
        var setup = content.CreateSetup(first.Materialize(content.Equipment), first.MaterializeEssences());
        var pipeline = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content, setup), setup);
        var request = new WorldTowerCombatRuntimeRequest(StableRandom.Guid(Version, "encounter", scenario.Id),
            StableRandom.Guid(Version, "rally", scenario.Id), input.Floor,
            characters.Select((c, i) => new SnapshotCombatantRequest(TowerBattleRunner.ToSnapshot(c, content),
                new(c.Id.ToString(), c.Id, CombatSide.Friendly, WorldTowerPartyRules.GetPartyNumber(i + 1)))).ToArray(),
            input.Guardian.Deserialize<Creature>(HarnessJson.Options)!, 0, 0, 0, scenario.StartsAt, 1);
        var prepared = await new WorldTowerCombatRuntimeFactory(pipeline).CreateAsync(request, token);
        return HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(prepared));
    }

    public static async Task<int> Command(string[] args, CancellationToken token)
    {
        if (args.Length is < 5 or > 6)
            throw new InvalidDataException("Use tower-acquisition-study <content-root> <fixtures-root> <pace-plan.json> <new-report.json> [reference-list.json]. Zero combat.");
        if (Path.Exists(args[4])) throw new IOException("Choose a new report; historical outputs cannot be overwritten.");
        var references = args.Length == 6 ? TowerContractJson.Read<TowerAcquisitionReference[]>(args[5]) : [];
        if (references.Length > 8) throw new InvalidDataException("At most eight pinned reference checks.");
        var report = await CreateAsync(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]), references, token);
        HarnessJson.WriteNew(args[4], report);
        Console.WriteLine("Conditional acquisition ledger prepared; zero fights, zero seeds; pace assumptions are not measured player evidence.");
        return 0;
    }
}
