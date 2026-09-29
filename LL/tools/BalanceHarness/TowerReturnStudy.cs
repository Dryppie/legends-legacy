using System.IO.Compression;
using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerReturnRequest(string ApiRoot,string Fixtures,string RuntimeArchive,string WaveArchive,
    string EntryArchive,string UnlockArchive,string PartyArchive,string Output,IReadOnlyDictionary<string,string> InputHashes,
    string? Qualification=null,string? QualificationPin=null,IReadOnlyDictionary<string,int[]>? Panels=null);
public sealed record TowerReturnOwner(string History,FixtureCharacter Origin,TowerEarnedPoint Point,TowerJourneyRuntime Runtime,
    TowerBlueprintState[] BlueprintProgress,string SourceFile,string SourceHash);
public sealed record TowerReturnProof(string Key,int Path,JsonElement PreviousState,JsonElement[] HistoricalAttempts,
    TowerReturnOwner[] Owners,TowerEarnedParty Party,TowerEarnedParty Baseline,TowerEarnedParty Supplied,
    IReadOnlyDictionary<string,JsonElement> Preparations,JsonElement InitialState);

/// <summary>Continue fixed, individually earned actors on their existing server. Supplied diagnostic arms never earn state.</summary>
public static class TowerReturnStudy
{
    public const string Version="tower-earned-return-v1";
    public const string WavePin="c389b202dd311c78d2e86ca52b06c5607a0e179fc70c2d9efe0be430fd6d8142";
    public const string UnlockPin="fce82fe32b7bfb224f9b6b4d6675a0fca72bc202c12aed54685d3735d698fc52";
    public static readonly string[] Arms=["actual","baseline","supplied"];
    public static DateTimeOffset At(TowerJourneyRuntime runtime)=>TowerJourneyProgression.Epoch.AddSeconds(runtime.IdleSeconds)
        .AddTicks(checked(runtime.CombatTicks*1000000+runtime.WaitingTicks));
    public static TowerJourneyRuntime Wait(TowerJourneyRuntime runtime,DateTimeOffset at)
    {
        var delta=(at-At(runtime)).Ticks;
        if(delta<0)throw new InvalidDataException("Cannot rewind earned activity.");
        return runtime with {WaitingTicks=checked(runtime.WaitingTicks+delta)};
    }
    public static TowerEarnedParty Arm(TowerReturnProof p,string arm)=>arm switch {
        "actual"=>p.Party,"baseline"=>p.Baseline,"supplied"=>p.Supplied,_=>throw new InvalidDataException("Unknown arm.")};
    public static void Save(string output,string file,object value)
    {
        using var memory=new MemoryStream();
        if(file.EndsWith(".gz")){using(var zip=new GZipStream(memory,CompressionLevel.Optimal,true))JsonSerializer.Serialize(zip,value,HarnessJson.Options);}
        else JsonSerializer.Serialize(memory,value,HarnessJson.Options);
        if(Directory.GetFiles(output).Sum(p=>new FileInfo(p).Length)+memory.Length>256*1048576L)throw new InvalidDataException("Return output cap exceeded.");
        using var stream=new FileStream(Path.Combine(output,file),FileMode.CreateNew);memory.Position=0;memory.CopyTo(stream);
    }
    private static void Manifest(string output)=>Save(output,"files.json",Directory.GetFiles(output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    private static JsonElement Plan(TowerReturnRequest q)
    {
        var plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-return.json"));
        if(plan.GetProperty("version").GetString()!=Version||plan.GetProperty("servers").GetInt32()!=32
            ||plan.GetProperty("ownersPerServer").GetInt32()!=16||plan.GetProperty("attemptsPerServer").GetInt32()!=4
            ||plan.GetProperty("maximumFights").GetInt32()!=480)throw new InvalidDataException("Changed return envelope.");
        return plan;
    }
    public static async Task Qualify(TowerReturnRequest q,
        Func<TowerReturnProof,CancellationToken,Task<ITowerUnlockServer>> serverFactory,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=Plan(q);
        foreach(var (folder,pin) in new[] {(q.RuntimeArchive,TowerNextEntryStudy.RuntimePin),(q.WaveArchive,WavePin),
            (q.EntryArchive,TowerRuntimeStudy.EntryPin),(q.UnlockArchive,UnlockPin),(q.PartyArchive,TowerUnlockStudy.PartyPin)})
            if(HarnessJson.FileHash(Path.Combine(folder,"files.json"))!=pin)throw new InvalidDataException("Changed source archive.");
        if(Path.Exists(q.Output))throw new InvalidDataException("Fresh output required.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var inventory=new TowerActivityInventory(q.ApiRoot,content);
        var floors=TowerContentProviders.Floors(Path.Combine(q.ApiRoot,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var bootstrap=TowerBootstrapCohorts.Read(Path.Combine(q.Fixtures,"tower-bootstrap.json"));
        var recipes=TowerBootstrapCohorts.Create(q.ApiRoot,q.Fixtures,bootstrap,content).Where(c=>c.Gear=="common"&&c.EssenceLevel==1).ToDictionary(c=>c.Recipe);
        var catalog=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(q.ApiRoot,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        var personal=HarnessJson.Read<JsonElement>(Path.Combine(q.RuntimeArchive,"result.json")).GetProperty("personal").EnumerateArray()
            .ToDictionary(r=>r.GetProperty("history").GetString()!,r=>TowerUnlockStudy.Read(Path.Combine(q.RuntimeArchive,r.GetProperty("file").GetString()!)));
        var branches=HarnessJson.Read<JsonElement[]>(Path.Combine(q.RuntimeArchive,"branches.json"));
        var wave=HarnessJson.Read<JsonElement>(Path.Combine(q.WaveArchive,"result.json")).GetProperty("results").EnumerateArray()
            .ToDictionary(r=>(r.GetProperty("server").GetString()!,r.GetProperty("history").GetString()!));
        Directory.CreateDirectory(q.Output);var rows=new List<object>();
        foreach(var group in branches.GroupBy(b=>b.GetProperty("server").GetString()!).OrderBy(g=>g.Key,StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();var key=group.Key;var parts=key.Split("--");var rotation=int.Parse(parts[1]);var path=int.Parse(parts[2]);
            var old=TowerUnlockStudy.Read(Path.Combine(q.UnlockArchive,key+"--journey.json"));var previous=old.GetProperty("final");
            var historical=old.GetProperty("steps").EnumerateArray().Select(s=>TowerUnlockStudy.Read(Path.Combine(q.UnlockArchive,s.GetProperty("file").GetString()!))).ToArray();
            var oldParty=TowerUnlockStudy.Read(Path.Combine(q.PartyArchive,$"earned-progression--{parts[0]}--{rotation}--25920--floor-1.json.gz"))
                .GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
            var owners=new List<TowerReturnOwner>();var baselines=new Dictionary<Guid,TowerEarnedPoint>();
            foreach(var branch in group)
            {
                var history=branch.GetProperty("history").GetString()!;var p=personal[history];var row=wave[(key,history)];
                var point=TowerUnlockStudy.Read(Path.Combine(q.EntryArchive,p.GetProperty("checkpointFile").GetString()!))
                    .GetProperty("checkpoint").GetProperty("point").Deserialize<TowerEarnedPoint>(HarnessJson.Options)!;
                baselines.Add(point.Character.Id,point);
                var runtime=p.GetProperty("canonicalRuntime").Deserialize<TowerJourneyRuntime>(HarnessJson.Options)!;
                runtime=Wait(runtime,branch.GetProperty("at").GetDateTimeOffset());
                var blueprint=p.GetProperty("retainedBlueprintProgress").Deserialize<TowerBlueprintState[]>(HarnessJson.Options)!;
                var source=Path.Combine(q.RuntimeArchive,history+"--runtime.json");
                if(row.GetProperty("actualEntries").GetInt32()==1)
                {
                    source=Path.Combine(q.WaveArchive,row.GetProperty("progressFile").GetString()!);var progress=TowerUnlockStudy.Read(source);
                    runtime=progress.GetProperty("after").Deserialize<TowerJourneyRuntime>(HarnessJson.Options)!;
                    blueprint=progress.GetProperty("ordinary").GetProperty("after").Deserialize<TowerBlueprintState[]>(HarnessJson.Options)!;
                    point=point with {Character=progress.GetProperty("character").Deserialize<FixtureCharacter>(HarnessJson.Options)!,
                        Owned=progress.GetProperty("owned").Deserialize<EquipmentData[]>(HarnessJson.Options)!,
                        SupplyItems=point.SupplyItems+(progress.GetProperty("supply").GetProperty("opened").GetBoolean()?1:0)};
                }
                point=point with {AvailableAt=At(runtime)};TowerEarnedPartyStudy.ValidateInventory(point.Character,point.Owned,inventory);
                owners.Add(new(history,p.GetProperty("origin").Deserialize<FixtureCharacter>(HarnessJson.Options)!,point,runtime,blueprint,source,HarnessJson.FileHash(source)));
            }
            if(owners.Count!=16||owners.Select(o=>o.Point.Character.Id).Distinct().Count()!=16)throw new InvalidDataException("Missing personal alternatives.");
            var starts=owners.Select(o=>At(o.Runtime)).Append(previous.GetProperty("at").GetDateTimeOffset()).Max();
            var retained=owners.Select(o=>o with {Runtime=Wait(o.Runtime,starts)}).ToArray();
            foreach(var o in retained)
            {
                var native=TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,o.Origin,o.Runtime);
                TowerRuntimeCopy.Equal(o.Point.Character,native.Growth.Snapshot(o.Point.Character),"post-wave growth");
                TowerRuntimeCopy.Equal(o.Runtime,native.ExportRuntime(),"retained native personal runtime");
            }
            var next=old.GetProperty("highestCleared").GetInt32()+1;var floor=floors.GetFloor(next)!;
            var state=previous.GetProperty("floors")[next-1];
            if(state.GetProperty("isCleared").GetBoolean()||state.GetProperty("unlockedAt").ValueKind==JsonValueKind.Null
                ||state.GetProperty("unlockedAt").GetDateTimeOffset()>starts)throw new InvalidDataException("Next floor not legally unlocked.");
            var members=oldParty.Members.Select(m=>retained.Single(o=>o.Point.Character.Id==m.Character.Id).Point).ToArray();
            var party=oldParty with {Id=key,StartsAt=starts,Floor=floor,Members=members};
            var baseline=party with {Id=key+"--baseline",Members=members.Select(m=>baselines[m.Character.Id]).ToArray()};
            var supplied=party with {Id=key+"--supplied",Members=members.Select(m=> {
                var owned=m.Owned.Concat(TowerContinuationSupply.Targets(content,catalog,"item.tower_supply.v1.floor_04",m.Character.Id,recipes[m.Recipe],bootstrap.PurchaseOrder)).ToArray();
                return m with {Owned=owned,Character=m.Character with {Equipment=inventory.Select(owned)}};
            }).ToArray()};
            var preparations=new Dictionary<string,JsonElement>();
            foreach(var (arm,value) in new[] {("actual",party),("baseline",baseline),("supplied",supplied)})
            {
                var prepared=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,value,0,ct));
                preparations.Add(arm,JsonSerializer.SerializeToElement(new {prepared,hash=HarnessJson.Hash(prepared)},HarnessJson.Options));
            }
            var proof=new TowerReturnProof(key,path,previous,historical,retained,party,baseline,supplied,preparations,default);
            await using var server=await serverFactory(proof,ct);proof=proof with {InitialState=await server.State(ct)};
            Save(q.Output,key+"--preparation.json.gz",proof);
            rows.Add(new {key,floor=next,party.StartsAt,owners=16,participants=members.Length,
                changedLoadouts=members.Count(m=>HarnessJson.Hash(m.Character.Equipment)!=HarnessJson.Hash(baselines[m.Character.Id].Character.Equipment)),
                historicalAttempts=historical.Length});
        }
        if(rows.Count!=32)throw new InvalidDataException("Missing model servers.");
        TowerEarnedPartyStudy.Verify(q.InputHashes);Save(q.Output,"result.json",new {version=Version,status="EarnedReturnPreparationComplete",plan,parties=rows,
            newFights=0,newCombatSeeds=0,measuredPlayerSamples=0});Manifest(q.Output);
    }
    public static async Task Run(TowerReturnRequest q,Func<TowerReturnProof,CancellationToken,Task<ITowerUnlockServer>> serverFactory,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=Plan(q);
        if(Path.Exists(q.Output)||q.Qualification is null||HarnessJson.FileHash(Path.Combine(q.Qualification,"files.json"))!=q.QualificationPin
            ||q.Panels is null||q.Panels.Count!=32||q.Panels.Values.Any(s=>s.Length!=4)||q.Panels.Values.SelectMany(s=>s).Distinct().Count()!=128)
            throw new InvalidDataException("Frozen qualification and 128 fresh seeds required.");
        var settings=TowerBundle.ReadSettings(q.ApiRoot);var content=OfflineContent.ForTower(q.ApiRoot,settings);
        Directory.CreateDirectory(q.Output);var rows=new List<object>();int fights=0,replays=0,attempts=0;
        foreach(var (key,seeds) in q.Panels.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            var proof=TowerUnlockStudy.Read(Path.Combine(q.Qualification,key+"--preparation.json.gz")).Deserialize<TowerReturnProof>(HarnessJson.Options)!;
            await using var server=await serverFactory(proof,ct);TowerRuntimeCopy.Equal(proof.InitialState,await server.State(ct),"restored server");
            var now=proof.Party.StartsAt;var floor=proof.Party.Floor.FloorNumber;var steps=new List<object>();bool success=false;
            var offset=proof.HistoricalAttempts.Count(a=>a.GetProperty("floor").GetInt32()==floor);
            foreach(var index in Enumerable.Range(0,4))
            {
                var seed=seeds[index];var battles=new Dictionary<string,JsonElement>();
                foreach(var arm in Arms)
                {
                    var party=Arm(proof,arm) with {StartsAt=now};
                    async Task<JsonElement> Battle()
                    {
                        ct.ThrowIfCancellationRequested();if(++fights>480)throw new InvalidDataException("Return fight bound exceeded.");
                        var runtime=await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,party,seed,ct);
                        var prepared=IdleBattleRunner.DescribeParticipants(runtime);
                        if(HarnessJson.Hash(prepared)!=proof.Preparations[arm].GetProperty("hash").GetString())throw new InvalidDataException("Prepared party drift.");
                        var result=(await content.CreateExecutor().ExecuteTowerPlaybackAsync(runtime,settings.CheckpointIntervalTicks,ct)).Result;
                        var resolution=new CombatEncounterResultFactory().Create(runtime,result);var guardian=resolution.HostilePostState.Single();
                        return JsonSerializer.SerializeToElement(new {seed,succeeded=resolution.Outcome==BattleOutcome.Victory,
                            guardianHealthRemainingPercent=guardian.MaxHealth<=0?0:Math.Round(100m*guardian.Health/guardian.MaxHealth,2),
                            summary=BattleSummary.From(resolution.CombatResult,6000)},HarnessJson.Options);
                    }
                    var battle=await Battle();battles.Add(arm,battle);
                    if(index==0){var replay=await Battle();replays++;TowerRuntimeCopy.Equal(battle,replay,"return exact replay");Save(q.Output,key+"--"+arm+"--replay.json",replay);}
                }
                var actual=battles["actual"];success=actual.GetProperty("succeeded").GetBoolean();attempts++;
                var receipt=await server.Apply(proof.Party with {StartsAt=now},offset+index,seed,success,actual.GetProperty("summary").GetProperty("durationTicks").GetInt32(),ct);
                var file=key+$"--attempt-{offset+index}.json";
                Save(q.Output,file,new {key,floor,index,attempt=offset+index,seed,battles,receipt});steps.Add(new {file,seed,success});
                now=receipt.GetProperty("after").GetProperty("at").GetDateTimeOffset();if(success)break;
            }
            // Tower rewards are server-side tokens/titles/unlocks only. Passage of time does not synthesize idle activity.
            var retained=proof.Owners.Select(o=>o with {Runtime=Wait(o.Runtime,now)}).ToArray();
            var final=await server.State(ct);
            Save(q.Output,key+"--continuation.json.gz",new {key,initial=proof.InitialState,steps,final,owners=retained,
                stop=success?"NextFloorCleared":"FourAttemptCap",highestCleared=success?floor:floor-1});
            rows.Add(new {key,floor,success,attempts=steps.Count,startedAt=proof.Party.StartsAt,endedAt=now});
        }
        TowerEarnedPartyStudy.Verify(q.InputHashes);Save(q.Output,"result.json",new {version=Version,status="EarnedReturnCombatComplete",plan,q.QualificationPin,
            journeys=rows,attempts,controls=attempts*2,replays,fights,reservedSeeds=128,measuredPlayerSamples=0,earnedNewEquipment=0,searchPerformed=false});Manifest(q.Output);
    }
}
