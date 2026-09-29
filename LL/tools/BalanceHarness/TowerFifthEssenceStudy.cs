using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerFifthRequest(string ApiRoot,string Fixtures,string GrowthArchive,string HistoryArchive,
    string WaveArchive,string ReturnArchive,string Output,IReadOnlyDictionary<string,string> InputHashes);
public sealed record TowerQuestCredit(Guid Owner,string History,int QuestEncounter,DateTimeOffset ActivatedAt,
    string HistoryHash,string? RunFile,string? RunHash,DateTimeOffset? MinesCompletedAt,DateTimeOffset[] ForestWins,
    DateTimeOffset ClaimAt,string InitialStateAssumption);
public sealed record TowerFifthResult(TowerReturnOwner Owner,TowerQuestCredit Credit,JsonElement Quest,
    TowerEssenceAcquisition? Acquisition,JsonElement Native);

/// <summary>Reconstruct only pinned past quest events; retain a real native claim/open/absorption at the current clock.</summary>
public static class TowerFifthEssenceStudy
{
    public const string Version="tower-retained-fifth-essence-v1";
    public const string GrowthPin="aabe1e35cfc93c8ea4e4f290b531a8d2975da379a0bd0f37e7701afc1f67736f";
    public const string WavePin="c389b202dd311c78d2e86ca52b06c5607a0e179fc70c2d9efe0be430fd6d8142";
    public static TowerQuestCredit Credit(TowerFifthRequest q,string server,TowerReturnOwner owner,TowerLevel40Growth growth)
    {
        var path=Path.Combine(q.HistoryArchive,owner.History+"--history.json");var h=TowerUnlockStudy.Read(path);var summary=h.GetProperty("summary");
        var gate=summary.GetProperty("questAt").GetInt32();var activated=TowerJourneyProgression.Epoch.AddSeconds(gate*10L);
        var forestStart=TowerReturnStudy.At(growth.Owner.Runtime).AddSeconds(-(growth.Until-growth.From)*10L);
        if(gate<=0||gate>growth.From||summary.GetProperty("policy").GetString()!="earned-progression"
            ||growth.Owner.History!=owner.History||growth.Victories<6||forestStart<=activated)
            throw new InvalidDataException("Unqualified prerequisite/forest prefix.");
        string? runFile=null,runHash=null;DateTimeOffset? completed=null;
        foreach(var step in h.GetProperty("steps").EnumerateArray())
        {
            if(step.GetProperty("encounter").GetInt32()>growth.From||step.GetProperty("dungeon").GetString()!="goblin_mines"
                ||step.GetProperty("status").GetString()!="Completed")continue;
            var entry=h.GetProperty("progression").GetProperty("entries").EnumerateArray().Single(e=>e.GetProperty("ordinal").GetInt32()==step.GetProperty("ordinal").GetInt32());
            var receipt=entry.GetProperty("receipt");var started=receipt.GetProperty("started").GetDateTimeOffset();var ended=receipt.GetProperty("ended").GetDateTimeOffset();
            var file=step.GetProperty("file").GetString()!;var run=TowerUnlockStudy.Read(Path.Combine(q.HistoryArchive,file));
            if(started<activated||ended>forestStart||ended<started||step.GetProperty("before").GetProperty("id").GetGuid()!=owner.Point.Character.Id
                ||step.GetProperty("sigilsBefore").GetInt32()-step.GetProperty("sigilsAfter").GetInt32()!=1
                ||run.GetProperty("status").GetString()!="Completed"||run.GetProperty("dungeon").GetString()!="goblin_mines"
                ||run.GetProperty("completionCallbacks").GetInt32()!=1||run.GetProperty("sigilsConsumed").GetInt32()!=1
                ||run.GetProperty("entryItems").GetProperty("sigil_goblin_mines").GetInt32()!=1||receipt.GetProperty("appliedExperience").GetInt32()<=0)
                throw new InvalidDataException("Unpaid, foreign or future Mines completion.");
            runFile=file;runHash=HarnessJson.FileHash(Path.Combine(q.HistoryArchive,file));completed=ended;break;
        }
        // The entire later wave is checked before classifying a source as missing. A different future archive needs its own qualification.
        var wave=TowerUnlockStudy.Read(Path.Combine(q.WaveArchive,"result.json")).GetProperty("results").EnumerateArray()
            .Single(r=>r.GetProperty("server").GetString()==server&&r.GetProperty("history").GetString()==owner.History);
        if(completed is null&&wave.GetProperty("actualEntries").GetInt32()>0&&wave.GetProperty("status").GetString()=="Completed")
        {
            var progress=TowerUnlockStudy.Read(Path.Combine(q.WaveArchive,wave.GetProperty("progressFile").GetString()!));
            if(progress.GetProperty("native").GetProperty("selectedDungeon").GetString()=="goblin_mines")
                throw new InvalidDataException("Additional wave credit needs explicit chronological qualification.");
        }
        var wins=Enumerable.Range(growth.From+1,growth.Until-growth.From).Where(n=>TowerActivityInventory.Victory(owner.Point.Outcome,n)).Take(6)
            .Select(n=>forestStart.AddSeconds((n-growth.From-1)*10L)).ToArray();
        return new(owner.Point.Character.Id,owner.History,gate,activated,HarnessJson.FileHash(path),runFile,runHash,completed,wins,
            TowerReturnStudy.At(owner.Runtime),"Roots Remember activates at the retained Between Day and Night gate and is unclaimed; historical quest inventory/status was not recorded. Claim is delayed until this level-40 interaction; no earlier fifth training is credited.");
    }

    public static async Task Run(TowerFifthRequest q,Func<TowerReturnOwner,TowerQuestCredit,string,CancellationToken,Task<TowerFifthResult>> retain,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);var plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-fifth-essence.json"));
        if(plan.GetProperty("version").GetString()!=Version||Path.Exists(q.Output)
            ||HarnessJson.FileHash(Path.Combine(q.GrowthArchive,"files.json"))!=GrowthPin
            ||HarnessJson.FileHash(Path.Combine(q.HistoryArchive,"files.json"))!=TowerEarnedPartyStudy.ArchivePin
            ||HarnessJson.FileHash(Path.Combine(q.WaveArchive,"files.json"))!=WavePin)throw new InvalidDataException("Changed fifth-source contract.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var rows=new List<object>();int grants=0,held=0;
        Directory.CreateDirectory(q.Output);
        foreach(var row in TowerUnlockStudy.Read(Path.Combine(q.GrowthArchive,"result.json")).GetProperty("journeys").EnumerateArray())
        {
            var key=row.GetProperty("key").GetString()!;var file=key+"--growth.json.gz";var old=TowerUnlockStudy.Read(Path.Combine(q.GrowthArchive,file));
            var owners=old.GetProperty("owners").Deserialize<TowerReturnOwner[]>(HarnessJson.Options)!;
            var growth=old.GetProperty("growth").Deserialize<TowerLevel40Growth[]>(HarnessJson.Options)!;var acquired=new List<TowerFifthResult>();
            foreach(var owner in owners)
            {
                ct.ThrowIfCancellationRequested();var credit=Credit(q,key,owner,growth.Single(g=>g.Owner.History==owner.History));
                var option=plan.GetProperty("choices").GetProperty(owner.Point.Recipe).GetString()!;
                var result=await retain(owner,credit,option,ct);acquired.Add(result);
                if(result.Acquisition is null)held++;else grants++;
                TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,result.Owner.Origin,result.Owner.Runtime);
            }
            var after=acquired.Select(r=>r.Owner).ToArray();var byId=after.ToDictionary(o=>o.Point.Character.Id);
            var party=old.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;
            party=party with {Members=party.Members.Select(m=>byId[m.Character.Id].Point).ToArray()};
            var prepared=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,party,0,ct));
            TowerReturnStudy.Save(q.Output,key+"--acquisition.json.gz",new {key,sourceFile=file,sourceHash=HarnessJson.FileHash(Path.Combine(q.GrowthArchive,file)),
                server=old.GetProperty("server"),historicalAttempts=old.GetProperty("historicalAttempts"),personalRefreshBefore=old.GetProperty("personalRefreshBefore"),
                nextPersonalRefreshBefore=old.GetProperty("nextPersonalRefreshBefore"),acquired,owners=after,party,prepared});
            rows.Add(new {key,granted=acquired.Count(r=>r.Acquisition is not null),pending=acquired.Count(r=>r.Acquisition is null),
                fifthInParty=party.Members.Count(m=>m.Character.Essences.Count==5)});
        }
        if(grants+held!=240||rows.Count!=15)throw new InvalidDataException("Incomplete fifth-source population.");
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="RetainedFifthEssencesQualified",plan,journeys=rows,grants,pending=held,
            held=TowerUnlockStudy.Read(Path.Combine(q.GrowthArchive,"result.json")).GetProperty("held"),newFights=0,newSeeds=0,newIdleEncounters=0,
            measuredPlayerSamples=0,searchPerformed=false,combatAdmission=false});
        TowerReturnStudy.Save(q.Output,"files.json",Directory.GetFiles(q.Output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    }
}
