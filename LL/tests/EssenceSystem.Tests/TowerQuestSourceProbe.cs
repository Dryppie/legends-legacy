using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Inventories.SelectionCrates;
using BalanceHarness;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Domain.Models.Quests;
using Microsoft.Extensions.Configuration;
using Services.LL.Essences;
using Services.LL.Inventories;
using Services.LL.Interfaces;
using Services.LL.Quests;
using static BalanceHarness.TowerGrowthProgression;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    public static async Task<JsonElement> ProbeTowerQuestSource(string root,OfflineContent content,TowerReturnOwner owner,int victories,CancellationToken ct)
    {
        var pin=HarnessJson.Hash(owner);var id=owner.Point.Character.Id;
        var definitions=new JsonQuestDefinitionProvider(new ConfigurationBuilder().Build(),root,HarnessJson.Options);
        var repo=new RecordingQuestRepository(owner.Point.Character.Level);
        // Explicit prospective scenario. This is not restoration of absent historical quest records.
        repo.Progresses.Add(CreateCompletedProgress(id,definitions.Get(QuestConstants.BetweenDayAndNight)));
        var loot=new RecordingLootRewardWriter();var clock=new ProbeClock(TowerReturnStudy.At(owner.Runtime));
        var tokenBase=ItemMetadata(root,"item.essence_token.old_forest").Deserialize<ItemBase>(HarnessJson.Options)!;
        var questItems=Boundary<IItemBaseRepository>((m,_)=>m.Name=="GetItemBasesByIdsAsync"
            ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(new Dictionary<string,ItemBase>{{tokenBase.Id,tokenBase}}):throw new InvalidDataException(m.Name));
        var service=new QuestService(repo,definitions,questItems,new InventoryItemFactory(),loot,clock);
        await service.GetJournalAsync(id,ct);
        var quest=repo.Progresses.Single(p=>p.QuestId==QuestConstants.RootsRemember);
        for(var i=0;i<Math.Min(victories,6);i++)
            await service.ProcessAsync(id,QuestTrigger.CombatCompleted(TowerLevel40Study.Area,true),null,"conditional-new-idle",ct);
        object Snapshot()=>new {quest.QuestId,status=quest.Status.ToString(),objectives=quest.Objectives.Select(o=>new {o.ObjectiveKey,o.CurrentAmount,o.RequiredAmount,complete=o.CompletedAt.HasValue}).ToArray()};
        var pending=JsonSerializer.SerializeToElement(Snapshot(),HarnessJson.Options);
        bool rejected=false;try{await service.TurnInAsync(id,quest.QuestId,ct);}catch(ArgumentException){rejected=true;}
        if(!rejected||loot.GrantedItems.Count!=0)throw new InvalidDataException("Unfunded quest turn-in.");
        // Wrong-family completion is a disposable negative control, followed by a hypothetical Mines completion.
        await service.ProcessAsync(id,QuestTrigger.DungeonRunCompleted("forgotten_catacombs"),null,"probe-only",ct);
        if(HarnessJson.Hash(pending)!=HarnessJson.Hash(Snapshot()))throw new InvalidDataException("Wrong dungeon advanced quest.");
        await service.ProcessAsync(id,QuestTrigger.DungeonRunCompleted("goblin_mines"),null,"probe-only",ct);
        await service.TurnInAsync(id,quest.QuestId,ct);await service.TurnInAsync(id,quest.QuestId,ct);
        var awarded=loot.GrantedItems.Single();
        if(awarded.ItemInstance.ItemBaseId!="item.essence_token.old_forest"||awarded.Quantity!=1)throw new InvalidDataException("Quest reward/retry changed.");
        var token=ShenicEssenceTokenCatalog.Definitions.Single(t=>t.ItemBaseId==awarded.ItemInstance.ItemBaseId);
        var candidates=new List<object>();
        foreach(var option in token.Options)
        {
            var already=owner.Runtime.Growth.OwnedEssences.Any(e=>e.Definition=="essence."+option.Id);
            if(already){candidates.Add(new {option.Id,option.ItemId,alreadyOwned=true,eligibleFifth=false});continue;}
            var result=await ProbeAbsorption(root,content,owner,option.Id,option.ItemId,ct);
            candidates.Add(new {option.Id,option.ItemId,alreadyOwned=false,eligibleFifth=true,native=result});
        }
        if(HarnessJson.Hash(owner)!=pin)throw new InvalidDataException("Disposable source probe mutated owner.");
        return JsonSerializer.SerializeToElement(new {owner=id,initialQuestStateAssumed=true,pending,prematureTurnInRejected=rejected,wrongFamilyRejected=true,
            hypotheticalMinesTurnInTokenQuantity=awarded.Quantity,duplicateTurnInGrantedNothing=true,candidates,
            remainingMinesSigils=owner.Runtime.Sources.State.Items.GetValueOrDefault("sigil_goblin_mines"),
            entryCost=new Dictionary<string,int>{{"sigil_goblin_mines",1}},needsFreshPaidMinesCompletion=true,
            probeRetained=false,newEssencesGranted=0,historicalDungeonCredit=0,historicalResonanceReset=false},HarnessJson.Options);
    }
    private sealed class ProbeClock(DateTimeOffset at):TimeProvider {public override DateTimeOffset GetUtcNow()=>at;}
    private static JsonElement ItemMetadata(string root,string id)
    {
        var items=HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/items/items.json"));
        return (items.ValueKind==JsonValueKind.Array?items.EnumerateArray():items.GetProperty("items").EnumerateArray()).Single(i=>i.GetProperty("id").GetString()==id);
    }
    private static async Task<object> ProbeAbsorption(string root,OfflineContent content,TowerReturnOwner owner,string option,string itemId,CancellationToken ct)
    {
        var id=owner.Point.Character.Id;var level=owner.Point.Character.Level;
        var factory=new InventoryItemFactory();var bag=new List<InventoryItem>();
        var token=factory.Create(ItemMetadata(root,"item.essence_token.old_forest").Deserialize<ItemBase>(HarnessJson.Options)!,1,id);bag.Add(token);
        var item=ItemMetadata(root,itemId).Deserialize<EssenceItemBase>(HarnessJson.Options)!;
        var bases=Boundary<IItemBaseRepository>((m,a)=>m.Name=="GetItemBasesByIdsAsync"?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(new Dictionary<string,ItemBase>{{itemId,item}}):throw new InvalidDataException(m.Name));
        InventoryItem? Find(object?[] a)=>bag.SingleOrDefault(i=>(Guid)a[0]! == id&&i.ItemInstanceId==(Guid)a[1]!&&i.Quantity>0);
        Task<bool> Consume(object?[] a){var i=Find(a);if(i is null)return Task.FromResult(false);i.Quantity--;return Task.FromResult(true);}
        Task Add(object?[] a){bag.AddRange((IEnumerable<InventoryItem>)a[1]!);return Task.CompletedTask;}
        var inventory=Boundary<IInventoryService>((m,a)=>m.Name switch {
            "GetInventoryItemAsync"=>Task.FromResult(Find(a)),"TryConsumeInventoryItemAsync"=>Consume(a),"AddItemsToInventory"=>Add(a),_=>throw new InvalidDataException(m.Name)});
        var opener=new SelectionCrateService(inventory,bases,factory);var opened=await opener.OpenSelectionContainerAsync(id,token.ItemInstanceId,option,ct);
        var retry=await opener.OpenSelectionContainerAsync(id,token.ItemInstanceId,option,ct);
        if(!opened.IsSuccess||retry.IsSuccess||opened.Rewards.Count!=1||token.Quantity!=0)throw new InvalidDataException("Token opening failed/repeated.");
        var owned=owner.Runtime.Growth.OwnedEssences.Select(e=>new PlayerEssence {Id=e.Id,CharacterId=id,EssenceDefinitionId=e.Definition,Level=e.Level,CurrentXp=e.CurrentXp}).ToList();
        var loadouts=new List<EssenceLoadout>();
        Task AddEssence(PlayerEssence e){owned.Add(e);return Task.CompletedTask;}
        Task AddLoadout(EssenceLoadout l){loadouts.Add(l);return Task.CompletedTask;}
        Task Bind(object?[] a){foreach(var s in (IEnumerable<EssenceLoadoutSlot>)a[1]!)s.PlayerEssence=owned.Single(e=>e.Id==s.PlayerEssenceId);return Task.CompletedTask;}
        var repository=Boundary<IEssenceRepository>((m,a)=>m.Name switch {
            "HasPlayerEssenceAsync"=>Task.FromResult(owned.Any(e=>e.EssenceDefinitionId==(string)a[1]!)),"GetPlayerEssencesAsync"=>Task.FromResult(owned),
            "AddPlayerEssenceAsync"=>AddEssence((PlayerEssence)a[0]!),"GetCharacterLevelAsync"=>Task.FromResult(level),
            "CountOwnedPlayerEssencesAsync"=>Task.FromResult(((IReadOnlyCollection<Guid>)a[1]!).Count(g=>owned.Any(e=>e.Id==g))),
            "HasLoadoutNameAsync"=>Task.FromResult(false),"GetLoadoutsWithSlotsAsync"=>Task.FromResult(loadouts),"AddLoadoutAsync"=>AddLoadout((EssenceLoadout)a[0]!),
            "ReplaceLoadoutSlotsAsync"=>Bind(a),_=>throw new InvalidDataException(m.Name)});
        var inventoryRepo=Boundary<IInventoryRepository>((m,a)=>m.Name switch {"GetInventoryItemAsync"=>Task.FromResult(Find(a)),"RemoveInventoryItem"=>null,_=>throw new InvalidDataException(m.Name)});
        var essences=new EssenceSystemService(repository,inventoryRepo,bases,content.Essences,new JsonCreatureEssenceLootTableRepository(new ConfigurationBuilder().Build(),root,HarnessJson.Options,content.Essences),
            new EssenceProgressionService(),new EssenceSlotUnlockService(),new EssenceLoadoutLimitService(),factory,Boundary<IRandomProvider>(),
            Boundary<IGameEventOutbox>((m,_)=>m.Name=="EnqueueAsync"?Task.CompletedTask:throw new InvalidDataException(m.Name)),
            Boundary<IGuildMissionService>((m,_)=>m.Name=="RecordContributionAsync"?Task.FromResult(new GuildContributionResult(false,false,0,0)):throw new InvalidDataException(m.Name)));
        var unbound=opened.Rewards.Single();var absorbed=await essences.AbsorbUnboundEssenceAsync(id,unbound.ItemInstanceId,ct);
        var twice=await essences.AbsorbUnboundEssenceAsync(id,unbound.ItemInstanceId,ct);
        if(!absorbed.Succeeded||twice.Succeeded||owned.Count!=5||unbound.Quantity!=0)throw new InvalidDataException("Absorption/retry failed.");
        var slots=owned.Select((e,i)=>new SaveEssenceLoadoutSlotRequest(i,e.Id)).ToArray();
        level=39;var locked=await essences.SaveLoadoutAsync(id,new(null,"probe-locked",slots),ct);
        level=40;var saved=await essences.SaveLoadoutAsync(id,new(null,"probe-five",slots),ct);
        if(locked.Succeeded||!saved.Succeeded)throw new InvalidDataException("Native slot/creature gate failed.");
        loadouts.Single().AutoUseActivities=EssenceCombatActivity.IdleCombat;
        await essences.GrantCombatXpToAttunedEssencesAsync(id,1,EssenceCombatActivity.IdleCombat,ct);
        if(owned[^1].Level!=1||owned[^1].CurrentXp!=1)throw new InvalidDataException("New Essence failed native training.");
        return new {tokenConsumed=true,duplicateOpenRejected=true,unboundConsumed=true,duplicateAbsorptionRejected=true,lockedAt39=true,acceptedAt40=true,
            newEssenceLevel=1,probeTrainingExperience=1,probeRetained=false};
    }
}
