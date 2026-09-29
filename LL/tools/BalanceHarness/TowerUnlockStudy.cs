using System.IO.Compression;
using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution;

namespace BalanceHarness;

public sealed record TowerUnlockPlan(string Version,int Checkpoint,int PathsPerParty,int MaximumFloor,int AttemptsPerFloor,string Assumptions);
public sealed record TowerUnlockRequest(string ApiRoot,string Fixtures,string Archive,string Output,
    IReadOnlyDictionary<string,string> InputHashes,string? Qualification=null,string? QualificationPin=null,
    IReadOnlyDictionary<string,int[]>? Panels=null,string? GrowthArchive=null);
public interface ITowerUnlockServer : IAsyncDisposable
{
    Task<JsonElement> State(CancellationToken ct);
    Task<JsonElement> Apply(TowerEarnedParty party,int attempt,int seed,bool succeeded,int durationTicks,CancellationToken ct);
}

/// <summary>Fixed early-floor continuation. Frozen checkpoint actors gain no rewards outside the native finalization boundary.</summary>
public static class TowerUnlockStudy
{
    public const string Version="tower-early-unlock-v1";
    public const string PartyPin="d96b80e062b07a14712ace1686857b8c6b377845af683dcf0b69146dd3d731f2";
    public static TowerUnlockPlan Plan(string fixtures)
    {
        var p=HarnessJson.Read<TowerUnlockPlan>(Path.Combine(fixtures,"tower-early-unlock.json"));
        if(p.Version!=Version || p.Checkpoint!=25920 || p.PathsPerParty!=4 || p.MaximumFloor!=3 || p.AttemptsPerFloor!=4)
            throw new InvalidDataException("Changed early-unlock envelope.");
        return p;
    }
    public static JsonElement Read(string path)
    {
        if(!path.EndsWith(".gz",StringComparison.Ordinal))return HarnessJson.Read<JsonElement>(path);
        using var file=File.OpenRead(path);using var zip=new GZipStream(file,CompressionMode.Decompress);
        return JsonSerializer.Deserialize<JsonElement>(zip,HarnessJson.Options);
    }
    private static void Save(string path,object value)
    {
        var bytes=JsonSerializer.SerializeToUtf8Bytes(value,HarnessJson.Options);
        if(Directory.GetFiles(Path.GetDirectoryName(path)!).Sum(p=>new FileInfo(p).Length)+bytes.Length>256*1048576L)
            throw new InvalidDataException("Early-unlock output bound exceeded.");
        using var stream=new FileStream(path,FileMode.CreateNew);stream.Write(bytes);
    }
    private static void Manifest(string output)=>Save(Path.Combine(output,"files.json"),Directory.GetFiles(output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    public static async Task Qualify(TowerUnlockRequest request,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(request.InputHashes);var plan=Plan(request.Fixtures);
        if(Path.Exists(request.Output)||HarnessJson.FileHash(Path.Combine(request.Archive,"files.json"))!=PartyPin)
            throw new InvalidDataException("Pinned earned-party archive and fresh qualification output required.");
        var manifest=HarnessJson.Read<Dictionary<string,string>>(Path.Combine(request.Archive,"files.json"));
        var files=manifest.Keys.Where(f=>f.StartsWith("earned-progression--",StringComparison.Ordinal)&&f.EndsWith("--25920--floor-1.json.gz",StringComparison.Ordinal)).Order().ToArray();
        if(files.Length!=8)throw new InvalidDataException("Expected all eight earned checkpoint alternatives.");
        var content=OfflineContent.ForTower(request.ApiRoot,TowerBundle.ReadSettings(request.ApiRoot));
        var floors=TowerContentProviders.Floors(Path.Combine(request.ApiRoot,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        Directory.CreateDirectory(request.Output);var rows=new List<object>();
        foreach(var file in files)
        {
            if(HarnessJson.FileHash(Path.Combine(request.Archive,file))!=manifest[file])throw new InvalidDataException("Historical party changed.");
            var original=Read(Path.Combine(request.Archive,file)).GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
            foreach(var floor in Enumerable.Range(1,plan.MaximumFloor))
            {
                var party=original with {Id=$"{original.Outcome}--{original.Rotation}--floor-{floor}",Floor=floors.GetFloor(floor)!};
                var prepared=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(request.ApiRoot,content,party,0,ct));
                var name=party.Id+".json";
                Save(Path.Combine(request.Output,name),new {party,prepared,preparedHash=HarnessJson.Hash(prepared),sourceFile=file,sourceHash=manifest[file],
                    unlockAssumedForPreparationOnly=floor>1});
                rows.Add(new {file=name,floor,party.Outcome,party.Rotation,party.StartsAt});
            }
        }
        TowerEarnedPartyStudy.Verify(request.InputHashes);
        Save(Path.Combine(request.Output,"result.json"),new {version=Version,status="EarlyUnlockPreparationComplete",plan,sourcePin=PartyPin,
            parties=rows,newFights=0,newCombatSeeds=0,measuredPlayerSamples=0});Manifest(request.Output);
    }
    public static async Task Run(TowerUnlockRequest request,
        Func<TowerEarnedParty,int,CancellationToken,Task<ITowerUnlockServer>> serverFactory,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(request.InputHashes);var plan=Plan(request.Fixtures);
        if(Path.Exists(request.Output)||request.Qualification is null||HarnessJson.FileHash(Path.Combine(request.Qualification,"files.json"))!=request.QualificationPin
            ||request.Panels is null||request.Panels.Count!=32||request.Panels.Values.Any(p=>p.Length!=12)||request.Panels.Values.SelectMany(p=>p).Distinct().Count()!=384)
            throw new InvalidDataException("Qualified preparations and fresh fixed 384-seed continuation required.");
        var manifest=HarnessJson.Read<Dictionary<string,string>>(Path.Combine(request.Qualification,"files.json"));
        foreach(var (name,hash) in manifest)if(HarnessJson.FileHash(Path.Combine(request.Qualification,name))!=hash)throw new InvalidDataException("Preparation changed.");
        var settings=TowerBundle.ReadSettings(request.ApiRoot);var content=OfflineContent.ForTower(request.ApiRoot,settings);
        Directory.CreateDirectory(request.Output);int fights=0,replays=0;var rows=new List<object>();
        foreach(var outcome in TowerEarnedPartyStudy.Outcomes)
        foreach(var rotation in Enumerable.Range(0,4))
        foreach(var path in Enumerable.Range(0,plan.PathsPerParty))
        {
            var key=$"{outcome}--{rotation}--{path}";var seeds=request.Panels[key];var steps=new List<object>();
            var first=Read(Path.Combine(request.Qualification,$"{outcome}--{rotation}--floor-1.json")).GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
            await using var server=await serverFactory(first,path,ct);
            var initial=await server.State(ct);int cleared=0;var now=first.StartsAt;
            foreach(var floor in Enumerable.Range(1,plan.MaximumFloor))
            {
                var file=$"{outcome}--{rotation}--floor-{floor}.json";var proof=Read(Path.Combine(request.Qualification,file));
                var frozen=proof.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
                bool success=false;
                foreach(var attempt in Enumerable.Range(0,plan.AttemptsPerFloor))
                {
                    var seed=seeds[(floor-1)*plan.AttemptsPerFloor+attempt];
                    var party=frozen with {StartsAt=now,Id=$"{key}--floor-{floor}--attempt-{attempt}"};
                    async Task<JsonElement> Battle()
                    {
                        ct.ThrowIfCancellationRequested();if(++fights>480)throw new InvalidDataException("Early-unlock combat bound exceeded.");
                        var runtime=await TowerEarnedPartyStudy.Prepare(request.ApiRoot,content,party,seed,ct);
                        var prepared=IdleBattleRunner.DescribeParticipants(runtime);
                        if(HarnessJson.Hash(prepared)!=proof.GetProperty("preparedHash").GetString())throw new InvalidDataException("Checkpoint preparation changed.");
                        var result=(await content.CreateExecutor().ExecuteTowerPlaybackAsync(runtime,settings.CheckpointIntervalTicks,ct)).Result;
                        var resolution=new CombatEncounterResultFactory().Create(runtime,result);
                        var guardian=resolution.HostilePostState.Single();
                        return JsonSerializer.SerializeToElement(new {seed,succeeded=resolution.Outcome==BattleOutcome.Victory,
                            guardianHealthRemainingPercent=guardian.MaxHealth<=0?0:Math.Round(100m*guardian.Health/guardian.MaxHealth,2),
                            summary=BattleSummary.From(resolution.CombatResult,6000)},HarnessJson.Options);
                    }
                    var battle=await Battle();var name=party.Id+".json";
                    if(attempt==0)
                    {
                        var replay=await Battle();replays++;
                        Save(Path.Combine(request.Output,party.Id+"--replay.json"),replay);
                        if(HarnessJson.Hash(battle)!=HarnessJson.Hash(replay))throw new InvalidDataException("Unlock playback replay mismatch.");
                    }
                    success=battle.GetProperty("succeeded").GetBoolean();var ticks=battle.GetProperty("summary").GetProperty("durationTicks").GetInt32();
                    var receipt=await server.Apply(party,attempt,seed,success,ticks,ct);
                    now=now.AddSeconds(ticks/10d);
                    if(receipt.GetProperty("after").GetProperty("at").GetDateTimeOffset()!=now)throw new InvalidDataException("Finalization clock drift.");
                    Save(Path.Combine(request.Output,name),new {key,floor,attempt,sourceFile=file,sourceHash=manifest[file],
                        preparedHash=proof.GetProperty("preparedHash").GetString(),battle,receipt});
                    steps.Add(new {file=name,floor,attempt,seed,success,startedAt=party.StartsAt,endedAt=now});
                    if(success){cleared=floor;break;}
                }
                if(!success)break;
            }
            var final=await server.State(ct);
            Save(Path.Combine(request.Output,key+"--journey.json"),new {key,outcome,rotation,path,initial,steps,final,
                stop=cleared==3?"FirstSupplyUpgradeUnlocked":"FloorAttemptCap",highestCleared=cleared,
                unchangedOwnedCharacterHashes=first.Members.Select(m=>new {owner=m.Character.Id,characterHash=HarnessJson.Hash(m.Character),ownedHash=HarnessJson.Hash(m.Owned)}).ToArray()});
            rows.Add(new {key,cleared,attempts=steps.Count,startedAt=first.StartsAt,endedAt=now});
        }
        TowerEarnedPartyStudy.Verify(request.InputHashes);
        Save(Path.Combine(request.Output,"result.json"),new {version=Version,status="EarlyTowerUnlockDiagnosticComplete",request.QualificationPin,
            journeys=rows,fights,replays,newSeeds=384,maximumFights=480,measuredPlayerSamples=0,earnedNewEquipment=0,searchPerformed=false});Manifest(request.Output);
    }
}
