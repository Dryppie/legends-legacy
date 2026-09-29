using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerExpansionRequest(string ApiRoot,string Fixtures,string ReturnArchive,string PreparationArchive,string Output,
    IReadOnlyDictionary<string,string> InputHashes,string? Qualification=null,string? QualificationPin=null,IReadOnlyDictionary<string,int[]>? Panels=null);
public sealed record TowerExpansionProof(string SourceFile,string SourceHash,string PreparationFile,string PreparationHash,int PersonalRefreshBefore,
    TowerReturnProof Continuation,JsonElement Eligibility,JsonElement[] Essences);

public static class TowerExpansionStudy
{
    public const string Version="tower-earned-expansion-v1";
    public const string ReturnPin="9b9cff072ac42afe75c17485015bc792f34bae5580f5cf789e40bf3fafc2e6be";
    public const string PreparationPin="4a775ba52a633e9571d5429712575c9f9aa9f9f8003025d7ca88ea9e7ec315b7";
    public static readonly string[] Arms=["actual","supplied"];
    private static JsonElement Plan(TowerExpansionRequest q)
    {
        var p=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-expansion.json"));
        if(p.GetProperty("version").GetString()!=Version||p.GetProperty("eligibleServers").GetInt32()!=15||p.GetProperty("targetFloor").GetInt32()!=5
            ||p.GetProperty("partySize").GetInt32()!=10||p.GetProperty("attemptsPerServer").GetInt32()!=4||p.GetProperty("maximumFights").GetInt32()!=150)
            throw new InvalidDataException("Changed expansion envelope.");
        return p;
    }
    private static void Manifest(string output)=>TowerReturnStudy.Save(output,"files.json",Directory.GetFiles(output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    public static TowerEarnedPoint[] Roster(TowerEarnedPlan plan,TowerReturnOwner[] owners,int rotation,int count)
    {
        var members=plan.Roster.Take(count).Select(s=>owners.Single(o=>o.Point.Recipe==s.Recipe&&o.Point.Path==(rotation+s.PathOffset)%4).Point).ToArray();
        if(members.Length!=count||members.Select(m=>m.Character.Id).Distinct().Count()!=count)throw new InvalidDataException("Cannot clone an expanded party owner.");
        return members;
    }
    public static async Task Qualify(TowerExpansionRequest q,Func<TowerExpansionProof,CancellationToken,Task<(JsonElement State,JsonElement Eligibility)>> native,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=Plan(q);
        if(Path.Exists(q.Output)||HarnessJson.FileHash(Path.Combine(q.ReturnArchive,"files.json"))!=ReturnPin
            ||HarnessJson.FileHash(Path.Combine(q.PreparationArchive,"files.json"))!=PreparationPin)throw new InvalidDataException("Pinned return history and fresh output required.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var inventory=new TowerActivityInventory(q.ApiRoot,content);
        var floors=TowerContentProviders.Floors(Path.Combine(q.ApiRoot,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var roster=HarnessJson.Read<TowerEarnedPlan>(Path.Combine(q.Fixtures,"tower-earned-party.json"));
        var bootstrap=TowerBootstrapCohorts.Read(Path.Combine(q.Fixtures,"tower-bootstrap.json"));
        var recipes=TowerBootstrapCohorts.Create(q.ApiRoot,q.Fixtures,bootstrap,content).Where(c=>c.Gear=="common"&&c.EssenceLevel==1).ToDictionary(c=>c.Recipe);
        var catalog=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(q.ApiRoot,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        Directory.CreateDirectory(q.Output);var prepared=new List<object>();var held=new List<object>();
        foreach(var row in TowerUnlockStudy.Read(Path.Combine(q.ReturnArchive,"result.json")).GetProperty("journeys").EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();var key=row.GetProperty("key").GetString()!;var file=key+"--continuation.json.gz";
            var current=TowerUnlockStudy.Read(Path.Combine(q.ReturnArchive,file));
            if(current.GetProperty("highestCleared").GetInt32()!=4){held.Add(new {key,file,hash=HarnessJson.FileHash(Path.Combine(q.ReturnArchive,file)),newAttempts=0});continue;}
            var prepFile=key+"--preparation.json.gz";var old=TowerUnlockStudy.Read(Path.Combine(q.PreparationArchive,prepFile)).Deserialize<TowerReturnProof>(HarnessJson.Options)!;
            var owners=current.GetProperty("owners").Deserialize<TowerReturnOwner[]>(HarnessJson.Options)!;
            var final=current.GetProperty("final");var at=final.GetProperty("at").GetDateTimeOffset();
            if(owners.Length!=16||owners.Any(o=>TowerReturnStudy.At(o.Runtime)!=at))throw new InvalidDataException("Lost personal state or clock.");
            foreach(var o in owners)
            {
                TowerEarnedPartyStudy.ValidateInventory(o.Point.Character,o.Point.Owned,inventory);
                var journey=TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,o.Origin,o.Runtime);
                TowerRuntimeCopy.Equal(o.Point.Character,journey.Growth.Snapshot(o.Point.Character),"expanded personal growth");
            }
            var members=Roster(roster,owners,old.Party.Rotation,10);
            if(!members.Take(5).Select(m=>m.Character.Id).SequenceEqual(old.Party.Members.Select(m=>m.Character.Id)))throw new InvalidDataException("Original seats moved.");
            var party=old.Party with {Id=key+"--floor-5",Floor=floors.GetFloor(5)!,StartsAt=at,Members=members};
            if(final.GetProperty("floors")[4].GetProperty("unlockedAt").ValueKind==JsonValueKind.Null||final.GetProperty("floors")[4].GetProperty("isCleared").GetBoolean())
                throw new InvalidDataException("Floor 5 not legally available.");
            var supplied=party with {Id=party.Id+"--supplied",Members=members.Select(m=> {
                var owned=m.Owned.Concat(TowerContinuationSupply.Targets(content,catalog,"item.tower_supply.v1.floor_04",m.Character.Id,recipes[m.Recipe],bootstrap.PurchaseOrder)).ToArray();
                return m with {Owned=owned,Character=m.Character with {Equipment=inventory.Select(owned)}};
            }).ToArray()};
            var history=old.HistoricalAttempts.ToList();
            foreach(var s in current.GetProperty("steps").EnumerateArray())
            {
                var a=TowerUnlockStudy.Read(Path.Combine(q.ReturnArchive,s.GetProperty("file").GetString()!));
                history.Add(JsonSerializer.SerializeToElement(new {floor=a.GetProperty("floor").GetInt32(),attempt=a.GetProperty("attempt").GetInt32(),
                    battle=a.GetProperty("battles").GetProperty("actual"),receipt=a.GetProperty("receipt")},HarnessJson.Options));
            }
            var preparations=new Dictionary<string,JsonElement>();
            foreach(var (arm,p) in new[] {("actual",party),("supplied",supplied)})
            {
                var actors=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,p,0,ct));
                preparations.Add(arm,JsonSerializer.SerializeToElement(new {prepared=actors,hash=HarnessJson.Hash(actors)},HarnessJson.Options));
            }
            var continuation=new TowerReturnProof(key,old.Path,final,history.ToArray(),owners,party,party,supplied,preparations,default);
            var gates=new List<JsonElement>();foreach(var o in owners)gates.Add(await TowerEssenceEligibility.Inspect(q.ApiRoot,content,o,ct));
            var proof=new TowerExpansionProof(file,HarnessJson.FileHash(Path.Combine(q.ReturnArchive,file)),prepFile,HarnessJson.FileHash(Path.Combine(q.PreparationArchive,prepFile)),
                old.HistoricalAttempts.Length,continuation,default,gates.ToArray());
            var (state,eligibility)=await native(proof,ct);proof=proof with {Continuation=continuation with {InitialState=state},Eligibility=eligibility};
            TowerReturnStudy.Save(q.Output,key+"--expansion.json.gz",proof);
            prepared.Add(new {key,participants=10,newParticipants=5,owners=16,party.StartsAt,levelMin=members.Min(m=>m.Character.Level),levelMax=members.Max(m=>m.Character.Level)});
        }
        if(prepared.Count!=15||held.Count!=17)throw new InvalidDataException("Changed eligible server population.");
        TowerReturnStudy.Save(q.Output,"essence-source-rules.json",TowerEssenceEligibility.SourceRules(q.ApiRoot,content));
        TowerEarnedPartyStudy.Verify(q.InputHashes);TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="EarnedExpansionPrepared",plan,prepared,held,
            newFights=0,newSeeds=0,measuredPlayerSamples=0,newEssencesGranted=0});Manifest(q.Output);
    }
    public static async Task Run(TowerExpansionRequest q,Func<TowerExpansionProof,CancellationToken,Task<ITowerUnlockServer>> factory,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=Plan(q);
        if(Path.Exists(q.Output)||q.Qualification is null||HarnessJson.FileHash(Path.Combine(q.Qualification,"files.json"))!=q.QualificationPin
            ||q.Panels is null||q.Panels.Count!=15||q.Panels.Values.Any(s=>s.Length!=4)||q.Panels.Values.SelectMany(s=>s).Distinct().Count()!=60)
            throw new InvalidDataException("Audited expansion and 60 fresh seeds required.");
        var settings=TowerBundle.ReadSettings(q.ApiRoot);var content=OfflineContent.ForTower(q.ApiRoot,settings);Directory.CreateDirectory(q.Output);
        var rows=new List<object>();int fights=0,replays=0,attempts=0;
        foreach(var (key,seeds) in q.Panels.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            var proof=TowerUnlockStudy.Read(Path.Combine(q.Qualification,key+"--expansion.json.gz")).Deserialize<TowerExpansionProof>(HarnessJson.Options)!;
            var p=proof.Continuation;await using var server=await factory(proof,ct);TowerRuntimeCopy.Equal(p.InitialState,await server.State(ct),"expanded initial server");
            var now=p.Party.StartsAt;var steps=new List<object>();bool success=false;
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
                var receipt=await server.Apply(p.Party with {StartsAt=now},index,seed,success,actual.GetProperty("summary").GetProperty("durationTicks").GetInt32(),ct);
                var file=key+$"--attempt-{index}.json";TowerReturnStudy.Save(q.Output,file,new {key,floor=5,index,seed,battles,receipt});steps.Add(new {file,seed,success});
                now=receipt.GetProperty("after").GetProperty("at").GetDateTimeOffset();if(success)break;
            }
            var final=await server.State(ct);var owners=p.Owners.Select(o=>o with {Runtime=TowerReturnStudy.Wait(o.Runtime,now)}).ToArray();
            TowerReturnStudy.Save(q.Output,key+"--continuation.json.gz",new {key,initial=p.InitialState,steps,final,owners,stop=success?"FloorFiveCleared":"FourAttemptCap",highestCleared=success?5:4});
            rows.Add(new {key,floor=5,success,attempts=steps.Count,startedAt=p.Party.StartsAt,endedAt=now});
        }
        var held=TowerUnlockStudy.Read(Path.Combine(q.Qualification,"result.json")).GetProperty("held");
        TowerEarnedPartyStudy.Verify(q.InputHashes);TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="EarnedExpansionCombatComplete",plan,q.QualificationPin,
            journeys=rows,held,attempts,controls=attempts,replays,fights,reservedSeeds=60,newEssencesGranted=0,earnedNewEquipment=0,measuredPlayerSamples=0,searchPerformed=false});Manifest(q.Output);
    }
}
