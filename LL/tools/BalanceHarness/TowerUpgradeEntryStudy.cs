using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Dungeons;
using Services.LL.Items;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Rewards;
using Services.LL.WorldTower;
using static BalanceHarness.TowerGrowthProgression;

namespace BalanceHarness;

public sealed record TowerUpgradeEntryRequest(string ApiRoot,string Fixtures,string GrowthArchive,string PartyArchive,
    string UnlockArchive,string Output,IReadOnlyDictionary<string,string> InputHashes);
public sealed record TowerUpgradeStock(string Item,int Quest,int Idle,int Assembled,int Spent,int Remaining);
public sealed record TowerUpgradeCheckpoint(TowerEarnedPoint Point,JsonElement State,IReadOnlyDictionary<string,int> Inventory,
    IReadOnlyList<TowerUpgradeStock> SigilReconciliation,JsonElement[] Steps,JsonElement[] Entries,JsonElement[] Claims,
    JsonElement[] Days,JsonElement[] DungeonEvents);

/// <summary>Seed-free qualification of the next personally funded entry. Does not grant a future completion or gear.</summary>
public static class TowerUpgradeEntryStudy
{
    public const string Version="tower-upgrade-entry-v1";
    public const string GrowthPin="f8f05eb4cf6d1e9c28a87b72bfeb0f1a9ff9f1229c5ad687c0413b042d7fbdc5";
    public const string UnlockPin="fce82fe32b7bfb224f9b6b4d6675a0fca72bc202c12aed54685d3735d698fc52";
    private static JsonElement[] Array(JsonElement value,string property)=>value.GetProperty(property).EnumerateArray().ToArray();
    public static TowerUpgradeCheckpoint Restore(TowerEarnedPoint point,JsonElement history)
    {
        var summary=history.GetProperty("summary");
        if(point.Policy!="earned-progression"||point.Horizon!=25920||point.Encounter!=25920
            ||summary.GetProperty("key").GetString()!=point.History||summary.GetProperty("outcome").GetString()!=point.Outcome)
            throw new InvalidDataException("Exact earned 72-hour history required.");
        var checkpoint=Array(history,"checkpoints").Single(c=>c.GetProperty("encounter").GetInt32()==point.Encounter);
        if(HarnessJson.Hash(checkpoint.GetProperty("character"))!=HarnessJson.Hash(point.Character))
            throw new InvalidDataException("Checkpoint owner/loadout drift.");
        var steps=Array(history,"steps").Where(s=>s.GetProperty("encounter").GetInt32()<=point.Encounter).ToArray();
        var progression=history.GetProperty("progression");
        var entries=Array(progression,"entries").Where(e=>steps.Any(s=>s.GetProperty("ordinal").GetInt32()==e.GetProperty("ordinal").GetInt32())).ToArray();
        var state=Array(progression,"checkpoints").Single(c=>c.GetProperty("encounter").GetInt32()==point.Encounter).GetProperty("state");
        if(steps.Any(s=>s.GetProperty("encounter").GetInt32()==point.Encounter))state=entries.Last().GetProperty("after");
        if(state.GetProperty("at").GetDateTimeOffset()!=point.AvailableAt||state.GetProperty("growth").GetProperty("level").GetInt32()!=point.Character.Level
            ||state.GetProperty("source").GetProperty("owner").GetGuid()!=point.Character.Id)
            throw new InvalidDataException("Checkpoint chronology/growth drift.");
        var windows=Array(history,"windows").Where(w=>w.GetProperty("until").GetInt32()<=point.Encounter).ToArray();
        var inventory=state.GetProperty("source").GetProperty("items").Deserialize<Dictionary<string,int>>(HarnessJson.Options)!;
        var rows=new List<TowerUpgradeStock>();
        foreach(var family in new[] {"goblin_mines","forgotten_catacombs"})
        {
            var item="sigil_"+family;
            var quest=family=="forgotten_catacombs"?1:summary.GetProperty("questAt").GetInt32()<=point.Encounter?1:0;
            var idle=windows.Sum(w=>w.GetProperty("sigils").TryGetProperty(item,out var v)?v.GetInt32():0);
            var assembled=family=="goblin_mines"?Array(progression,"checkpoints").Where(c=>c.GetProperty("encounter").GetInt32()<=point.Encounter).Sum(c=>c.GetProperty("assembled").GetInt32()):0;
            var spent=steps.Count(s=>s.GetProperty("dungeon").GetString()==family);
            var remaining=checkpoint.GetProperty("sigils").GetProperty(item).GetInt32();
            if(remaining<0||quest+idle+assembled-spent!=remaining||inventory.GetValueOrDefault(item)!=assembled)
                throw new InvalidDataException("Sigil conservation failed; source receipts are not spendable stock.");
            inventory[item]=remaining;rows.Add(new(item,quest,idle,assembled,spent,remaining));
        }
        if(inventory.Any(p=>p.Value<0)||entries.Length!=steps.Length)throw new InvalidDataException("Invalid personal prefix.");
        foreach(var step in steps)
        foreach(var blueprint in step.GetProperty("dungeonLoot").GetProperty("blueprints").EnumerateObject())
            inventory[blueprint.Name]=checked(inventory.GetValueOrDefault(blueprint.Name)+blueprint.Value.GetInt32());
        var owned=history.GetProperty("starting").Deserialize<List<EquipmentData>>(HarnessJson.Options)!;
        foreach(var w in windows)owned.AddRange(w.GetProperty("equipment").Deserialize<EquipmentData[]>(HarnessJson.Options)!);
        foreach(var step in steps)
        {
            if(step.GetProperty("award").ValueKind!=JsonValueKind.Null)owned.Add(step.GetProperty("award").Deserialize<EquipmentData>(HarnessJson.Options)!);
            owned.AddRange(step.GetProperty("dungeonLoot").GetProperty("equipment").Deserialize<EquipmentData[]>(HarnessJson.Options)!);
        }
        if(owned.Any(e=>e.State.Ownership.OwnerId!=point.Character.Id)||owned.Select(e=>e.State.Id).Distinct().Count()!=owned.Count
            ||HarnessJson.Hash(owned.OrderBy(e=>e.State.Id).ToArray())!=HarnessJson.Hash(point.Owned.OrderBy(e=>e.State.Id).ToArray()))
            throw new InvalidDataException("Future, missing, duplicated or foreign owned equipment.");
        // Idle advances the clock after activity. An Observe at exactly the terminal checkpoint
        // belongs to the next activity, whereas a completed run's claims/events can occur at its end.
        JsonElement[] Prefix(string field)=>Array(progression,field).Where(e=>field=="days"
            ?e.GetProperty("at").GetDateTimeOffset()<point.AvailableAt
            :e.GetProperty("at").GetDateTimeOffset()<=point.AvailableAt).ToArray();
        return new(point,state,new SortedDictionary<string,int>(inventory),rows,steps,entries,Prefix("claims"),Prefix("days"),Prefix("dungeonEvents"));
    }

    public static async Task<object> Entry(string root,OfflineContent content,TowerUpgradeCheckpoint checkpoint,int highestCleared,
        DateTimeOffset serverAt,CancellationToken ct)
        =>await Entry(root,content,checkpoint.Point,checkpoint.Inventory,highestCleared,serverAt,ct);

    public static async Task<object> Entry(string root,OfflineContent content,TowerEarnedPoint point,IReadOnlyDictionary<string,int> stock,int highestCleared,
        DateTimeOffset serverAt,CancellationToken ct)
    {
        if(highestCleared is <0 or >4)throw new InvalidDataException("Unqualified server progress.");
        var c=point.Character;
        var config=new ConfigurationBuilder().Build();
        var rewards=new JsonRewardTableDefinitionProvider(config,root,HarnessJson.Options,new RewardTableDefinitionValidator());
        var definitions=new JsonDungeonDefinitions(new JsonDocumentReader<DungeonCatalogDocument>(root,"Data/dungeons/dungeons.json",HarnessJson.Options),new(new()),new DungeonDefinitionValidator(),rewards);
        var bases=HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/items/items.json")).EnumerateArray()
            .Where(j=>j.GetProperty("id").GetString()!.StartsWith("sigil_",StringComparison.Ordinal)).Select(j=>j.Deserialize<ItemBase>(HarnessJson.Options)!).ToDictionary(b=>b.Id);
        var access=new DungeonAccessPolicy(Boundary<IDungeonRunRepository>(),
            Boundary<IInventoryRepository>((m,a)=>m.Name=="GetInventoryQuantityAsync"&&(Guid)a[0]! == c.Id
                ?Task.FromResult(stock.GetValueOrDefault((string)a[1]!)):throw new InvalidDataException(m.Name)),
            Boundary<IItemBaseRepository>((m,a)=>m.Name=="GetItemBasesByIdsAsync"
                ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(((IReadOnlyCollection<string>)a[0]!).ToDictionary(id=>id,id=>bases[id])):throw new InvalidDataException(m.Name)),
            Boundary<IDbContext>(),Options.Create(new WorldTowerOptions()));
        var accessRows=new List<object>();
        foreach(var family in new[] {"goblin_mines","forgotten_catacombs"})
        {
            var d=definitions.GetByKey(family);
            if(d.Region!=1||d.Tier!=1||d.RequiredPreviousDungeonId is not null||d.RequiredTowerFloor is not null
                ||d.EntryCosts.Count!=1||d.EntryCosts[0].ItemId!="sigil_"+family||d.EntryCosts[0].Amount!=1)
                throw new InvalidDataException("Changed native entry prerequisites/costs.");
            accessRows.Add(new {family,costs=d.EntryCosts,result=await access.EvaluateAsync(c.Id,d,ct)});
        }
        var missing=TowerEntryReadiness.MissingSlots(c.Equipment);
        var familyChoice=TowerActivityStudy.ChooseSource("mines-first-either",stock);
        var reason=missing.Count>0?"EquipmentCoverage":familyChoice is null?"NoSigil":"Enter";
        var selected=reason=="Enter"?familyChoice:null;
        var after=new SortedDictionary<string,int>(stock.ToDictionary(p=>p.Key,p=>p.Value));
        if(selected is not null)after["sigil_"+selected]--;
        var supplies=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(root,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        var floors=TowerContentProviders.Floors(Path.Combine(root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var supply=supplies.Candidates(1,c.Level).FirstOrDefault(s=>s.RequiredClearedFloor<=highestCleared&&floors.GetFloor(s.TargetFloor) is not null);
        var setup=content.CreateSetup(c.Materialize(content.Equipment),c.MaterializeEssences());
        var pipeline=new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content,setup),setup);
        var prepared=(await pipeline.PrepareAsync(CombatContentType.Dungeon,
            [new(new("player",c.Id,CombatSide.Friendly,1),new SnapshotCombatantPreparationSource(TowerBattleRunner.ToSnapshot(c,content)))],ct)).Single().Combatant;
        return new {owner=c.Id,highestCleared,startsAt=serverAt>point.AvailableAt?serverAt:point.AvailableAt,
            policy="full-slot-ready-mines-first-next-entry-v1",reason,selectedDungeon=selected,missingSlots=missing,
            nativeAccess=accessRows,inventoryBefore=stock,inventoryAfterHypotheticalEntry=after,
            affordableAttempts=stock.GetValueOrDefault("sigil_goblin_mines")+stock.GetValueOrDefault("sigil_forgotten_catacombs"),
            prospectiveSupply=supply,awardedEquipment=0,actualEntries=0,
            prepared=new {prepared.Level,prepared.CombatAttributes,equipment=prepared.Equipment.Select(e=>e.ProgressionData).ToArray(),
                essences=prepared.EquippedEssences.Select(e=>new {e.Id,e.EssenceDefinitionId,e.Level,e.AscensionTier}).ToArray()}};
    }

    public static async Task Run(TowerUpgradeEntryRequest request,CancellationToken ct)
    {
        TowerEarnedPartyStudy.Verify(request.InputHashes);
        foreach(var (path,pin) in new[] {(request.GrowthArchive,GrowthPin),(request.PartyArchive,TowerUnlockStudy.PartyPin),(request.UnlockArchive,UnlockPin)})
            if(HarnessJson.FileHash(Path.Combine(path,"files.json"))!=pin)throw new InvalidDataException("Wrong input manifest pin.");
        if(Path.Exists(request.Output))throw new InvalidDataException("Fresh output required.");
        var plan=HarnessJson.Read<JsonElement>(Path.Combine(request.Fixtures,"tower-upgrade-entry.json"));
        if(plan.GetProperty("version").GetString()!=Version||plan.GetProperty("checkpoint").GetInt32()!=25920)
            throw new InvalidDataException("Changed qualification plan.");
        var content=OfflineContent.ForTower(request.ApiRoot,TowerBundle.ReadSettings(request.ApiRoot));
        var points=HarnessJson.Read<TowerEarnedPoint[]>(Path.Combine(request.PartyArchive,"points.json"))
            .Where(p=>p.Policy=="earned-progression"&&p.Horizon==25920).ToArray();
        if(points.Length!=32)throw new InvalidDataException("All 32 personal alternatives required.");
        Directory.CreateDirectory(request.Output);var restored=new Dictionary<string,TowerUpgradeCheckpoint>();var personal=new List<object>();long bytes=0;
        void Save(string file,object value)
        {
            var data=JsonSerializer.SerializeToUtf8Bytes(value,HarnessJson.Options);bytes+=data.Length;
            if(bytes>256*1048576L)throw new InvalidDataException("Output bound exceeded.");
            using var stream=new FileStream(Path.Combine(request.Output,file),FileMode.CreateNew);stream.Write(data);
        }
        foreach(var point in points)
        {
            ct.ThrowIfCancellationRequested();var history=HarnessJson.Read<JsonElement>(Path.Combine(request.GrowthArchive,point.History+"--history.json"));
            var checkpoint=Restore(point,history);restored.Add(point.History,checkpoint);
            var repository=new TowerGrowthStudy.MasteryRepository();var mastery=new DungeonMasteryService(repository);var masteryPrefix=new List<object>();
            foreach(var step in checkpoint.Steps)
            {
                var file=step.GetProperty("file").GetString()!;var recorded=TowerUnlockStudy.Read(Path.Combine(request.GrowthArchive,file)).Deserialize<DungeonAcquisitionRun>(HarnessJson.Options)!;
                var run=TowerGrowthStudy.Reconstruct(point.Character.Id,recorded);var award=await mastery.AwardRunMasteryAsync(run,ct);
                var entry=checkpoint.Entries.Single(e=>e.GetProperty("ordinal").GetInt32()==step.GetProperty("ordinal").GetInt32());
                if(HarnessJson.Hash(award)!=HarnessJson.Hash(entry.GetProperty("receipt").GetProperty("mastery")))throw new InvalidDataException("Native mastery prefix mismatch.");
                if(!(await mastery.AwardRunMasteryAsync(run,ct)).AlreadyAwarded)throw new InvalidDataException("Mastery duplicated.");
                masteryPrefix.Add(new {file,award});
            }
            var name=point.History+"--checkpoint.json";
            Save(name,new {checkpoint,masteryPrefix,mastery=repository.Rows.OrderBy(r=>r.DungeonDefinitionId).Select(r=>new {r.DungeonDefinitionId,r.Experience,r.Level,r.CompletionCount}).ToArray(),
                retainedBlueprintProgress=checkpoint.Steps.LastOrDefault().ValueKind==JsonValueKind.Undefined?JsonSerializer.SerializeToElement(new object[0]):checkpoint.Steps.Last().GetProperty("dungeonLoot").GetProperty("after"),
                pendingDungeonExperience=0,prophecyRuntimeRehydrated=false,
                historicalFile=point.History+"--history.json",historicalHash=HarnessJson.FileHash(Path.Combine(request.GrowthArchive,point.History+"--history.json"))});
            personal.Add(new {file=name,point.History,owner=point.Character.Id,point.Outcome});
        }
        var servers=new List<object>();var result=HarnessJson.Read<JsonElement>(Path.Combine(request.UnlockArchive,"result.json"));
        foreach(var row in Array(result,"journeys"))
        {
            var key=row.GetProperty("key").GetString()!;var journey=HarnessJson.Read<JsonElement>(Path.Combine(request.UnlockArchive,key+"--journey.json"));
            var state=journey.GetProperty("final");var participants=Array(journey,"unchangedOwnedCharacterHashes").Select(p=>p.GetProperty("owner").GetGuid()).ToHashSet();
            var entries=new List<object>();
            foreach(var point in points.Where(p=>p.Outcome==journey.GetProperty("outcome").GetString()))
                entries.Add(new {history=point.History,participant=participants.Contains(point.Character.Id),
                    native=await Entry(request.ApiRoot,content,restored[point.History],journey.GetProperty("highestCleared").GetInt32(),state.GetProperty("at").GetDateTimeOffset(),ct)});
            var file=key+"--entries.json";Save(file,new {key,serverFile=key+"--journey.json",serverHash=HarnessJson.FileHash(Path.Combine(request.UnlockArchive,key+"--journey.json")),serverState=state,entries});
            servers.Add(new {key,file});
        }
        TowerEarnedPartyStudy.Verify(request.InputHashes);
        Save("result.json",new {version=Version,status="FundedUpgradeEntriesQualifiedNotExecuted",plan,personal,servers,preparations=512,newFights=0,newCombatSeeds=0,actualEntries=0,earnedEquipment=0,measuredPlayerSamples=0});
        Save("files.json",Directory.GetFiles(request.Output).Order().ToDictionary(p=>Path.GetFileName(p)!,HarnessJson.FileHash));
    }
}
