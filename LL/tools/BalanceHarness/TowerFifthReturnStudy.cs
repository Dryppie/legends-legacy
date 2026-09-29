using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerFifthReturnRequest(string ApiRoot,string Fixtures,string SourceArchive,string Output,
    IReadOnlyDictionary<string,string> InputHashes,string? Qualification=null,string? QualificationPin=null,IReadOnlyDictionary<string,int[]>? Panels=null);
public sealed record TowerFifthReturnProof(string SourceFile,string SourceHash,int PersonalRefreshBefore,int PopulationRefreshBefore,int NextPersonalRefreshBefore,
    TowerReturnProof Continuation,JsonElement Eligibility,JsonElement[] Essences);

/// <summary>Return the actual earned five-Essence parties to their retained Tower histories, without extra training or rewards.</summary>
public static class TowerFifthReturnStudy
{
    public const string Version="tower-earned-fifth-return-v1";
    public const string SourcePin="963fd340840d7b1f7d9f3e16eef9f57a97fec4445b322ef7d157e6adbd5f37c7";
    public static readonly string[] Arms=["actual","supplied"];
    private static JsonElement Plan(TowerFifthReturnRequest q)
    {
        var p=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-fifth-return.json"));
        if(p.GetProperty("version").GetString()!=Version||p.GetProperty("eligibleServers").GetInt32()!=15||p.GetProperty("targetFloor").GetInt32()!=5
            ||p.GetProperty("partySize").GetInt32()!=10||p.GetProperty("attemptsPerServer").GetInt32()!=4||p.GetProperty("maximumFights").GetInt32()!=150)
            throw new InvalidDataException("Changed fifth return envelope.");
        return p;
    }
    private static void Manifest(string output)=>TowerReturnStudy.Save(output,"files.json",Directory.GetFiles(output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    public static async Task Qualify(TowerFifthReturnRequest q,Func<TowerFifthReturnProof,CancellationToken,Task<(JsonElement State,JsonElement Eligibility)>> native,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=Plan(q);
        if(Path.Exists(q.Output)||HarnessJson.FileHash(Path.Combine(q.SourceArchive,"files.json"))!=SourcePin)throw new InvalidDataException("Pinned paid source and fresh output required.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var inventory=new TowerActivityInventory(q.ApiRoot,content);
        var bootstrap=TowerBootstrapCohorts.Read(Path.Combine(q.Fixtures,"tower-bootstrap.json"));
        var recipes=TowerBootstrapCohorts.Create(q.ApiRoot,q.Fixtures,bootstrap,content).Where(c=>c.Gear=="common"&&c.EssenceLevel==1).ToDictionary(c=>c.Recipe);
        var catalog=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(q.ApiRoot,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        var source=TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,"result.json"));var rows=new List<object>();Directory.CreateDirectory(q.Output);
        foreach(var row in source.GetProperty("servers").EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();var key=row.GetProperty("key").GetString()!;var file=key+"--continuation.json.gz";
            var saved=TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,file));var owners=saved.GetProperty("owners").Deserialize<TowerReturnOwner[]>(HarnessJson.Options)!;
            var party=saved.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;var previous=saved.GetProperty("server");
            var history=saved.GetProperty("historicalAttempts").Deserialize<JsonElement[]>(HarnessJson.Options)!;
            var population=Array.FindIndex(history,a=>a.GetProperty("floor").GetInt32()==5);
            if(population<=0||history.Length-population!=4||history.Skip(population).Any(a=>a.GetProperty("floor").GetInt32()!=5||a.GetProperty("battle").GetProperty("succeeded").GetBoolean())
                ||saved.GetProperty("nextPersonalRefreshBefore").GetInt32()!=history.Length||party.Floor.FloorNumber!=5||owners.Length!=16||party.Members.Count!=10
                ||owners.Any(o=>TowerReturnStudy.At(o.Runtime)!=party.StartsAt||o.Point.Character.Level!=40||o.Runtime.Growth.OwnedEssences.Length!=5))
                throw new InvalidDataException("Lost paid state, prior floor-5 failures or refresh boundary.");
            if(previous.GetProperty("floors")[4].GetProperty("isCleared").GetBoolean()||previous.GetProperty("floors")[4].GetProperty("unlockedAt").ValueKind==JsonValueKind.Null)
                throw new InvalidDataException("Floor five unavailable.");
            var gates=new List<JsonElement>();
            foreach(var o in owners)
            {
                TowerEarnedPartyStudy.ValidateInventory(o.Point.Character,o.Point.Owned,inventory);
                var j=TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,o.Origin,o.Runtime);
                TowerRuntimeCopy.Equal(o.Point.Character,j.Growth.Snapshot(o.Point.Character),"retained five-Essence growth");
                gates.Add(await TowerEssenceEligibility.Inspect(q.ApiRoot,content,o,ct));
            }
            foreach(var m in party.Members)TowerRuntimeCopy.Equal(m,owners.Single(o=>o.Point.Character.Id==m.Character.Id).Point,"latest party owner");
            var supplied=party with {Id=party.Id+"--supplied",Members=party.Members.Select(m=> {
                var owned=m.Owned.Concat(TowerContinuationSupply.Targets(content,catalog,"item.tower_supply.v1.floor_04",m.Character.Id,recipes[m.Recipe],bootstrap.PurchaseOrder)).ToArray();
                return m with {Owned=owned,Character=m.Character with {Equipment=inventory.Select(owned)}};
            }).ToArray()};
            var preparations=new Dictionary<string,JsonElement>();
            foreach(var (arm,p) in new[] {("actual",party),("supplied",supplied)})
            {
                var actors=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,p,0,ct));
                if(arm=="actual")TowerRuntimeCopy.Equal(saved.GetProperty("prepared"),actors,"paid source preparation parity");
                preparations.Add(arm,JsonSerializer.SerializeToElement(new {prepared=actors,hash=HarnessJson.Hash(actors)},HarnessJson.Options));
            }
            var path=int.Parse(key.Split("--",StringSplitOptions.None)[^1],System.Globalization.CultureInfo.InvariantCulture);
            var continuation=new TowerReturnProof(key,path,previous,history,owners,party,party,supplied,preparations,default);
            var proof=new TowerFifthReturnProof(file,HarnessJson.FileHash(Path.Combine(q.SourceArchive,file)),saved.GetProperty("personalRefreshBefore").GetInt32(),
                population,history.Length,continuation,default,gates.ToArray());
            var (state,eligibility)=await native(proof,ct);proof=proof with {Continuation=continuation with {InitialState=state},Eligibility=eligibility};
            TowerReturnStudy.Save(q.Output,key+"--preparation.json.gz",proof);
            rows.Add(new {key,participants=10,owners=16,party.StartsAt,personalRefreshBefore=proof.PersonalRefreshBefore,populationRefreshBefore=population,nextPersonalRefreshBefore=history.Length});
        }
        if(rows.Count!=15)throw new InvalidDataException("Changed eligible population.");
        TowerEarnedPartyStudy.Verify(q.InputHashes);TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="EarnedFifthReturnPrepared",plan,prepared=rows,
            held=source.GetProperty("held"),newFights=0,newSeeds=0,measuredPlayerSamples=0,newEssencesGranted=0});Manifest(q.Output);
    }
    public static async Task Run(TowerFifthReturnRequest q,Func<TowerFifthReturnProof,CancellationToken,Task<ITowerUnlockServer>> factory,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=Plan(q);
        if(Path.Exists(q.Output)||q.Qualification is null||HarnessJson.FileHash(Path.Combine(q.Qualification,"files.json"))!=q.QualificationPin
            ||q.Panels is null||q.Panels.Count!=15||q.Panels.Values.Any(s=>s.Length!=4)||q.Panels.Values.SelectMany(s=>s).Distinct().Count()!=60)
            throw new InvalidDataException("Audited expansion and 60 fresh seeds required.");
        var settings=TowerBundle.ReadSettings(q.ApiRoot);var content=OfflineContent.ForTower(q.ApiRoot,settings);Directory.CreateDirectory(q.Output);
        var rows=new List<object>();int fights=0,replays=0,attempts=0;
        foreach(var (key,seeds) in q.Panels.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            var proof=TowerUnlockStudy.Read(Path.Combine(q.Qualification,key+"--preparation.json.gz")).Deserialize<TowerFifthReturnProof>(HarnessJson.Options)!;
            var p=proof.Continuation;await using var server=await factory(proof,ct);TowerRuntimeCopy.Equal(p.InitialState,await server.State(ct),"expanded initial server");
            var now=p.Party.StartsAt;var steps=new List<object>();bool success=false;var history=p.HistoricalAttempts.ToList();
            var offset=history.Count(a=>a.GetProperty("floor").GetInt32()==5);
            for(var index=0;index<4;index++)
            {
                var seed=seeds[index];var battles=new Dictionary<string,JsonElement>();
                foreach(var arm in Arms)
                {
                    var party=TowerReturnStudy.Arm(p,arm) with {StartsAt=now};
                    async Task<JsonElement> Battle()
                    {
                        ct.ThrowIfCancellationRequested();if(++fights>150)throw new InvalidDataException("Expansion fight cap.");
                        var runtime=await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,party,seed,ct);
                        if(HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(runtime))!=p.Preparations[arm].GetProperty("hash").GetString())throw new InvalidDataException("Expanded preparation drift.");
                        var result=(await content.CreateExecutor().ExecuteTowerPlaybackAsync(runtime,settings.CheckpointIntervalTicks,ct)).Result;
                        var resolved=new CombatEncounterResultFactory().Create(runtime,result);var guardian=resolved.HostilePostState.Single();
                        return JsonSerializer.SerializeToElement(new {seed,succeeded=resolved.Outcome==BattleOutcome.Victory,
                            guardianHealthRemainingPercent=guardian.MaxHealth<=0?0:Math.Round(100m*guardian.Health/guardian.MaxHealth,2),summary=BattleSummary.From(resolved.CombatResult,6000)},HarnessJson.Options);
                    }
                    var battle=await Battle();battles.Add(arm,battle);
                    if(index==0){var replay=await Battle();replays++;TowerRuntimeCopy.Equal(battle,replay,"expansion replay");TowerReturnStudy.Save(q.Output,key+"--"+arm+"--replay.json",replay);}
                }
                var actual=battles["actual"];success=actual.GetProperty("succeeded").GetBoolean();attempts++;
                var receipt=await server.Apply(p.Party with {StartsAt=now},offset+index,seed,success,actual.GetProperty("summary").GetProperty("durationTicks").GetInt32(),ct);
                var file=key+$"--attempt-{offset+index}.json";TowerReturnStudy.Save(q.Output,file,new {key,floor=5,index,seed,battles,receipt});steps.Add(new {file,seed,success});
                history.Add(JsonSerializer.SerializeToElement(new {floor=5,attempt=offset+index,battle=actual,receipt},HarnessJson.Options));
                now=receipt.GetProperty("after").GetProperty("at").GetDateTimeOffset();if(success)break;
            }
            var final=await server.State(ct);var owners=p.Owners.Select(o=>o with {Runtime=TowerReturnStudy.Wait(o.Runtime,now)}).ToArray();
            TowerReturnStudy.Save(q.Output,key+"--continuation.json.gz",new {key,initial=p.InitialState,steps,final,owners,historicalAttempts=history,
                personalRefreshBoundaries=new[] {proof.PersonalRefreshBefore,proof.NextPersonalRefreshBefore},populationRefreshBefore=proof.PopulationRefreshBefore,
                party=p.Party with {StartsAt=now,Members=p.Party.Members.Select(m=>owners.Single(o=>o.Point.Character.Id==m.Character.Id).Point).ToArray()},stop=success?"FloorFiveCleared":"FourAttemptCap",highestCleared=success?5:4});
            rows.Add(new {key,floor=5,success,attempts=steps.Count,startedAt=p.Party.StartsAt,endedAt=now});
        }
        var held=TowerUnlockStudy.Read(Path.Combine(q.Qualification,"result.json")).GetProperty("held");
        TowerEarnedPartyStudy.Verify(q.InputHashes);TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="EarnedFifthReturnCombatComplete",plan,q.QualificationPin,
            journeys=rows,held,attempts,controls=attempts,replays,fights,reservedSeeds=60,newEssencesGranted=0,earnedNewEquipment=0,measuredPlayerSamples=0,searchPerformed=false});Manifest(q.Output);
    }
}
