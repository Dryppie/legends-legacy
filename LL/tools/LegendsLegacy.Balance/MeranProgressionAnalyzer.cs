using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Application.Interfaces.Services.LL.Regions;
using Common.Randomness;
using Domain.Helpers;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Items;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.PowerRatings;
using Services.LL.Spawnings;

namespace LegendsLegacy.Balance;

public sealed record MeranTrial(int Seed, string Outcome, int Ticks, IReadOnlyList<string> Enemies, int Cinders);
public sealed record MeranEconomy(double WinRate, double ScrapPerDay, double CindersPerDay,
    double? PlainTargetHours, double? SigilHours, double? FullItemScrapDays, double? FullItemCinderDays);
public sealed record MeranEncounterResult(string SourceId, string Room, string BuildId, int Level, int Tier, int Rank,
    int EssenceLevel, IReadOnlyList<string> EssenceIds, IReadOnlyList<MeranTrial> Trials, MeranEconomy? Economy);
public sealed record MeranProgressionReport(int Version, int Seed, int TrialsPerCase, string Purpose,
    IReadOnlyDictionary<string, string> ContentHashes, IReadOnlyList<EquipmentReferenceBuildDefinition> Builds,
    IReadOnlyList<MeranEncounterResult> Results);

/// <summary>Detached production PvE fights. Dungeon rooms begin fresh, without optional run buffs or support.</summary>
public sealed class MeranProgressionAnalyzer(string contentRoot, EquipmentReferenceBuildFactory builds,
    ICombatSetupService setup, ICombatEngineExecutor engine, IAreaExperienceBalanceProvider income)
{
    public const string FixtureFileName = "equipment-meran-builds.v1.json";
    private static readonly JsonSerializerOptions Json = EquipmentReferenceCommand.JsonOptions;
    private sealed record RegionDocument(IReadOnlyList<RegionContent> Regions);
    private sealed record RegionContent(string Name, IReadOnlyList<Area> Areas);

    public async Task<MeranProgressionReport> RunAsync(int seed, int trials, int essenceLevel = 10, int dungeonLevel = 50, CancellationToken ct = default)
    {
        if (dungeonLevel is < 50 or > 65) throw new ArgumentOutOfRangeException(nameof(dungeonLevel));
        if (essenceLevel is not (10 or 30)) throw new ArgumentOutOfRangeException(nameof(essenceLevel));
        if (trials is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(trials));
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", FixtureFileName);
        var profiles = Read<EquipmentReferenceBuildDefinition[]>(fixturePath);
        var equipment = JsonStarterEquipmentCatalog.Load(Data("equipment/equipment-starters.v1.json"));
        var ordinary = JsonStarterEquipmentCatalog.LoadOrdinary(equipment, Data("equipment/equipment-ordinary.v1.json"));
        var pool = ordinary.Pools.Single(p => p.EquipmentTier == 2);
        var prices = JsonStarterEquipmentCatalog.LoadForgePrices(Data("equipment/equipment-forge-prices.v1.json"));
        var areas = Read<RegionDocument>(Data("world/regions.json")).Regions.SelectMany(r => r.Areas)
            .Where(a => pool.Areas.Any(p => p.AreaId == a.Id)).ToArray();
        var dungeons = new DungeonDefinitionMaterializer(new DungeonCatalogValidator())
            .Materialize(Read<DungeonCatalogDocument>(Data("dungeons/dungeons.json"))).Where(d => d.Region == 2).ToArray();
        var creatures = WorldTowerCreatureCatalog.Load(Data("world/creatures.json"), Json);
        var byKey = creatures.Values.ToDictionary(c => CreatureEssenceSource.GetMonsterDefinitionId(c)[8..]);
        var cases = new List<MeranEncounterResult>();
        // Equal seeds and source rosters across gear states isolate the equipment change.
        foreach (var profile in profiles)
        foreach (var (tier, rank) in new[] { (1, 5), (2, 0), (2, 3), (2, 5) })
        {
            foreach (var area in areas)
            {
                var build = profile with { CharacterLevel = area.LevelRequirement, Tier = tier, Rank = rank };
                var outcomes = new List<MeranTrial>();
                for (var trial = 0; trial < trials; trial++)
                {
                    ct.ThrowIfCancellationRequested();
                    var encounterSeed = Seed(seed, area.Id, trial);
                    var spawn = new Random(Seed(seed, area.Id + "/spawn", trial));
                    var count = WeightedSpawnSelector.SelectCreatureCount(area.SpawnProbabilities, spawn);
                    var enemies = WeightedSpawnSelector.SelectCreatures(area.Creatures.ToArray(), count, spawn)
                        .Select(c => creatures[c.CreatureId]).ToArray();
                    outcomes.Add(await Fight(build, enemies, area, null, encounterSeed, essenceLevel, ct));
                }
                var economy = ProjectEconomy(outcomes, pool.Areas.Single(a => a.AreaId == area.Id).ScrapPerPerfectDay,
                    pool, prices.ForTier(2));
                cases.Add(Result(area.Id, "Ordinary", build, outcomes, economy, essenceLevel));
            }
            foreach (var dungeon in dungeons)
            foreach (var room in dungeon.Rooms.Where(r => r.EncounterIds.Count > 0))
            {
                var build = profile with { CharacterLevel = dungeonLevel, Tier = tier, Rank = rank };
                var area = new Area { DifficultyTier = DungeonEnemyDifficultyScaling.GetProgressionPosition(dungeon.Tier, dungeon.Region) };
                var enemies = room.EncounterIds.Select(key => byKey[key]).ToArray();
                var outcomes = new List<MeranTrial>();
                for (var trial = 0; trial < trials; trial++)
                    outcomes.Add(await Fight(build, enemies, area, dungeon, Seed(seed, dungeon.Id + "/" + room.Type, trial), essenceLevel, ct));
                cases.Add(Result(dungeon.Id, room.Type.ToString(), build, outcomes, null, essenceLevel));
            }
            Console.WriteLine($"Meran assessment: {profile.Id}, tier {tier}, rank {rank} complete.");
        }
        var paths = Directory.GetFiles(Path.Combine(contentRoot, "Data"), "*.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal).ToArray();
        var hashes = paths.ToDictionary(p => Path.GetRelativePath(contentRoot, p).Replace('\\', '/'),
            p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        hashes["Fixtures/" + FixtureFileName] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixturePath)));
        return new(1, seed, trials,
            "Solo Meran PvE; authored spawns/abilities/scaling, live 6000-tick limit and initial cooldowns. " +
            "Six Shenic Essences at the reported level, ascended once at level 30, no evolutions, Soulstones or run buffs. Areas at entry level; dungeon rooms at the reported character level. " +
            "Room results are not full-run completion probabilities. Economy uses measured victories, a 10-second cadence, and no bonus/discovery/dungeon income.",
            hashes, profiles, cases);
    }

    private async Task<MeranTrial> Fight(EquipmentReferenceBuildDefinition definition, IReadOnlyList<Creature> sources,
        Area area, DungeonDefinition? dungeon, int seed, int essenceLevel, CancellationToken ct)
    {
        var build = builds.Create(definition);
        foreach (var essence in build.EquippedEssences) { essence.Level = essenceLevel; essence.AscensionTier = essenceLevel > 10 ? 1 : 0; }
        var player = new CombatEntity(build.Character) { Id = "player", EquippedEssences = [.. build.EquippedEssences], HasEquippedEssenceSnapshot = true };
        var playerSlot = new CombatParticipantSlot(player.Id, build.Character.Id, CombatSide.Friendly);
        var hostiles = sources.Select((source, i) => {
            var clone = Clone(source);
            var entity = setup.CreateCreatureCombatEntities([clone], area).Single();
            entity.Id = "enemy-" + i.ToString(CultureInfo.InvariantCulture);
            if (dungeon != null) DungeonEnemyDifficultyScaling.Apply(entity, dungeon.Tier, dungeon.EnemyStrengthMultiplier);
            return new CombatRuntimeParticipant(new(entity.Id, clone.Id, CombatSide.Hostile), clone, entity);
        }).ToArray();
        await setup.PrepareEntitiesForCombat([player, .. hostiles.Select(x => x.Combatant)],
            dungeon == null ? EssenceCombatActivity.IdleCombat : EssenceCombatActivity.Dungeon);
        var mode = dungeon == null ? CombatMode.Idle : CombatMode.Dungeon;
        CombatEncounterSourceContext context = dungeon == null
            ? new IdleEncounterSourceContext(build.Character.Id, area, TimeSpan.FromSeconds(10))
            : new DungeonEncounterSourceContext(Guid.Empty);
        var plan = new CombatEncounterPlan(StableRandom.Guid("meran-pve-v1", seed.ToString(CultureInfo.InvariantCulture)), mode,
            1, DateTimeOffset.UnixEpoch, [playerSlot, .. hostiles.Select(x => x.Slot)], context)
        { ContentType = dungeon == null ? CombatContentType.Idle : CombatContentType.Dungeon, RandomSeed = seed, CaptureEventLog = false };
        var result = await engine.ExecuteAsync(new(plan, [new(playerSlot, build.Character, player)], hostiles), false, ct);
        return new(seed, result.Outcome.ToString(), result.Duration, sources.Select(x => x.Name).ToArray(),
            dungeon == null && result.Outcome == BattleOutcome.Victory ? income.CalculateEncounterCinders(area.Id, sources.Count) : 0);
    }

    public static MeranEconomy ProjectEconomy(IReadOnlyList<MeranTrial> outcomes, int scrapPerDay,
        CombatAcquisitionRules pool, ForgeTierPrices price)
    {
        if (outcomes.Count == 0) throw new ArgumentException("Measured encounters are required.");
        var wins = outcomes.Count(x => x.Outcome == nameof(BattleOutcome.Victory));
        var winRate = (double)wins / outcomes.Count;
        var scrap = scrapPerDay * winRate;
        var cinders = outcomes.Sum(x => (double)x.Cinders) / outcomes.Count * pool.VictoriesPerPerfectDay;
        var victoriesPerHour = pool.VictoriesPerPerfectDay / 24d * winRate;
        return new(winRate, scrap, cinders,
            wins == 0 ? null : pool.PlainTargetVictories / victoriesPerHour,
            wins == 0 ? null : pool.SigilVictories / victoriesPerHour,
            scrap == 0 ? null : price.RankScrapCosts.Sum() / scrap,
            cinders == 0 ? null : price.RankCinderCosts.Sum() / cinders);
    }

    private static MeranEncounterResult Result(string source, string room, EquipmentReferenceBuildDefinition build,
        List<MeranTrial> trials, MeranEconomy? economy, int essenceLevel) =>
        new(source, room, build.Id, build.CharacterLevel, build.Tier, build.Rank, essenceLevel, build.EssenceIds, trials, economy);
    private static int Seed(int seed, string source, int trial) => StableRandom.Seed("meran-pve-v1",
        seed.ToString(CultureInfo.InvariantCulture), source, trial.ToString(CultureInfo.InvariantCulture));
    private string Data(string relative) => Path.Combine(contentRoot, "Data", relative);
    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
        ?? throw new InvalidOperationException($"Invalid Meran assessment input: {path}");
    private static Creature Clone(Creature c) => new() { Id = c.Id, Name = c.Name, ImagePath = c.ImagePath,
        Archetype = c.Archetype, DamageProfile = c.DamageProfile, DefenseProfile = c.DefenseProfile,
        RewardTableId = c.RewardTableId, BaseLevel = c.BaseLevel, Level = c.Level, Tier = c.Tier,
        StatOverrides = c.StatOverrides.ToArray(), BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(c.Id) };
}
