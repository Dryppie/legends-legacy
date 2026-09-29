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
    private sealed class RetainedQuestClock(DateTimeOffset at):TimeProvider
    {
        public DateTimeOffset At {get;set;}=at;
        public override DateTimeOffset GetUtcNow()=>At;
    }
    public static async Task<TowerFifthResult> RetainTowerFifth(string root,OfflineContent content,TowerReturnOwner owner,TowerQuestCredit credit,string option,CancellationToken ct)
    {
        var before=HarnessJson.Hash(owner);var id=owner.Point.Character.Id;
        if(credit.Owner!=id||credit.History!=owner.History||credit.ForestWins.Length!=6||credit.ActivatedAt>credit.ForestWins[0]
            ||credit.ForestWins.Zip(credit.ForestWins.Skip(1)).Any(p=>p.First>=p.Second)||credit.ForestWins[^1]>credit.ClaimAt
            ||credit.MinesCompletedAt is {} completed&&(completed<credit.ActivatedAt||completed>credit.ForestWins[0])
            ||credit.ClaimAt!=TowerReturnStudy.At(owner.Runtime)||owner.Runtime.Growth.OwnedEssences.Length!=4||owner.Point.Character.Level<40)
            throw new InvalidDataException("Foreign/future quest credit or repeated acquisition.");
        var definitions=new JsonQuestDefinitionProvider(new ConfigurationBuilder().Build(),root,HarnessJson.Options);
        var repo=new RecordingQuestRepository(30); // Earned origin is already 30; this objective is a threshold, not new XP.
        var prerequisite=CreateCompletedProgress(id,definitions.Get(QuestConstants.BetweenDayAndNight));
        prerequisite.CompletedAt=credit.ActivatedAt;
        foreach(var objective in prerequisite.Objectives)objective.CompletedAt=credit.ActivatedAt;
        repo.Progresses.Add(prerequisite);
        var clock=new RetainedQuestClock(credit.ActivatedAt);var loot=new RecordingLootRewardWriter();
        var tokenBase=ItemMetadata(root,"item.essence_token.old_forest").Deserialize<ItemBase>(HarnessJson.Options)!;
        var bases=Boundary<IItemBaseRepository>((m,_)=>m.Name=="GetItemBasesByIdsAsync"
            ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(new Dictionary<string,ItemBase>{{tokenBase.Id,tokenBase}}):throw new InvalidDataException(m.Name));
        var service=new QuestService(repo,definitions,bases,new InventoryItemFactory(),loot,clock);
        await service.GetJournalAsync(id,ct);
        if(credit.MinesCompletedAt is {} mines)
        {
            clock.At=mines;
            await service.ProcessAsync(id,QuestTrigger.DungeonRunCompleted("goblin_mines"),null,"retained-paid-completion",ct);
        }
        foreach(var at in credit.ForestWins)
        {
            clock.At=at;await service.ProcessAsync(id,QuestTrigger.CombatCompleted(TowerLevel40Study.Area,true),null,"retained-old-forest-win",ct);
        }
        clock.At=credit.ClaimAt;var quest=repo.Progresses.Single(p=>p.QuestId==QuestConstants.RootsRemember);
        if(credit.MinesCompletedAt is null)
        {
            var rejected=false;try{await service.TurnInAsync(id,quest.QuestId,ct);}catch(ArgumentException){rejected=true;}
            if(!rejected||loot.GrantedItems.Count!=0)throw new InvalidDataException("Unfunded fifth granted.");
            return new(owner,credit,JsonSerializer.SerializeToElement(quest,HarnessJson.Options),null,
                JsonSerializer.SerializeToElement(new {pendingPaidMines=true,prematureTurnInRejected=true},HarnessJson.Options));
        }
        await service.TurnInAsync(id,quest.QuestId,ct);await service.TurnInAsync(id,quest.QuestId,ct);
        var token=loot.GrantedItems.Single();
        if(token.Quantity!=1||token.ItemInstance.ItemBaseId!=tokenBase.Id)throw new InvalidDataException("Quest reward/retry drift.");
        var selected=ShenicEssenceTokenCatalog.Definitions.Single(t=>t.ItemBaseId==tokenBase.Id).Options.Single(o=>o.Id==option);
        var (essence,receipt,native)=await RetainAbsorption(root,content,owner,credit,token,option,selected.ItemId,ct);
        var journey=TowerJourneyProgression.RestoreRuntime(root,content,owner.Origin,owner.Runtime);
        journey.Growth.RetainAcquisition(receipt,essence);
        await journey.Observe(ct);
        await journey.Event(Application.Interfaces.Services.LL.Prophecies.ProphecyProgressKind.EssenceAbsorbed,1,ct);
        await journey.Claim(ct);
        var after=owner with {Point=owner.Point with {Character=journey.Growth.Snapshot(owner.Point.Character)},Runtime=journey.ExportRuntime()};
        if(HarnessJson.Hash(owner)!=before)throw new InvalidDataException("Acquisition mutated archived state.");
        return new(after,credit,JsonSerializer.SerializeToElement(quest,HarnessJson.Options),receipt,native);
    }
    private static async Task<(PlayerEssence Essence,TowerEssenceAcquisition Receipt,JsonElement Native)> RetainAbsorption(string root,OfflineContent content,TowerReturnOwner owner,TowerQuestCredit credit,InventoryItem token,string option,string itemId,CancellationToken ct)
    {
        var id=owner.Point.Character.Id;var level=owner.Point.Character.Level;
        var factory=new InventoryItemFactory();var bag=new List<InventoryItem>();
        bag.Add(token);
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
        int notifications=0;
        var publisher=Boundary<MediatR.IPublisher>((m,a)=> {
            if(m.Name!="Publish"||a[0] is not Application.UseCases.Prophecies.Events.ProphecyProgressNotification p
                ||p.ProgressEvent.Kind!=Application.Interfaces.Services.LL.Prophecies.ProphecyProgressKind.EssenceAbsorbed)throw new InvalidDataException("Unexpected absorption event.");
            notifications++;return Task.CompletedTask;
        });
        var essences=new EssenceSystemService(repository,inventoryRepo,bases,content.Essences,new JsonCreatureEssenceLootTableRepository(new ConfigurationBuilder().Build(),root,HarnessJson.Options,content.Essences),
            new EssenceProgressionService(),new EssenceSlotUnlockService(),new EssenceLoadoutLimitService(),factory,Boundary<IRandomProvider>(),
            Boundary<IGameEventOutbox>((m,_)=>m.Name=="EnqueueAsync"?Task.CompletedTask:throw new InvalidDataException(m.Name)),
            Boundary<IGuildMissionService>((m,_)=>m.Name=="RecordContributionAsync"?Task.FromResult(new GuildContributionResult(false,false,0,0)):throw new InvalidDataException(m.Name)),publisher);
        var unbound=opened.Rewards.Single();var absorbed=await essences.AbsorbUnboundEssenceAsync(id,unbound.ItemInstanceId,ct);
        var twice=await essences.AbsorbUnboundEssenceAsync(id,unbound.ItemInstanceId,ct);
        if(!absorbed.Succeeded||twice.Succeeded||owned.Count!=5||unbound.Quantity!=0)throw new InvalidDataException("Absorption/retry failed.");
        var slots=owned.Select((e,i)=>new SaveEssenceLoadoutSlotRequest(i,e.Id)).ToArray();
        level=39;var locked=await essences.SaveLoadoutAsync(id,new(null,"probe-locked",slots),ct);
        level=40;var saved=await essences.SaveLoadoutAsync(id,new(null,"probe-five",slots),ct);
        if(locked.Succeeded||!saved.Succeeded)throw new InvalidDataException("Native slot/creature gate failed.");
        if(notifications!=1)throw new InvalidDataException("Missing/repeated absorption event.");
        var e=owned[^1];var receipt=new TowerEssenceAcquisition(id,"quest.shenic.roots_remember",token.ItemInstance.ItemBaseId,option,itemId,
            token.ItemInstanceId,unbound.ItemInstanceId,e.Id,e.EssenceDefinitionId,credit.ClaimAt,HarnessJson.Hash(credit));
        var native=JsonSerializer.SerializeToElement(new {tokenConsumed=true,duplicateOpenRejected=true,unboundConsumed=true,duplicateAbsorptionRejected=true,
            lockedAt39=true,acceptedAt40=true,absorptionNotifications=notifications,newEssenceLevel=e.Level,newEssenceXp=e.CurrentXp,
            savedOwnedIds=slots.Select(s=>s.PlayerEssenceId).ToArray()},HarnessJson.Options);
        return (e,receipt,native);
    }
}

