using System.IO.Compression;
using System.Text.Json;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerNextEntryRequest(string ApiRoot,string Fixtures,string Qualification,string QualificationPin,
    string EntryArchive,string Output,IReadOnlyDictionary<string,string> InputHashes,IReadOnlyDictionary<string,DungeonAcquisitionPanel> Panels);

public static class TowerNextEntryStudy
{
    public const string Version="tower-funded-next-entry-v1";
    public const string RuntimePin="981c2adf6ad621f0060d03df409969fd69e91cabc6d196c8b94fa60b6892a76b";
    public static async Task Run(TowerNextEntryRequest q,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        if(q.QualificationPin!=RuntimePin||HarnessJson.FileHash(Path.Combine(q.Qualification,"files.json"))!=RuntimePin
            ||HarnessJson.FileHash(Path.Combine(q.EntryArchive,"files.json"))!=TowerRuntimeStudy.EntryPin||Path.Exists(q.Output))
            throw new InvalidDataException("Audited runtime and fresh output required.");
        var plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-funded-next-entry.json"));
        if(plan.GetProperty("version").GetString()!=Version||q.Panels.Count!=12)throw new InvalidDataException("Changed next-entry panel.");
        var seeds=q.Panels.Values.SelectMany(p=>p.RoomSeeds.Prepend(p.LayoutSeed)).ToArray();
        if(seeds.Length!=780||seeds.Distinct().Count()!=780)throw new InvalidDataException("Fresh distinct reserved panel required.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var runner=new DungeonAcquisitionRunner(q.ApiRoot,content);
        var inventory=new TowerActivityInventory(q.ApiRoot,content);var bootstrap=TowerBootstrapCohorts.Read(Path.Combine(q.Fixtures,"tower-bootstrap.json"));
        var recipes=TowerBootstrapCohorts.Create(q.ApiRoot,q.Fixtures,bootstrap,content).Where(c=>c.Gear=="common"&&c.EssenceLevel==1).ToDictionary(c=>c.Recipe);
        var catalog=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(q.ApiRoot,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        var rows=HarnessJson.Read<JsonElement[]>(Path.Combine(q.Qualification,"branches.json"));
        var runtimes=HarnessJson.Read<JsonElement>(Path.Combine(q.Qualification,"result.json")).GetProperty("personal").EnumerateArray()
            .ToDictionary(r=>r.GetProperty("history").GetString()!,r=>HarnessJson.Read<JsonElement>(Path.Combine(q.Qualification,r.GetProperty("file").GetString()!)));
        if(rows.Length!=512||rows.Count(r=>r.GetProperty("native").GetProperty("reason").GetString()=="Enter")!=192)
            throw new InvalidDataException("All qualified server/owner alternatives required.");
        Directory.CreateDirectory(q.Output);long bytes=0;var fights=0;var results=new List<object>();var controls=new List<object>();
        var first=new Dictionary<string,string>();var used=new HashSet<int>();
        void Count(){ct.ThrowIfCancellationRequested();if(++fights>13824)throw new InvalidDataException("Fight bound exceeded.");}
        void Save(string file,object value)
        {
            byte[] data;
            using(var m=new MemoryStream())
            {
                if(file.EndsWith(".gz")){using(var gzip=new GZipStream(m,CompressionLevel.Optimal,true))JsonSerializer.Serialize(gzip,value,HarnessJson.Options);}
                else JsonSerializer.Serialize(m,value,HarnessJson.Options);
                data=m.ToArray();
            }
            bytes+=data.Length;if(bytes>256*1048576L)throw new InvalidDataException("Output byte bound exceeded.");
            using var f=new FileStream(Path.Combine(q.Output,file),FileMode.CreateNew);f.Write(data);
        }
        foreach(var row in rows)
        {
            ct.ThrowIfCancellationRequested();var history=row.GetProperty("history").GetString()!;var server=row.GetProperty("server").GetString()!;
            var native=row.GetProperty("native");var reason=native.GetProperty("reason").GetString()!;var source=runtimes[history];
            var checkpoint=HarnessJson.Read<JsonElement>(Path.Combine(q.EntryArchive,source.GetProperty("checkpointFile").GetString()!))
                .GetProperty("checkpoint").Deserialize<TowerUpgradeCheckpoint>(HarnessJson.Options)!;
            var origin=source.GetProperty("origin").Deserialize<FixtureCharacter>(HarnessJson.Options)!;
            var saved=source.GetProperty("canonicalRuntime").Deserialize<TowerJourneyRuntime>(HarnessJson.Options)!;
            var journey=TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,origin,saved);journey.WaitUntil(row.GetProperty("at").GetDateTimeOffset());
            if(HarnessJson.Hash(journey.ExportRuntime())!=row.GetProperty("runtimeHash").GetString())throw new InvalidDataException("Qualified runtime drift.");
            var before=journey.ExportRuntime();var entry=checkpoint.Point.Character;var owned=checkpoint.Point.Owned.ToList();
            if(reason!="Enter")
            {
                results.Add(new {server,history,reason,participant=row.GetProperty("participant").GetBoolean(),actualEntries=0,
                    runtimeHash=HarnessJson.Hash(before),ownedHash=HarnessJson.Hash(owned),newEquipment=0});continue;
            }
            var panel=q.Panels[history];var family=native.GetProperty("selectedDungeon").GetString()!;
            if(panel.Dungeon!=family)throw new InvalidDataException("Changed source decision.");
            var mastery=(await journey.Mastery.GetMasteryByDungeonAsync(entry.Id,[family],ct))[family].Level;
            var costs=native.GetProperty("nativeAccess").EnumerateArray().Single(a=>a.GetProperty("family").GetString()==family)
                .GetProperty("costs").EnumerateArray().ToDictionary(c=>c.GetProperty("itemId").GetString()!,c=>c.GetProperty("amount").GetInt32());
            journey.Sources.SpendEntry(costs);
            TowerRuntimeCopy.Equal(native.GetProperty("inventoryAfterHypotheticalEntry"),journey.Sources.State().Items,"actual native entry debit");
            var progress=new TowerJourneyDungeon(q.ApiRoot,journey);var run=await runner.RunAsync(entry,panel,Count,ct,progress,mastery);
            var afterProgress=journey.ExportRuntime();
            var key=server+"--"+history;Save(key+"--run.json.gz",run);used.Add(panel.LayoutSeed);foreach(var b in run.Battles)used.Add(b.Seed);
            if(!first.TryGetValue(history,out var firstHash))
            {
                first.Add(history,HarnessJson.Hash(run));
                var replay=await runner.RunAsync(entry,panel,Count,ct,masteryLevel:mastery);
                TowerRuntimeCopy.Equal(run,replay,"exact full-run replay");Save(history+"--replay.json.gz",replay);
                var target=TowerContinuationSupply.Targets(content,catalog,"item.tower_supply.v1.floor_04",entry.Id,recipes[checkpoint.Point.Recipe],bootstrap.PurchaseOrder);
                var control=entry with {Name=history+"--supplied-epic-control",Equipment=inventory.Select(target)};
                var controlRun=await runner.RunAsync(control,panel,Count,ct,masteryLevel:mastery);
                Save(history+"--control.json.gz",controlRun);foreach(var b in controlRun.Battles)used.Add(b.Seed);
                controls.Add(new {history,character=control,mastery,controlRun.Status,controlRun.CombatSeconds,earned=false});
            }
            else if(firstHash!=HarnessJson.Hash(run))throw new InvalidDataException("Identical entry inputs changed across server alternatives.");
            var loot=TowerDungeonLoot.Restore(q.ApiRoot,content,entry.Id,source.GetProperty("retainedBlueprintProgress").Deserialize<TowerBlueprintState[]>(HarnessJson.Options)!);
            var retryLoot=loot.Fork();var awards=await loot.ApplyAsync(entry.Id,run,ct,mastery,true);
            TowerRuntimeCopy.Equal(awards,await retryLoot.ApplyAsync(entry.Id,run,ct,mastery,true),"ordinary reward replay");
            owned.AddRange(awards.Equipment);journey.Sources.AddClaimedItems(awards.Blueprints);
            var supply=await TowerContinuationSupply.Claim(q.ApiRoot,content,entry,run,native.GetProperty("highestCleared").GetInt32(),journey.Sources,owned,
                recipes[checkpoint.Point.Recipe],bootstrap.PurchaseOrder,ct);
            if(supply.Equipment is not null)owned.Add(supply.Equipment);
            var afterCharacter=journey.Growth.Snapshot(entry with {Equipment=inventory.Select(owned)});
            if(owned.Select(i=>i.State.Id).Distinct().Count()!=owned.Count||checkpoint.Point.Owned.Any(i=>!owned.Contains(i)))throw new InvalidDataException("Owned gear lost/duplicated.");
            var afterPoint=checkpoint.Point with {Character=afterCharacter,Owned=owned.ToArray(),AvailableAt=journey.Now};
            var next=await TowerUpgradeEntryStudy.Entry(q.ApiRoot,content,checkpoint with {Point=afterPoint,Inventory=journey.Sources.State().Items},native.GetProperty("highestCleared").GetInt32(),journey.Now,ct);
            Save(key+"--progress.json.gz",new {server,history,participant=row.GetProperty("participant").GetBoolean(),native,before,costs,entry,runFile=key+"--run.json.gz",
                receipt=progress.Receipt,afterProgress,ordinary=awards,supply,after=journey.ExportRuntime(),owned,character=afterCharacter,next,
                candidateTargets=supply.Claimed.Count==0?Array.Empty<EquipmentData>():TowerContinuationSupply.Targets(content,catalog,supply.Claimed.Keys.Single(),entry.Id,recipes[checkpoint.Point.Recipe],bootstrap.PurchaseOrder),
                newlyEquipped=afterCharacter.Equipment.Select(e=>e.Data.State.Id).Except(entry.Equipment.Select(e=>e.Data.State.Id)).ToArray(),
                assemblyAfterEntry=0,additionalEntries=0,measuredPlayerSamples=0});
            results.Add(new {server,history,reason,participant=row.GetProperty("participant").GetBoolean(),actualEntries=1,run.Status,run.CombatSeconds,
                newEquipment=awards.Equipment.Count+(supply.Equipment is null?0:1),supplyOpened=supply.Opened,
                suppliedChest=supply.Claimed.Keys.SingleOrDefault(),loadoutChanged=HarnessJson.Hash(entry.Equipment)!=HarnessJson.Hash(afterCharacter.Equipment),
                progressFile=key+"--progress.json.gz"});
        }
        if(first.Count!=12||controls.Count!=12)throw new InvalidDataException("Incomplete funded personal panel.");
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        Save("result.json",new {version=Version,status="FundedNextEntriesComplete",plan,results,controls,attempts=192,replays=12,fights,
            personalPanels=12,pairedServerAlternatives=true,reservedSeeds=780,usedSeeds=used.Order().ToArray(),measuredPlayerSamples=0});
        Save("files.json",Directory.GetFiles(q.Output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    }
}
