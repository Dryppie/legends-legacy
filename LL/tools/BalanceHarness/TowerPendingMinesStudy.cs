using System.Text.Json;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Items;

namespace BalanceHarness;

public sealed record TowerPendingMinesRequest(string Mode,string ApiRoot,string Fixtures,string SourceArchive,string ReturnArchive,
    string Output,IReadOnlyDictionary<string,string> InputHashes,string? Qualification=null,string? QualificationPin=null,
    IReadOnlyDictionary<string,DungeonAcquisitionPanel>? Panels=null);
public sealed record TowerPendingMinesBranch(string Key,string Server,string SourceFile,string SourceHash,TowerFifthResult Pending,
    int Mastery,JsonElement Native,FixtureCharacter Control,JsonElement ControlNative);
public delegate Task<TowerFifthResult> TowerPendingQuestClaim(TowerFifthResult pending,TowerReturnOwner after,DungeonAcquisitionRun run,
    TowerJourneyDungeonReceipt receipt,string file,string hash,string option,CancellationToken ct);

/// <summary>One explicitly paid Mines attempt per pending state; completed quests and historical Tower failures stay immutable.</summary>
public static class TowerPendingMinesStudy
{
    public const string Version="tower-pending-mines-v1";
    public const string SourcePin="e0c1e2f53dcd3c0778e032c97a9904b695b8ba753de0979aa2d05b0701a49f25";
    public const int Owners=97,SeedCount=6305,FightCap=18624;
    private static JsonElement Json(object value)=>JsonSerializer.SerializeToElement(value,HarnessJson.Options);
    private static async Task<JsonElement> Preview(string root,OfflineContent content,TowerReturnOwner owner,CancellationToken ct)
        =>Json(await TowerUpgradeEntryStudy.Entry(root,content,owner.Point,owner.Runtime.Sources.State.Items,4,TowerReturnStudy.At(owner.Runtime),ct));
    private static (TowerBootstrapPlan Plan,Dictionary<string,TowerBootstrapCell> Recipes,TowerEquipmentSupplyCatalog Catalog) Supplies(string root,string fixtures,OfflineContent content)
    {
        var plan=TowerBootstrapCohorts.Read(Path.Combine(fixtures,"tower-bootstrap.json"));
        return(plan,TowerBootstrapCohorts.Create(root,fixtures,plan,content).Where(c=>c.Gear=="common"&&c.EssenceLevel==1).ToDictionary(c=>c.Recipe),
            JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(root,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment));
    }
    private static void Validate(TowerPendingMinesRequest q)
    {
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        if(Path.Exists(q.Output)||HarnessJson.FileHash(Path.Combine(q.SourceArchive,"files.json"))!=SourcePin
            ||HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-pending-mines.json")).GetProperty("version").GetString()!=Version)
            throw new InvalidDataException("Changed pending Mines scope.");
    }
    public static async Task Prepare(TowerPendingMinesRequest q,CancellationToken ct)
    {
        Validate(q);if(q.Mode!="prepare"||q.Panels is not null)throw new InvalidDataException("Preparation must be seed-free.");
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var inventory=new TowerActivityInventory(q.ApiRoot,content);
        var (plan,recipes,catalog)=Supplies(q.ApiRoot,q.Fixtures,content);var branches=new List<TowerPendingMinesBranch>();
        foreach(var row in TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,"result.json")).GetProperty("journeys").EnumerateArray())
        {
            var server=row.GetProperty("key").GetString()!;var file=server+"--acquisition.json.gz";var source=TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,file));
            foreach(var pending in source.GetProperty("acquired").Deserialize<TowerFifthResult[]>(HarnessJson.Options)!.Where(r=>r.Acquisition is null))
            {
                var owner=pending.Owner;var native=await Preview(q.ApiRoot,content,owner,ct);
                if(native.GetProperty("reason").GetString()!="Enter"||native.GetProperty("selectedDungeon").GetString()!="goblin_mines"
                    ||owner.Point.Character.Level!=40||owner.Point.Character.Essences.Count!=4||pending.Quest.GetProperty("status").GetString()!="Active")
                    throw new InvalidDataException("Unfunded, held or completed pending entry.");
                TowerEarnedPartyStudy.ValidateInventory(owner.Point.Character,owner.Point.Owned,inventory);
                var journey=TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,owner.Origin,owner.Runtime);
                var mastery=(await journey.Mastery.GetMasteryByDungeonAsync(owner.Point.Character.Id,["goblin_mines"],ct))["goblin_mines"].Level;
                var targets=TowerContinuationSupply.Targets(content,catalog,"item.tower_supply.v1.floor_04",owner.Point.Character.Id,recipes[owner.Point.Recipe],plan.PurchaseOrder);
                var control=owner.Point.Character with {Name=owner.History+"--pending-mines-epic-control",Equipment=inventory.Select(targets)};
                var controlNative=await Preview(q.ApiRoot,content,owner with {Point=owner.Point with {Character=control,Owned=targets}},ct);
                branches.Add(new(server+"--"+owner.History,server,file,HarnessJson.FileHash(Path.Combine(q.SourceArchive,file)),pending,mastery,native,control,controlNative));
            }
        }
        if(branches.Count!=Owners||branches.Select(b=>b.Key).Distinct().Count()!=Owners)throw new InvalidDataException("Changed pending population.");
        Directory.CreateDirectory(q.Output);TowerReturnStudy.Save(q.Output,"branches.json.gz",branches);
        TowerReturnStudy.Save(q.Output,"result.json",new {version=Version,status="PendingMinesQualified",plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-pending-mines.json")),
            owners=Owners,preparations=Owners*2,newFights=0,newSeeds=0,heldCompleted=143,heldServers=17,measuredPlayerSamples=0});
        TowerEarnedPartyStudy.Verify(q.InputHashes);Manifest(q.Output);
    }
    private static void Manifest(string output)=>TowerReturnStudy.Save(output,"files.json",Directory.GetFiles(output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));

    public static async Task Run(TowerPendingMinesRequest q,TowerPendingQuestClaim claim,CancellationToken ct)
    {
        Validate(q);if(q.Mode!="combat"||q.Qualification is null||q.QualificationPin is null
            ||HarnessJson.FileHash(Path.Combine(q.Qualification,"files.json"))!=q.QualificationPin||q.Panels?.Count!=Owners)
            throw new InvalidDataException("Missing pending Mines qualification.");
        var seeds=q.Panels.Values.SelectMany(p=>p.RoomSeeds.Prepend(p.LayoutSeed)).ToArray();
        if(seeds.Length!=SeedCount||seeds.Distinct().Count()!=SeedCount)throw new InvalidDataException("Changed reserved panel.");
        var branches=TowerUnlockStudy.Read(Path.Combine(q.Qualification,"branches.json.gz")).Deserialize<TowerPendingMinesBranch[]>(HarnessJson.Options)!;
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));var runner=new DungeonAcquisitionRunner(q.ApiRoot,content);
        var inventory=new TowerActivityInventory(q.ApiRoot,content);var (bootstrap,recipes,catalog)=Supplies(q.ApiRoot,q.Fixtures,content);
        var choices=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-fifth-essence.json")).GetProperty("choices");
        Directory.CreateDirectory(q.Output);int fights=0;var used=new HashSet<int>();var rows=new List<object>();var updates=new Dictionary<string,TowerFifthResult>();
        void Count(){ct.ThrowIfCancellationRequested();if(++fights>FightCap)throw new InvalidDataException("Mines fight cap.");}
        void Save(string file,object data)
        {
            TowerReturnStudy.Save(q.Output,file,data);
            if(Directory.GetFiles(q.Output).Sum(f=>new FileInfo(f).Length)>256*1048576L)throw new InvalidDataException("Mines output cap.");
        }
        foreach(var b in branches)
        {
            var owner=b.Pending.Owner;var before=owner.Runtime;var entry=owner.Point.Character;var panel=q.Panels[b.Key];
            if(panel.Dungeon!="goblin_mines")throw new InvalidDataException("Unqualified family.");
            TowerRuntimeCopy.Equal(b.Native,await Preview(q.ApiRoot,content,owner,ct),"qualified entry parity");
            var journey=TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,owner.Origin,before);
            var costs=new Dictionary<string,int>{{"sigil_goblin_mines",1}};journey.Sources.SpendEntry(costs);
            TowerRuntimeCopy.Equal(b.Native.GetProperty("inventoryAfterHypotheticalEntry"),journey.Sources.State().Items,"native debit parity");
            var progress=new TowerJourneyDungeon(q.ApiRoot,journey);var run=await runner.RunAsync(entry,panel,Count,ct,progress,b.Mastery);
            var afterProgress=journey.ExportRuntime();var file=b.Key+"--run.json.gz";Save(file,run);
            var replay=await runner.RunAsync(entry,panel,Count,ct,masteryLevel:b.Mastery);TowerRuntimeCopy.Equal(run,replay,"pending Mines full-run replay");Save(b.Key+"--replay.json.gz",replay);
            var control=await runner.RunAsync(b.Control,panel,Count,ct,masteryLevel:b.Mastery);Save(b.Key+"--control.json.gz",control);
            used.Add(panel.LayoutSeed);foreach(var battle in run.Battles.Concat(control.Battles))used.Add(battle.Seed);
            var loot=TowerDungeonLoot.Restore(q.ApiRoot,content,entry.Id,owner.BlueprintProgress);var fork=loot.Fork();
            var ordinary=await loot.ApplyAsync(entry.Id,run,ct,b.Mastery,true);
            TowerRuntimeCopy.Equal(ordinary,await fork.ApplyAsync(entry.Id,run,ct,b.Mastery,true),"pending Mines loot replay");
            var owned=owner.Point.Owned.Concat(ordinary.Equipment).ToList();journey.Sources.AddClaimedItems(ordinary.Blueprints);
            var supply=await TowerContinuationSupply.Claim(q.ApiRoot,content,entry,run,4,journey.Sources,owned,recipes[owner.Point.Recipe],bootstrap.PurchaseOrder,ct);
            if(supply.Equipment is not null)owned.Add(supply.Equipment);
            var character=journey.Growth.Snapshot(entry with {Equipment=inventory.Select(owned)});
            var after=owner with {Point=owner.Point with {Character=character,Owned=owned.ToArray(),AvailableAt=journey.Now},Runtime=journey.ExportRuntime(),BlueprintProgress=ordinary.After.ToArray()};
            var acquisition=await claim(b.Pending,after,run,progress.Receipt!,file,HarnessJson.FileHash(Path.Combine(q.Output,file)),choices.GetProperty(owner.Point.Recipe).GetString()!,ct);
            updates.Add(b.Key,acquisition);TowerJourneyProgression.RestoreRuntime(q.ApiRoot,content,acquisition.Owner.Origin,acquisition.Owner.Runtime);
            var next=await Preview(q.ApiRoot,content,acquisition.Owner,ct);
            var progressFile=b.Key+"--progress.json.gz";
            Save(progressFile,new {key=b.Key,server=b.Server,history=owner.History,native=b.Native,before,entry,costs,runFile=file,receipt=progress.Receipt,
                afterProgress,ordinary,supply,owned,character,after=after.Runtime,beforeQuest=after,acquisition,next,
                candidateTargets=supply.Claimed.Count==0?Array.Empty<EquipmentData>():TowerContinuationSupply.Targets(content,catalog,supply.Claimed.Keys.Single(),entry.Id,recipes[owner.Point.Recipe],bootstrap.PurchaseOrder),
                assemblyAfterEntry=0,additionalEntries=0,measuredPlayerSamples=0});
            rows.Add(new {key=b.Key,server=b.Server,history=owner.History,progressFile,run.Status,run.CombatSeconds,controlStatus=control.Status,
                newFifth=acquisition.Acquisition is not null,newEquipment=owned.Count-owner.Point.Owned.Count});
        }
        var servers=new List<object>();
        foreach(var row in TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,"result.json")).GetProperty("journeys").EnumerateArray())
        {
            var key=row.GetProperty("key").GetString()!;var file=key+"--acquisition.json.gz";var old=TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,file));
            var acquired=old.GetProperty("acquired").Deserialize<TowerFifthResult[]>(HarnessJson.Options)!.Select(r=>updates.GetValueOrDefault(key+"--"+r.Owner.History,r)).ToArray();
            var at=acquired.Max(r=>TowerReturnStudy.At(r.Owner.Runtime));var owners=acquired.Select(r=>r.Owner with {Runtime=TowerReturnStudy.Wait(r.Owner.Runtime,at)}).ToArray();
            var party=old.GetProperty("party").Deserialize<TowerEarnedParty>(HarnessJson.Options)!;var byId=owners.ToDictionary(o=>o.Point.Character.Id);
            party=party with {StartsAt=at,Members=party.Members.Select(m=>byId[m.Character.Id].Point).ToArray()};
            var prepared=IdleBattleRunner.DescribeParticipants(await TowerEarnedPartyStudy.Prepare(q.ApiRoot,content,party,0,ct));
            Save(key+"--continuation.json.gz",new {key,sourceFile=file,sourceHash=HarnessJson.FileHash(Path.Combine(q.SourceArchive,file)),
                server=old.GetProperty("server"),historicalAttempts=old.GetProperty("historicalAttempts"),personalRefreshBefore=old.GetProperty("personalRefreshBefore"),
                nextPersonalRefreshBefore=old.GetProperty("nextPersonalRefreshBefore"),acquired,owners,party,prepared});
            servers.Add(new {key,newFifths=acquired.Count(r=>r.Acquisition is not null)-row.GetProperty("granted").GetInt32(),pending=acquired.Count(r=>r.Acquisition is null),endsAt=at});
        }
        if(rows.Count!=Owners||updates.Count!=Owners||servers.Count!=15)throw new InvalidDataException("Incomplete paid wave.");
        TowerEarnedPartyStudy.Verify(q.InputHashes);
        Save("result.json",new {version=Version,status="PendingMinesComplete",plan=HarnessJson.Read<JsonElement>(Path.Combine(q.Fixtures,"tower-pending-mines.json")),results=rows,servers,
            held=TowerUnlockStudy.Read(Path.Combine(q.SourceArchive,"result.json")).GetProperty("held"),attempts=Owners,controls=Owners,replays=Owners,fights,
            reservedSeeds=SeedCount,usedSeeds=used.Order().ToArray(),measuredPlayerSamples=0,newTowerFights=0,searchPerformed=false});Manifest(q.Output);
    }
}
