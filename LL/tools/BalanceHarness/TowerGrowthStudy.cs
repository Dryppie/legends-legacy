using System.IO.Compression;
using System.Text.Json;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Prophecies;
using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Prophecies;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Dungeons;

namespace BalanceHarness;

public sealed record TowerGrowthPlan(string Version, int CreaturesPerEncounter, int StartingCharacterExperience,
    int StartingEssenceExperience, int StartingMasteryExperience, int CharacterExperienceBonusBps,
    int EssenceExperienceBonusBps, int DefeatRetentionBps, string Assumptions);
public sealed record TowerGrowthDay(DateTimeOffset Day, int Level, IReadOnlyList<TowerProphecyOffer> Daily,
    TowerProphecyOffer Weekly, string? Selected);

public static class TowerGrowthStudy
{
    public const string Version = "tower-growth-qualification-v1";
    public const string ArchivePin = "820ecd26646eb4d1b00b59c14db489054dee86d629b2d83427dc47064027e3c5";
    public static TowerGrowthPlan Read(string fixtures)
    {
        var p = HarnessJson.Read<TowerGrowthPlan>(Path.Combine(fixtures,"tower-growth.json"));
        if (p.Version != Version || p.CreaturesPerEncounter != 1 || p.StartingCharacterExperience != 0 || p.StartingEssenceExperience != 0
            || p.StartingMasteryExperience != 0 || p.CharacterExperienceBonusBps != 0 || p.EssenceExperienceBonusBps != 0 || p.DefeatRetentionBps != 0
            || string.IsNullOrWhiteSpace(p.Assumptions)) throw new InvalidDataException("Changed growth qualification requires a new declaration.");
        return p;
    }

    public static async Task<object> Project(string root, OfflineContent content, JsonElement history, CancellationToken ct)
    {
        var summary = history.GetProperty("summary");
        var before = history.GetProperty("steps")[0].GetProperty("before").Deserialize<FixtureCharacter>(HarnessJson.Options)!;
        var first = history.GetProperty("steps")[0].GetProperty("encounter").GetInt32();
        var quest = summary.GetProperty("questAt").GetInt32();
        var outcome = summary.GetProperty("outcome").GetString()!;
        var growth = new TowerGrowthProgression(root,content,before);
        var source = new TowerEntrySources(root,before.Id,growth.Character,growth.Leveling);
        var epoch = new DateTimeOffset(2026,9,28,0,0,0,TimeSpan.Zero);
        var windows = history.GetProperty("windows").EnumerateArray().ToArray(); var windowIndex = 0;
        var days = new List<TowerGrowthDay>(); var changes = new List<object>(); var checkpoints = new List<object>();
        var rates = new SortedDictionary<string,int>();
        PropheciesOverview? overview = null;
        for (var encounter = 1; encounter <= first; encounter++)
        {
            ct.ThrowIfCancellationRequested();
            var at = epoch.AddSeconds((long)(encounter - 1) * 10);
            if ((encounter - 1) % 8640 == 0)
            {
                overview = await source.Overview(at,ct);
                var daily = overview.DailyProphecies.Select(TowerProphecyOffers.Snapshot).ToArray();
                var weekly = TowerProphecyOffers.Snapshot(overview.GreaterProphecy);
                var choice = TowerProphecyOffers.Choose(overview.DailyProphecies);
                if (choice is not null && !await source.AcceptGeneratedOffer(choice.Id,at,ct)) throw new InvalidDataException("Native growth offer rejected.");
                days.Add(new(at,growth.Character.Level,daily,weekly,choice?.ProphecyDefinitionId));
            }
            while (windows[windowIndex].GetProperty("until").GetInt32() < encounter) windowIndex++;
            var area = windows[windowIndex].GetProperty("area").GetString()!;
            if (!rates.TryGetValue(area,out var xp)) rates[area] = xp = growth.Areas.CalculateEncounterExperience(area,1);
            var oldLevel = growth.Character.Level;
            var oldEssences = growth.State(0).Essences.Select(e => e.Level).ToArray();
            if (TowerActivityInventory.Victory(outcome,encounter))
            {
                await growth.AwardIdle(xp,ct);
                await source.TrackKills(1,at,ct);
                await source.TrackWins(1,at,ct);
                if (growth.LastEssenceExperience > 0) await source.TrackEssenceExperience(growth.LastEssenceExperience,at,ct);
                foreach (var p in overview!.DailyProphecies.Append(overview.GreaterProphecy).Where(p => p.Status == ProphecyStatus.Completed))
                    if (!await source.Claim(p,at,ct)) throw new InvalidDataException("Completed growing prophecy rejected.");
                await source.ClaimMilestones(at,ct);
            }
            if (encounter == quest) growth.Attune(4);
            var state = growth.State(source.Claims.Sum(c => (long)c.Reward.CharacterExperience));
            if (state.Level != oldLevel || !state.Essences.Select(e => e.Level).SequenceEqual(oldEssences)) changes.Add(new { encounter, state });
            if (new[] {2160,8640,25920,86400}.Contains(encounter))
            {
                var quantity = source.State().Items.GetValueOrDefault("sigil_fragment") / source.FragmentCost;
                if (quantity > 0 && !await source.Assemble("goblin_mines",quantity,ct)) throw new InvalidDataException("Growth assembly failed.");
                checkpoints.Add(new { encounter, state, source = source.State() });
            }
        }
        var candidate = growth.Snapshot(before);
        async Task<object> Prepare(FixtureCharacter character)
        {
            var setup = content.CreateSetup(character.Materialize(content.Equipment),character.MaterializeEssences());
            var pipeline = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content,setup),setup);
            var prepared = (await pipeline.PrepareAsync(CombatContentType.Dungeon,
                [new(new("player",character.Id,CombatSide.Friendly,1),new SnapshotCombatantPreparationSource(TowerBattleRunner.ToSnapshot(character,content)))],ct)).Single().Combatant;
            if (HarnessJson.Hash(prepared.Equipment.Select(e => e.ProgressionData).OrderBy(e => e!.State.Id).ToArray())
                != HarnessJson.Hash(character.Equipment.Select(e => e.Data).OrderBy(e => e.State.Id).ToArray())) throw new InvalidDataException("Growth preparation changed retained equipment.");
            return new { prepared.Level, prepared.CombatAttributes, equipment = prepared.Equipment.Select(e => e.ProgressionData).ToArray(),
                essences = prepared.EquippedEssences.Select(e => new { e.Id,e.EssenceDefinitionId,e.Level,e.AscensionTier }).ToArray() };
        }
        return new { history = summary.GetProperty("key").GetString(), owner = before.Id, outcome, horizon = first, questAt = quest,
            rates, days, changes, checkpoints, claims = source.Claims, duplicateClaimsRejected = source.DuplicateClaimsRejected,
            state = growth.State(source.Claims.Sum(c => (long)c.Reward.CharacterExperience)), before, candidate,
            baselinePrepared = await Prepare(before), candidatePrepared = await Prepare(candidate),
            stop = "BeforeFirstArchivedDungeonEntry", transferredDungeonOutcomes = 0, dungeonMasteryAtEntry = 0 };
    }

    public static DungeonRun Reconstruct(Guid owner, DungeonAcquisitionRun recorded)
    {
        if (recorded.Status is not (DungeonRunStatus.Completed or DungeonRunStatus.Failed) || recorded.Actions.Count == 0)
            throw new InvalidDataException("Only recorded terminal runs qualify.");
        var run = new DungeonRun { Id = StableRandom.Guid("dungeon-acquisition-run-v1",owner.ToString(),recorded.Dungeon,recorded.LayoutSeed.ToString()),
            CharacterId = owner, DungeonDefinitionId = recorded.Dungeon, Status = recorded.Status,
            CurrentRoomIndex = recorded.Actions.Last().Room, Rooms = recorded.Layout.GetProperty("rooms").Deserialize<List<RoomInstance>>(HarnessJson.Options)! };
        if (recorded.Actions.Any(a => a.Action is not ("fight" or "rest" or "choose_route") || a.Type is not (RoomType.Combat or RoomType.MiniBoss or RoomType.Boss or RoomType.RestSite)))
            throw new InvalidDataException("Unmodeled mastery action.");
        // Production choose_route resolves combat/rest immediately; it is not merely navigation.
        foreach (var action in recorded.Actions)
            run.Rooms.Single(r => r.RoomIndex == action.Room).Status = RoomInstanceStatus.Completed;
        if (recorded.Failure is not null) run.State.FailureAnalysis = new() { PrimaryCause = recorded.Failure };
        return run;
    }

    public sealed class MasteryRepository : ICharacterDungeonMasteryRepository
    {
        public List<CharacterDungeonMastery> Rows { get; } = [];
        public Task AddAsync(CharacterDungeonMastery row,CancellationToken ct) { Rows.Add(row); return Task.CompletedTask; }
        public Task<CharacterDungeonMastery?> GetAsync(Guid owner,string family,CancellationToken ct) => Task.FromResult(Rows.SingleOrDefault(r => r.CharacterId == owner && r.DungeonDefinitionId == family));
        public Task<IReadOnlyList<CharacterDungeonMastery>> GetForCharacterAsync(Guid owner,IReadOnlyCollection<string> ids,CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CharacterDungeonMastery>>(Rows.Where(r => r.CharacterId == owner && ids.Contains(r.DungeonDefinitionId)).ToArray());
    }

    public static async Task Run(TowerEntrySourceRequest request,CancellationToken ct)
    {
        if (Directory.Exists(request.Output)) throw new InvalidDataException("Fresh output required.");
        void Verify() { foreach (var (path,pin) in request.InputHashes) if (HarnessJson.FileHash(path)!=pin) throw new InvalidDataException("Frozen growth input changed."); }
        Verify(); var plan = Read(request.Fixtures);
        if (HarnessJson.FileHash(Path.Combine(request.Archive,"files.json")) != ArchivePin) throw new InvalidDataException("Wrong native-offer archive.");
        var manifest = HarnessJson.Read<Dictionary<string,string>>(Path.Combine(request.Archive,"files.json"));
        var histories = manifest.Keys.Where(k => k.EndsWith("--offered-kills-no-reroll--history.json")).Order().ToArray();
        if (histories.Length != 32) throw new InvalidDataException("Expected 32 funded histories.");
        var content = OfflineContent.ForTower(request.ApiRoot,TowerBundle.ReadSettings(request.ApiRoot));
        Directory.CreateDirectory(request.Output); var rows = new List<object>();
        foreach (var file in histories)
        {
            var path = Path.Combine(request.Archive,file);
            if (HarnessJson.FileHash(path)!=manifest[file]) throw new InvalidDataException("Historical growth input changed.");
            var history = HarnessJson.Read<JsonElement>(path);
            var projected = JsonSerializer.SerializeToElement(await Project(request.ApiRoot,content,history,ct),HarnessJson.Options);
            var owner = projected.GetProperty("owner").GetGuid();
            var repository = new MasteryRepository(); var mastery = new DungeonMasteryService(repository);
            var masteryPrefix = new List<object>(); object? firstMasteryChange = null;
            foreach (var step in history.GetProperty("steps").EnumerateArray())
            {
                var family = step.GetProperty("dungeon").GetString()!;
                var start = (await mastery.GetMasteryByDungeonAsync(owner,[family],ct))[family];
                if (start.Level != 0)
                {
                    firstMasteryChange = new { ordinal = step.GetProperty("ordinal").GetInt32(), family, start, benefits = DungeonMasteryBenefits.Resolve(start.Level), stop = "BeforeChangedMasteryEntryNoLaterOutcomeTransferred" }; break;
                }
                var runFile = step.GetProperty("file").GetString()!; var runPath = Path.Combine(request.Archive,runFile);
                if (HarnessJson.FileHash(runPath)!=manifest[runFile]) throw new InvalidDataException("Changed mastery run.");
                using var stream = File.OpenRead(runPath); using var gzip = new GZipStream(stream,CompressionMode.Decompress);
                var recorded = (await JsonSerializer.DeserializeAsync<DungeonAcquisitionRun>(gzip,HarnessJson.Options,ct))!;
                var run = Reconstruct(owner,recorded); var award = await mastery.AwardRunMasteryAsync(run,ct);
                var retry = await mastery.AwardRunMasteryAsync(run,ct);
                if (!retry.AlreadyAwarded || retry.ExperienceAwarded != 0 || retry.TotalExperience != award.TotalExperience) throw new InvalidDataException("Mastery retry duplicated credit.");
                masteryPrefix.Add(new { file = runFile, ordinal = step.GetProperty("ordinal").GetInt32(), award });
            }
            var name = file.Replace("--history.json","--growth.json");
            HarnessJson.WriteNew(Path.Combine(request.Output,name),new { historicalFile = file,historicalHash = manifest[file],growth = projected,masteryOnly = new { prefix = masteryPrefix,firstChange = firstMasteryChange,
                states = repository.Rows.OrderBy(r => r.DungeonDefinitionId).Select(r => new { r.DungeonDefinitionId,r.Experience,r.Level,r.CompletionCount }).ToArray() } });
            rows.Add(new { file = name, horizon = projected.GetProperty("horizon").GetInt32(), level = projected.GetProperty("state").GetProperty("level").GetInt32(), changedMasteryEntry = firstMasteryChange is not null });
        }
        Verify();
        HarnessJson.WriteNew(Path.Combine(request.Output,"result.json"),new { version = Version,status = "GrowthDependenciesQualifiedNotCombat",plan,archivePin = ArchivePin,histories = rows,preparations = 64,newFights = 0,newCombatSeeds = 0,measuredPlayerSamples = 0 });
        HarnessJson.WriteNew(Path.Combine(request.Output,"files.json"),Directory.GetFiles(request.Output).Order().ToDictionary(Path.GetFileName,HarnessJson.FileHash));
    }
}
