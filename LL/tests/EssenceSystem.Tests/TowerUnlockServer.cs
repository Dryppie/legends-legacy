using System.Text.Json;
using Application.Interfaces.Services.LL.Items;
using Services.LL.Interfaces;
using Application.Interfaces.Services.LL.WorldTower;
using Application.UseCases.WorldTower.Dtos;
using BalanceHarness;
using Common.Randomness;
using Domain.Models.Achievements;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Persistence.LL;
using Persistence.LL.Repositories.Achievements;
using Persistence.LL.Repositories.WorldTower;
using Services.LL.Achievements;
using Services.LL.Items;
using Services.LL.Synchronization;
using Services.LL.WorldTower;
using static BalanceHarness.TowerGrowthProgression;

namespace EssenceSystem.Tests;

// Reuse the existing isolated InMemory provider/outbox fixtures; never construct an API host or database connection.
public sealed partial class WorldTowerServiceTests
{
    internal sealed class UnlockClock(DateTimeOffset now):TimeProvider
    {
        public DateTimeOffset Now=now;
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    internal sealed partial class UnlockServer:ITowerUnlockServer
    {
        private const string Server="test-server";
        private readonly LLDbContext db=CreateDbContext();
        private readonly MemoryCache cache=new(new MemoryCacheOptions());
        private readonly IWorldTowerDefinitionProvider definitions;
        private readonly WorldTowerService service;
        private readonly TowerEquipmentSupplyService supply;
        private readonly UnlockClock clock;
        private readonly TowerEarnedParty starting;
        private readonly IReadOnlyList<FixtureCharacter> actors;
        private readonly Dictionary<Guid,DungeonRun> oldRuns=[];
        private readonly string key;
        public static async Task<UnlockServer> Create(string root,OfflineContent content,TowerEarnedParty party,TowerEarnedPoint outsider,int path,CancellationToken ct,
            IReadOnlyDictionary<Guid,int>? experience=null,IReadOnlyList<FixtureCharacter>? population=null)
        {
            var server=new UnlockServer(root,content,party,outsider,path,experience,population);
            await server.db.SaveChangesAsync(ct);
            foreach(var c in server.actors)
            {
                var run=server.Run(c,"before-tower");await server.supply.CompleteAsync(run,1,ct);server.oldRuns.Add(c.Id,run);
            }
            return server;
        }
        private UnlockServer(string root,OfflineContent content,TowerEarnedParty party,TowerEarnedPoint outsider,int path,IReadOnlyDictionary<Guid,int>? experience,
            IReadOnlyList<FixtureCharacter>? population=null)
        {
            TowerEarnedPartyStudy.ValidateParty(party);
            if(party.Members.Any(m=>m.Character.Id==outsider.Character.Id)||outsider.Outcome!=party.Outcome||outsider.Horizon!=party.Horizon)
                throw new InvalidDataException("Outsider probe needs another earned owner from this checkpoint alternative.");
            starting=party;key=$"{party.Outcome}--{party.Rotation}--{path}";clock=new(party.StartsAt);
            actors=population??party.Members.Select(m=>m.Character).Append(outsider.Character).ToArray();
            if(actors.Select(c=>c.Id).Distinct().Count()!=actors.Count||party.Members.Any(m=>actors.All(c=>c.Id!=m.Character.Id)))
                throw new InvalidDataException("Invalid personal population.");
            definitions=TowerContentProviders.Floors(Path.Combine(root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
            db.TitleDefinitions.RemoveRange(db.TitleDefinitions);
            db.TitleDefinitions.AddRange(HarnessJson.Read<TitleDefinition[]>(Path.Combine(root,"Data/titles/world-tower.json")));
            foreach(var c in actors)db.Characters.Add(new Character {Id=c.Id,UserId=c.Id,Name=c.Name,Level=c.Level,Experience=experience?[c.Id]??0});
            foreach(var f in definitions.GetFloors())db.TowerFloorProgresses.Add(new TowerFloorProgress {
                Id=StableRandom.Guid(TowerUnlockStudy.Version,key,"progress",f.FloorNumber.ToString()),ServerId=Server,FloorNumber=f.FloorNumber,
                CreatedAt=clock.Now,UpdatedAt=clock.Now,UnlockedAt=f.FloorNumber==1?clock.Now:null});
            var outbox=new TestGameEventOutbox();
            var config=new ConfigurationBuilder().AddJsonFile(Path.Combine(root,"appsettings.json")).Build();
            var settings=config.GetSection("WorldTower").Get<WorldTowerOptions>()!;settings.ServerId=Server;
            var options=Options.Create(settings);
            var achievements=new AchievementRepository(db);
            var titles=new WorldTowerTitleService(new WorldTowerTitleRepository(db),achievements,new AchievementService(achievements),definitions,outbox,options,clock);
            var snapshotBoundary=Boundary<ICharacterSnapshotService>((m,a)=> {
                if(m.Name!="CreateAsync")throw new InvalidDataException(m.Name);
                var snapshot=TowerBattleRunner.ToSnapshot(actors.Single(c=>c.Id==(Guid)a[0]!),content);
                db.CharacterSnapshots.Add(snapshot);return Task.FromResult(snapshot);
            });
            service=new WorldTowerService(db:db,rallies:new WorldTowerRallyRepository(db),definitions:definitions,snapshots:snapshotBoundary,
                powerRatings:new FixedPowerRatingService(actors.Select(c=>(c.Id,1)).ToArray()),
                entities:null!,combatEngine:null!,runtimeFactory:null!,creatureAbilities:null!,abilityCatalog:null!,resultFactory:null!,
                outbox:outbox,stateSync:new StateSyncService(db,new UnexpectedRealtimeBroadcaster(),clock),mapper:null!,options:options,
                jsonOptions:HarnessJson.Options,playbackCache:cache,timeProvider:clock,logger:NullLogger<WorldTowerService>.Instance,titles:titles);
            var snapshots=actors.ToDictionary(c=>c.Id,c=>new CharacterSnapshot {Id=c.Id,CharacterId=c.Id,Level=c.Level});
            var bases=HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/items/items.json")).EnumerateArray()
                .Where(e=>e.GetProperty("id").GetString()!.StartsWith("item.tower_supply.",StringComparison.Ordinal))
                .Select(e=>e.Deserialize<ItemBase>(HarnessJson.Options)!).ToDictionary(e=>e.Id);
            supply=new(JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(root,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment),
                new WorldTowerProgressRepository(db),definitions,
                Boundary<ICharacterSnapshotRepository>((m,a)=>m.Name=="GetSnapshotByIdAsync"?Task.FromResult<CharacterSnapshot?>(snapshots[(Guid)a[0]!]):throw new InvalidDataException(m.Name)),
                Boundary<IDungeonRunRepository>((m,a)=> {
                    if(m.Name!="AddPendingRewardAsync")throw new InvalidDataException(m.Name);
                    var run=(DungeonRun)a[0]!;var reward=(RunReward)a[1]!;
                    if(run.PendingRewards.Any(r=>r.Id==reward.Id))return Task.FromResult(false);
                    run.PendingRewards.Add(reward);return Task.FromResult(true);
                }),
                Boundary<IItemBaseRepository>((m,a)=>m.Name=="GetItemBasesByIdsAsync"
                    ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(((IReadOnlyCollection<string>)a[0]!).ToDictionary(id=>id,id=>bases[id]))
                    :throw new InvalidDataException(m.Name)),options,Options.Create(config.GetSection("EquipmentProgression").Get<EquipmentProgressionOptions>()??new()));
        }
        private DungeonRun Run(FixtureCharacter c,string suffix)=>new() {
            Id=StableRandom.Guid(TowerUnlockStudy.Version,key,c.Id.ToString(),suffix),CharacterId=c.Id,CharacterSnapshotId=c.Id,
            DungeonDefinitionId="goblin_mines",Status=DungeonRunStatus.Completed};
        public async Task<JsonElement> State(CancellationToken ct)
        {
            var probes=new List<object>();
            foreach(var c in actors)
            {
                var probe=Run(c,clock.Now.ToString("O"));await supply.CompleteAsync(probe,1,ct);
                var old=oldRuns[c.Id];await supply.CompleteAsync(old,1,ct);
                probes.Add(new {owner=c.Id,level=c.Level,participating=starting.Members.Any(m=>m.Character.Id==c.Id),
                    hypotheticalNextCompletedDungeonChest=probe.PendingRewards.SingleOrDefault()?.ItemId,
                    oldCompletedDecisionChest=old.PendingRewards.SingleOrDefault()?.ItemId,old.State.TowerEquipmentSupplyProcessed});
            }
            return JsonSerializer.SerializeToElement(new {at=clock.Now,
                floors=await db.TowerFloorProgresses.OrderBy(f=>f.FloorNumber).Select(f=>new {f.FloorNumber,f.IsCleared,f.UnlockedAt,f.ClearedAt,f.FirstClearAttemptId,f.ScoutingProgress}).ToArrayAsync(ct),
                tokens=await db.Characters.OrderBy(c=>c.Id).Select(c=>new {owner=c.Id,c.TowerTokens,c.Level,c.Experience}).ToArrayAsync(ct),
                titles=await db.PlayerTitleUnlocks.Include(t=>t.TitleDefinition).OrderBy(t=>t.CharacterId).ThenBy(t=>t.TitleDefinition.Key)
                    .Select(t=>new {owner=t.CharacterId,key=t.TitleDefinition.Key}).ToArrayAsync(ct),
                unlocks=await db.ServerUnlocks.OrderBy(u=>u.SourceFloorNumber).ThenBy(u=>u.UnlockKey).Select(u=>new {u.UnlockKey,u.SourceFloorNumber,u.UnlockedAt}).ToArrayAsync(ct),
                probes,earnedNewEquipment=0},HarnessJson.Options);
        }
        public async Task<JsonElement> Apply(TowerEarnedParty party,int ordinal,int seed,bool succeeded,int ticks,CancellationToken ct)
        {
            if(ticks is <0 or >6000||party.StartsAt!=clock.Now)throw new InvalidDataException("Invalid chronological Tower attempt.");
            TowerEarnedPartyStudy.ValidateParty(party);
            if(!party.Members.Select(m=>HarnessJson.Hash(m.Character)).SequenceEqual(starting.Members.Select(m=>HarnessJson.Hash(m.Character))))
                throw new InvalidDataException("Changed unearned actor state.");
            var f=await db.TowerFloorProgresses.SingleAsync(f=>f.FloorNumber==party.Floor.FloorNumber,ct);
            if(f.IsCleared||f.UnlockedAt is null||f.UnlockedAt>clock.Now)throw new InvalidDataException("First-clear floor is unavailable.");
            var before=await State(ct);var id=StableRandom.Guid(TowerUnlockStudy.Version,key,party.Floor.FloorNumber.ToString(),ordinal.ToString(),seed.ToString());
            var end=clock.Now.AddSeconds(ticks/10d);
            var rally=new TowerRally {Id=id,ServerId=Server,FloorNumber=party.Floor.FloorNumber,Mode=TowerRallyMode.FirstClear,
                Status=TowerRallyStatus.InProgress,RequiredSlots=party.Floor.RequiredSlots,CreatedAt=clock.Now,StartedAt=clock.Now,
                CreatedByCharacterId=party.Members[0].Character.Id,
                Participants=party.Members.Select((m,i)=>new TowerRallyParticipant {Id=StableRandom.Guid(id.ToString(),m.Character.Id.ToString()),
                    CharacterId=m.Character.Id,AccountId=m.Character.Id,CharacterName=m.Character.Name,PartySlot=i+1,JoinedAt=clock.Now}).ToList()};
            var report=new TowerBattleReportDto(party.Floor.FloorNumber,party.Floor.GuardianName,succeeded,null,(int)Math.Ceiling(ticks/10d),0,[],null!);
            var attempt=new TowerAttempt {Id=id,TowerRally=rally,ServerId=Server,FloorNumber=party.Floor.FloorNumber,Mode=TowerRallyMode.FirstClear,
                Status=TowerAttemptStatus.Playback,AttemptNumberForFloor=ordinal+1,StartedAt=clock.Now,FightDurationSeconds=(int)Math.Ceiling(ticks/10d),
                BattleReportJson=JsonSerializer.Serialize(report,HarnessJson.Options)};
            var playback=new TowerCombatPlayback {TowerAttempt=attempt,TowerAttemptId=id,TotalTicks=ticks,SimulationCompletedAt=clock.Now,
                PlaybackStartedAt=clock.Now,PlaybackEndsAt=end,FinalizationLeaseOwner="offline-owner",FinalizationLeaseUntil=end.AddMinutes(1),
                BundleHash="offline-finalization-boundary",BundleContentType="application/json",BundleContentEncoding="br"};
            db.TowerCombatPlaybacks.Add(playback);await db.SaveChangesAsync(ct);db.ChangeTracker.Clear();
            if(await service.FinalizePlaybackAsync(id,"offline-owner",end.AddTicks(-1),ct))throw new InvalidDataException("Native playback finalized early.");
            clock.Now=end;
            if(!await service.FinalizePlaybackAsync(id,"offline-owner",end,ct))throw new InvalidDataException("Native finalization failed.");
            db.ChangeTracker.Clear();var after=await State(ct);
            if(await service.FinalizePlaybackAsync(id,"offline-owner",end,ct)||HarnessJson.Hash(after)!=HarnessJson.Hash(await State(ct)))
                throw new InvalidDataException("Finalization retry changed rewards or server progression.");
            var native=await db.TowerAttempts.SingleAsync(a=>a.Id==id,ct);
            return JsonSerializer.SerializeToElement(new {attemptId=id,before,after,native.Status,earlyFinalizationRejected=true,duplicateFinalizationRejected=true},HarnessJson.Options);
        }
        public async ValueTask DisposeAsync(){cache.Dispose();await db.DisposeAsync();}
    }
}
