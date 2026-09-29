using System.Text.Json;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.WorldTower;
using Common.Randomness;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;
using Services.LL.Interfaces;
using Services.LL.Items;
using Services.LL.WorldTower;
using static BalanceHarness.TowerGrowthProgression;

namespace BalanceHarness;

public sealed record TowerContinuationSupplyReceipt(IReadOnlyList<RunReward> Pending,IReadOnlyDictionary<string,int> Claimed,
    EquipmentData? Equipment,string? Choice,bool Opened,int RetryChecks);

public static class TowerContinuationSupply
{
    public static EquipmentData[] Targets(OfflineContent content,TowerEquipmentSupplyCatalog catalog,string supply,Guid owner,
        TowerBootstrapCell recipe,IReadOnlyList<Domain.Models.Items.Equipments.Slots.EquipmentSlotType> order)
    {
        var band=catalog.Find(supply)!.Band;return order.Select((slot,i)=> {
            var original=content.Equipment.Evaluator.GetDefinition(recipe.Target.Single(e=>e.Slot==slot).Data.State.DefinitionId);
            var definition=content.Equipment.Evaluator.Definitions.Single(d=>d.ArchetypeId==original.ArchetypeId
                &&d.SpecializationId==original.SpecializationId&&d.NativeStyleId is null&&d.Rarity==band.Rarity);
            return catalog.Award(supply,definition.Id,owner,StableRandom.Guid("tower-next-entry-target-v1",owner.ToString(),supply,i.ToString()),"target-only");
        }).ToArray();
    }
    public static async Task<TowerContinuationSupplyReceipt> Claim(string root,OfflineContent content,FixtureCharacter entry,
        DungeonAcquisitionRun recorded,int highestCleared,TowerEntrySources sources,IReadOnlyList<EquipmentData> owned,
        TowerBootstrapCell recipe,IReadOnlyList<Domain.Models.Items.Equipments.Slots.EquipmentSlotType> order,CancellationToken ct)
    {
        var catalog=JsonTowerEquipmentSupplyCatalog.Load(Path.Combine(root,"Data/equipment/tower-equipment-supplies.v1.json"),content.Equipment);
        var bases=HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/items/items.json")).EnumerateArray()
            .ToDictionary(j=>j.GetProperty("id").GetString()!,j=>j.Deserialize<ItemBase>(HarnessJson.Options)!);
        var metadata=Boundary<IItemBaseRepository>((m,a)=>m.Name=="GetItemBasesByIdsAsync"
            ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(((IReadOnlyCollection<string>)a[0]!).ToDictionary(id=>id,id=>bases[id])):throw new InvalidDataException(m.Name));
        var run=TowerGrowthStudy.Reconstruct(entry.Id,recorded);var snapshot=TowerBattleRunner.ToSnapshot(entry,content);run.CharacterSnapshotId=snapshot.Id;
        var config=new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Path.Combine(root,"appsettings.json"))).Build();
        var settings=config.GetSection("EquipmentProgression").Get<EquipmentProgressionOptions>()??new();
        var service=new TowerEquipmentSupplyService(catalog,
            Boundary<IWorldTowerProgressRepository>((m,a)=>m.Name=="HasClearedFloorAsync"?Task.FromResult((int)a[1]!<=highestCleared):throw new InvalidDataException(m.Name)),
            TowerContentProviders.Floors(Path.Combine(root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options),
            Boundary<ICharacterSnapshotRepository>((m,a)=>m.Name=="GetSnapshotByIdAsync"&&(Guid)a[0]! ==snapshot.Id?Task.FromResult<CharacterSnapshot?>(snapshot):throw new InvalidDataException(m.Name)),
            Boundary<IDungeonRunRepository>((m,a)=> {
                if(m.Name!="AddPendingRewardAsync"||!ReferenceEquals(a[0],run))throw new InvalidDataException(m.Name);
                run.PendingRewards.Add((RunReward)a[1]!);return Task.FromResult(true);
            }),metadata,Options.Create(new WorldTowerOptions()),Options.Create(settings));
        await service.CompleteAsync(run,1,ct);var pending=TowerRuntimeCopy.Of(run.PendingRewards.ToArray());
        await service.CompleteAsync(run,1,ct);TowerRuntimeCopy.Equal(pending,run.PendingRewards,"native supply retry");
        var claimed=new SortedDictionary<string,int>();
        var inventory=Boundary<IInventoryService>((m,a)=> {
            if(m.Name!="AddItemsToInventory"||(Guid)a[0]! !=entry.Id)throw new InvalidDataException(m.Name);
            foreach(var i in (IEnumerable<InventoryItem>)a[1]!)
            {
                if(i.InventoryId!=entry.Id||i.Quantity!=1||!i.ItemInstance.IsBound)throw new InvalidDataException("Invalid native supply grant.");
                claimed[i.ItemInstance.ItemBaseId]=claimed.GetValueOrDefault(i.ItemInstance.ItemBaseId)+i.Quantity;
            }
            return Task.CompletedTask;
        });
        var claimer=new DungeonRunRewardClaimer(Boundary<IExperienceRewardWriter>((m,_)=>throw new InvalidDataException("Supply claim may not repeat XP")),
            Boundary<ICurrencyRewardWriter>((m,_)=>Task.CompletedTask),metadata,new InventoryItemFactory(),inventory);
        await claimer.ClaimAsync(run,ct);run.Status=DungeonRunStatus.RewardsClaimed;run.RewardsClaimedAt=DateTimeOffset.UnixEpoch;
        var claimedHash=HarnessJson.Hash(claimed);await claimer.ClaimAsync(run,ct);
        if(HarnessJson.Hash(claimed)!=claimedHash)throw new InvalidDataException("Repeated supply claim.");
        sources.AddClaimedItems(claimed);
        if(claimed.Count==0)return new(pending,claimed,null,null,false,2);
        if(claimed.Count!=1||claimed.Single().Value!=1||recorded.Status!=DungeonRunStatus.Completed)throw new InvalidDataException("Unexpected supply quantity/status.");
        var supply=claimed.Single().Key;var inventoryPolicy=new TowerActivityInventory(root,content);
        var before=inventoryPolicy.Select(owned).Sum(i=>inventoryPolicy.Score(i.Data));
        // Reuse recipe and purchase order. Open the first item that increases the existing inventory score;
        // otherwise retain the earned chest. This is a fixed inventory rule, not a new combat search.
        foreach(var target in Targets(content,catalog,supply,entry.Id,recipe,order))
        {
            var award=catalog.Award(supply,target.State.DefinitionId,entry.Id,
                StableRandom.Guid("tower-next-entry-award-v1",entry.Id.ToString(),run.Id.ToString(),supply),run.Id.ToString());
            if(inventoryPolicy.Select(owned.Append(award).ToArray()).Sum(i=>inventoryPolicy.Score(i.Data))<=before)continue;
            sources.OpenSupply(supply);return new(pending,claimed,award,target.State.DefinitionId,true,2);
        }
        return new(pending,claimed,null,null,false,2);
    }
}
