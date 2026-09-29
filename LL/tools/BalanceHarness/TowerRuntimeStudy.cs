using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerRuntimeRequest(string ApiRoot,string Fixtures,string GrowthArchive,string EntryArchive,string Output,
    IReadOnlyDictionary<string,string> InputHashes);

public static class TowerRuntimeStudy
{
    public const string Version="tower-continuation-runtime-v1";
    public const string EntryPin="4b0cbf561aa44dca4d9eaccc50dfac846399cf1c60f67560e9cf0bd30a37c315";
    public static async Task Run(TowerRuntimeRequest request,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(request.InputHashes);
        if(HarnessJson.FileHash(Path.Combine(request.EntryArchive,"files.json"))!=EntryPin
            ||HarnessJson.FileHash(Path.Combine(request.GrowthArchive,"files.json"))!=TowerUpgradeEntryStudy.GrowthPin
            ||Path.Exists(request.Output))throw new InvalidDataException("Pinned sources and fresh output required.");
        var plan=HarnessJson.Read<JsonElement>(Path.Combine(request.Fixtures,"tower-continuation-runtime.json"));
        if(plan.GetProperty("version").GetString()!=Version)throw new InvalidDataException("Changed runtime plan.");
        var content=OfflineContent.ForTower(request.ApiRoot,TowerBundle.ReadSettings(request.ApiRoot));
        var predecessor=HarnessJson.Read<JsonElement>(Path.Combine(request.EntryArchive,"result.json"));
        var summaries=new List<object>();var runtimes=new Dictionary<string,(TowerUpgradeCheckpoint Checkpoint,FixtureCharacter Origin,TowerJourneyRuntime Runtime)>();
        Directory.CreateDirectory(request.Output);long bytes=0;var guards=0;
        void Save(string file,object value)
        {
            var data=JsonSerializer.SerializeToUtf8Bytes(value,HarnessJson.Options);bytes+=data.Length;
            if(bytes>256*1048576L)throw new InvalidDataException("Runtime output bound exceeded.");
            using var f=new FileStream(Path.Combine(request.Output,file),FileMode.CreateNew);f.Write(data);
        }
        foreach(var row in predecessor.GetProperty("personal").EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();var filename=row.GetProperty("file").GetString()!;
            var proof=HarnessJson.Read<JsonElement>(Path.Combine(request.EntryArchive,filename));
            var archived=proof.GetProperty("checkpoint").Deserialize<TowerUpgradeCheckpoint>(HarnessJson.Options)!;
            var history=HarnessJson.Read<JsonElement>(Path.Combine(request.GrowthArchive,archived.Point.History+"--history.json"));
            var cp=TowerUpgradeEntryStudy.Restore(archived.Point,history);
            var excludedBoundaryOffers=archived.Days.Where(d=>d.GetProperty("at").GetDateTimeOffset()==cp.Point.AvailableAt).ToArray();
            TowerRuntimeCopy.Equal(archived with {Days=archived.Days.Except(excludedBoundaryOffers).ToArray()},cp,"only future boundary offers removed");
            var origin=TowerJourneyReplay.Origin(request.ApiRoot,request.Fixtures,content,cp.Point);
            var live=await TowerJourneyReplay.Restore(request.ApiRoot,request.Fixtures,request.GrowthArchive,content,cp,ct);
            var replayRuntime=live.ExportRuntime();
            var restored=TowerJourneyProgression.RestoreRuntime(request.ApiRoot,content,origin,replayRuntime);
            var count=await restored.Sources.VerifyClaimGuards(ct);guards+=count;
            TowerRuntimeCopy.Equal(replayRuntime,restored.ExportRuntime(),"retry guards preserve growth and all source state");
            live.Sources.ReconcileResources(cp);restored.Sources.ReconcileResources(cp);
            var canonicalRuntime=restored.ExportRuntime();
            TowerRuntimeCopy.Equal(live.ExportRuntime(),canonicalRuntime,"reconciled clone");
            TowerRuntimeCopy.Equal(proof.GetProperty("mastery"),canonicalRuntime.Mastery.Select(m=>new {m.DungeonDefinitionId,m.Experience,m.Level,m.CompletionCount}).ToArray(),"mastery witness");
            // Discarded branch exercises current-day and next-UTC-day progress/claims through the same native services.
            var probeAt=new DateTimeOffset(live.Now.UtcDateTime.Date,TimeSpan.Zero).AddDays(1);
            foreach(var j in new[]{live,restored})
            {
                await j.Idle("region_01_area_06",true,10,ct);
                j.WaitUntil(probeAt);await j.Idle("region_01_area_06",true,10,ct);
            }
            TowerRuntimeCopy.Equal(live.ExportRuntime(),restored.ExportRuntime(),"resumed current/next-day behavior");
            var file=cp.Point.History+"--runtime.json";
            Save(file,new {history=cp.Point.History,origin,checkpointFile=filename,checkpointHash=HarnessJson.FileHash(Path.Combine(request.EntryArchive,filename)),
                replayRuntime,canonicalRuntime,excludedBoundaryOffers,claimGuards=count,prophecyRuntimeRehydrated=true,
                discardedProbe=new {at=probeAt,state=restored.State(),runtimeHash=HarnessJson.Hash(restored.ExportRuntime()),retained=false},
                retainedBlueprintProgress=proof.GetProperty("retainedBlueprintProgress"),owned=cp.Point.Owned,character=cp.Point.Character});
            runtimes.Add(cp.Point.History,(cp,origin,canonicalRuntime));
            summaries.Add(new {file,history=cp.Point.History,claims=cp.Claims.Length,offers=cp.Days.Length,dungeonRuns=cp.Steps.Length,claimGuards=count});
        }
        var branches=new List<object>();
        foreach(var server in predecessor.GetProperty("servers").EnumerateArray())
        {
            var sourceFile=server.GetProperty("file").GetString()!;
            var source=HarnessJson.Read<JsonElement>(Path.Combine(request.EntryArchive,sourceFile));
            foreach(var entry in source.GetProperty("entries").EnumerateArray())
            {
                var history=entry.GetProperty("history").GetString()!;var (cp,origin,snapshot)=runtimes[history];
                var native=entry.GetProperty("native");var branch=TowerJourneyProgression.RestoreRuntime(request.ApiRoot,content,origin,snapshot);
                var before=branch.ExportRuntime();branch.WaitUntil(native.GetProperty("startsAt").GetDateTimeOffset());
                TowerRuntimeCopy.Equal(before.Growth,branch.ExportRuntime().Growth,"waiting growth");
                TowerRuntimeCopy.Equal(before.Sources,branch.ExportRuntime().Sources,"waiting resources");
                var prepared=await TowerUpgradeEntryStudy.Entry(request.ApiRoot,content,cp,native.GetProperty("highestCleared").GetInt32(),branch.Now,ct);
                TowerRuntimeCopy.Equal(native,prepared,"resumed preparation/access parity");
                branches.Add(new {server=source.GetProperty("key").GetString(),history,at=branch.Now,branch.WaitingTicks,
                    runtimeHash=HarnessJson.Hash(branch.ExportRuntime()),native,participant=entry.GetProperty("participant").GetBoolean()});
            }
        }
        if(runtimes.Count!=32||branches.Count!=512)throw new InvalidDataException("Incomplete restoration population.");
        Save("branches.json",branches);
        TowerEarnedPartyStudy.Verify(request.InputHashes);
        Save("result.json",new {version=Version,status="ContinuationRuntimeQualified",plan,personal=summaries,branches=512,preparations=512,
            claimGuards=guards,replayedIdleEncounters=32*25920,newFights=0,newSeeds=0,actualEntries=0,earnedEquipment=0,measuredPlayerSamples=0});
        Save("files.json",Directory.GetFiles(request.Output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    }
}
