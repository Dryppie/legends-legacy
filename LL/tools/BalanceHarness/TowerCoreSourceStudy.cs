using System.Text.Json;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences;

namespace BalanceHarness;

public sealed record TowerCoreSourceRequest(string ApiRoot,string Fixtures,string SourceArchive,string EntryArchive,
    string HistoryArchive,string WaveArchive,string MinesArchive,string ReturnArchive,string Output,IReadOnlyDictionary<string,string> InputHashes);
public sealed record TowerCoreRun(string Archive,string File,string Hash,Guid RunId,string Family,string Status,
    DateTimeOffset Started,DateTimeOffset Ended,double CombatSeconds,int SigilsPaid,bool FirstCompletion);
public sealed record TowerCoreProbability(int Cores,double Probability);

/// <summary>Source qualification, not a retroactive reward claim. Missing Random.Shared draws stay missing.</summary>
public static class TowerCoreSourceStudy
{
    public const string Version="tower-core-source-v1";
    public const string SourcePin="dd754b30a0ce0a26032fb0149d64984c2ef41b55f458212a0a4e2529d9adfd50";
    public const string EntryPin="4b0cbf561aa44dca4d9eaccc50dfac846399cf1c60f67560e9cf0bd30a37c315";
    public const string MinesPin="963fd340840d7b1f7d9f3e16eef9f57a97fec4445b322ef7d157e6adbd5f37c7";
    public static TowerCoreProbability[] Distribution(int successes,int firstFamilies)
    {
        if(successes<0||firstFamilies<0||firstFamilies>2||firstFamilies>successes)throw new ArgumentOutOfRangeException(nameof(successes));
        // Uniform integer 3..5, plus independent Bernoulli(0.25). Not uniform 3..6.
        var probabilities=new SortedDictionary<int,double>{{6*firstFamilies,1}};
        for(var n=0;n<successes;n++)
        {
            var next=new SortedDictionary<int,double>();
            foreach(var (cores,p) in probabilities)
            foreach(var (award,weight) in new[] {(3,3),(4,4),(5,4),(6,1)})
                next[cores+award]=next.GetValueOrDefault(cores+award)+p*weight/12d;
            probabilities=next;
        }
        return probabilities.Select(p=>new TowerCoreProbability(p.Key,p.Value)).ToArray();
    }
    public static int XpToFirstAscension(TowerGrowingEssence e)
    {
        if(e.AscensionTier!=0||e.Level is <1 or >10||e.CurrentXp<0
            ||e.CurrentXp>=(e.Level==10?1:EssenceProgressionConstants.GetXpRequiredForLevel(e.Level)))
            throw new InvalidDataException("Invalid unascended training state.");
        return Enumerable.Range(e.Level,10-e.Level).Sum(EssenceProgressionConstants.GetXpRequiredForLevel)-e.CurrentXp;
    }
    public static void ValidateRuns(TowerReturnOwner owner,IReadOnlyList<TowerCoreRun> runs)
    {
        if(runs.Select(r=>r.RunId).Distinct().Count()!=runs.Count)throw new InvalidDataException("Duplicate completion source.");
        var first=new HashSet<string>();DateTimeOffset prior=TowerJourneyProgression.Epoch;
        foreach(var r in runs)
        {
            var succeeded=r.Status=="Completed";
            if(r.Family is not ("goblin_mines" or "forgotten_catacombs")||r.Status is not ("Completed" or "Failed")
                ||r.SigilsPaid!=1||r.Started<prior||r.Ended<r.Started||r.Ended>TowerReturnStudy.At(owner.Runtime)
                ||r.CombatSeconds<=0||r.Hash.Length!=64||r.FirstCompletion!=(succeeded&&first.Add(r.Family)))
                throw new InvalidDataException("Invalid paid chronological core source.");
            prior=r.Ended;
        }
        foreach(var family in new[] {"goblin_mines","forgotten_catacombs"})
        {
            var saved=owner.Runtime.Mastery.SingleOrDefault(m=>m.DungeonDefinitionId==family);
            if((saved?.CompletionCount??0)!=runs.Count(r=>r.Family==family&&r.Status=="Completed")
                ||(saved is not null&&saved.CharacterId!=owner.Point.Character.Id))
                throw new InvalidDataException("Core history disagrees with retained mastery.");
        }
    }
    public static TowerCoreRun[] History(TowerCoreSourceRequest q,string server,TowerReturnOwner owner,
        Func<string,JsonElement> read)
    {
        var rows=new List<TowerCoreRun>();var seen=new HashSet<string>();
        void Add(string archive,string file,Guid ownerId,string family,JsonElement receipt,int paid,Guid runId)
        {
            var path=Path.Combine(archive,file);var run=read(path).Deserialize<DungeonAcquisitionRun>(HarnessJson.Options)!;
            bool success=run.Status==DungeonRunStatus.Completed;
            if(ownerId!=owner.Point.Character.Id||run.Character!=owner.Point.Character.Name||run.Dungeon!=family
                ||run.SigilsConsumed!=paid||paid!=1||run.EntryItems.Count!=1||run.EntryItems.GetValueOrDefault("sigil_"+family)!=1
                ||run.CompletionCallbacks!=(success?1:0)||receipt.GetProperty("appliedExperience").GetInt32()!=(success?receipt.GetProperty("pendingExperience").GetInt32():0))
                throw new InvalidDataException("Unpaid/foreign run or invalid success claim.");
            var reconstructed=TowerGrowthStudy.Reconstruct(ownerId,run);
            if(reconstructed.Id!=runId)throw new InvalidDataException("Changed source run identity.");
            rows.Add(new(Path.GetFileName(archive),file,HarnessJson.FileHash(path),runId,family,run.Status.ToString(),
                receipt.GetProperty("started").GetDateTimeOffset(),receipt.GetProperty("ended").GetDateTimeOffset(),run.CombatSeconds,paid,success&&seen.Add(family)));
        }
        var proof=read(Path.Combine(q.EntryArchive,owner.History+"--checkpoint.json"));var cp=proof.GetProperty("checkpoint");
        if(cp.GetProperty("point").GetProperty("history").GetString()!=owner.History)throw new InvalidDataException("Wrong personal checkpoint.");
        foreach(var s in cp.GetProperty("steps").EnumerateArray())
        {
            var e=cp.GetProperty("entries").EnumerateArray().Single(e=>e.GetProperty("ordinal").GetInt32()==s.GetProperty("ordinal").GetInt32());
            Add(q.HistoryArchive,s.GetProperty("file").GetString()!,s.GetProperty("before").GetProperty("id").GetGuid(),s.GetProperty("dungeon").GetString()!,
                e.GetProperty("receipt"),s.GetProperty("sigilsBefore").GetInt32()-s.GetProperty("sigilsAfter").GetInt32(),s.GetProperty("dungeonLoot").GetProperty("runId").GetGuid());
        }
        foreach(var archive in new[] {q.WaveArchive,q.MinesArchive})
        foreach(var row in read(Path.Combine(archive,"result.json")).GetProperty("results").EnumerateArray()
            .Where(r=>r.GetProperty("server").GetString()==server&&r.GetProperty("history").GetString()==owner.History))
        {
            if(!row.TryGetProperty("progressFile",out var f))continue;
            var p=read(Path.Combine(archive,f.GetString()!));var family=p.GetProperty("native").GetProperty("selectedDungeon").GetString()!;
            var costs=p.GetProperty("costs").Deserialize<Dictionary<string,int>>(HarnessJson.Options)!;
            if(costs.Count!=1||costs.GetValueOrDefault("sigil_"+family)!=1)throw new InvalidDataException("Invalid wave payment.");
            Add(archive,p.GetProperty("runFile").GetString()!,p.GetProperty("entry").GetProperty("id").GetGuid(),family,p.GetProperty("receipt"),1,p.GetProperty("ordinary").GetProperty("runId").GetGuid());
        }
        ValidateRuns(owner,rows);return rows.ToArray();
    }
    public static async Task Run(TowerCoreSourceRequest q,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        var plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-core-source.json"));
        foreach(var (archive,pin) in new[] {(q.SourceArchive,SourcePin),(q.EntryArchive,EntryPin),(q.HistoryArchive,TowerEarnedPartyStudy.ArchivePin),
            (q.WaveArchive,TowerFifthEssenceStudy.WavePin),(q.MinesArchive,MinesPin)})
            if(HarnessJson.FileHash(Path.Combine(archive,"files.json"))!=pin)throw new InvalidDataException("Changed core source archive.");
        if(plan.GetProperty("version").GetString()!=Version||Path.Exists(q.Output))throw new InvalidDataException("Fresh core qualification required.");
        var cache=new Dictionary<string,JsonElement>();JsonElement Read(string file){if(!cache.TryGetValue(file,out var value))cache[file]=value=TowerUnlockStudy.Read(file);return value;}
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));Directory.CreateDirectory(q.Output);
        var completions=await TowerCoreSourceProbe.Completions(q.ApiRoot,ct);TowerReturnStudy.Save(q.Output,"native-completion-probes.json",completions);
        var rows=new List<object>();int ownersCount=0,attempts=0,successes=0,guaranteed=0,possible=0;
        var source=Read(Path.Combine(q.SourceArchive,"result.json"));
        foreach(var row in source.GetProperty("journeys").EnumerateArray())
        {
            var key=row.GetProperty("key").GetString()!;var file=key+"--continuation.json.gz";var old=Read(Path.Combine(q.SourceArchive,file));
            var owners=old.GetProperty("owners").Deserialize<TowerReturnOwner[]>(HarnessJson.Options)!;var qualified=new List<object>();
            foreach(var owner in owners)
            {
                ct.ThrowIfCancellationRequested();TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,owner.Origin,owner.Runtime);
                if(owner.Runtime.Sources.State.Items.GetValueOrDefault(EssenceProgressionConstants.LesserMonsterCoreItemId)!=0)
                    throw new InvalidDataException("Already credited core channel needs a different reconciliation.");
                var runs=History(q,key,owner,Read);var n=runs.Count(r=>r.Status=="Completed");var f=runs.Count(r=>r.FirstCompletion);
                var distribution=Distribution(n,f);var minimum=distribution.First().Cores;var maximum=distribution.Last().Cores;
                var cost=owner.Runtime.Growth.OwnedEssences.Length*EssenceProgressionConstants.GetAscensionCost(1).Amount;
                var native=await TowerCoreSourceProbe.Ascension(q.ApiRoot,content,owner,ct);
                qualified.Add(new {owner=owner.Point.Character.Id,owner.History,runs,successes=n,failures=runs.Length-n,firstFamilies=f,
                    projection=new {minimum,maximum,mean=4.25*n+6*f,distribution,allFiveCoreCost=cost,
                        probabilityAllFive=distribution.Where(p=>p.Cores>=cost).Sum(p=>p.Probability),
                        minimumAffordable=Math.Min(5,minimum/6),maximumAffordable=Math.Min(5,maximum/6),
                        minimumAdditionalRepeatSuccesses=Math.Max(0,(cost-maximum+5)/6),guaranteedAdditionalRepeatSuccesses=Math.Max(0,(cost-minimum+2)/3)},native});
                ownersCount++;attempts+=runs.Length;successes+=n;if(minimum>=cost)guaranteed++;if(maximum>=cost)possible++;
            }
            var party=old.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
            var prepared=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,party,0,ct));
            TowerReturnStudy.Save(q.Output,key+"--sources.json.gz",new {key,sourceFile=file,sourceHash=HarnessJson.FileHash(Path.Combine(q.SourceArchive,file)),
                retained=old,qualified,prepared});rows.Add(new {key,owners=owners.Length});
        }
        if(ownersCount!=240||rows.Count!=15)throw new InvalidDataException("Incomplete core source population.");
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="CoreSourcesQualifiedNotCredited",plan,journeys=rows,owners=ownersCount,
            paidAttempts=attempts,successfulCompletions=successes,failedAttempts=attempts-successes,guaranteedCoreBudgets=guaranteed,possibleCoreBudgets=possible,
            held=source.GetProperty("held"),newResourcesCredited=0,newExperience=0,newAscensions=0,newFights=0,newSeeds=0,measuredPlayerSamples=0,searchPerformed=false});
        TowerReturnStudy.Save(q.Output,"files.json",Directory.GetFiles(q.Output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    }
}
