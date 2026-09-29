using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Services.LL.Rewards;
using Application.UseCases.Prophecies.Events;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using MediatR;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Essences;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;
using Services.LL.JsonDefinitions;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Rewards;
using static BalanceHarness.TowerGrowthProgression;

namespace BalanceHarness;

/// <summary>Disposable native integration controls. Neither random probe loot nor hypothetical XP becomes retained state.</summary>
public static class TowerCoreSourceProbe
{
    public static async Task<object> Completions(string root,CancellationToken ct)
    {
        var rewards=new JsonRewardTableDefinitionProvider(new ConfigurationBuilder().Build(),root,HarnessJson.Options,new RewardTableDefinitionValidator());
        var definitions=new JsonDungeonDefinitions(new JsonDocumentReader<DungeonCatalogDocument>(root,"Data/dungeons/dungeons.json",HarnessJson.Options),new(new()),new DungeonDefinitionValidator(),rewards);
        var item=HarnessJson.Read<JsonElement>(Path.Combine(root,"Data/items/items.json")).EnumerateArray()
            .Single(e=>e.GetProperty("id").GetString()==EssenceProgressionConstants.LesserMonsterCoreItemId).Deserialize<ItemBase>(HarnessJson.Options)!;
        var bases=Boundary<IItemBaseRepository>((m,a)=>m.Name=="GetItemBasesByIdsAsync"
            ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(((IReadOnlyCollection<string>)a[0]!).ToDictionary(id=>id,id=>id==item.Id?item:throw new InvalidDataException("Unexpected completion item.")))
            :throw new InvalidDataException(m.Name));
        var completed=new HashSet<string>();DungeonRun run=null!;var bag=new List<InventoryItem>();var events=new List<ProphecyProgressNotification>();int marks=0;
        Task<bool> AddPending(object?[] a){if(!ReferenceEquals(a[0],run))throw new InvalidDataException("Foreign pending run.");run.PendingRewards.Add((RunReward)a[1]!);return Task.FromResult(true);}
        Task Mark(object?[] a){if((Guid)a[0]!=run.CharacterId||(string)a[1]!=run.DungeonDefinitionId||(DateTimeOffset)a[2]!=run.CompletedAt)throw new InvalidDataException("Completion identity drift.");completed.Add(run.DungeonDefinitionId);marks++;return Task.CompletedTask;}
        var repo=Boundary<IDungeonRunRepository>((m,a)=>m.Name switch {
            "HasCompletedDungeonAsync"=>Task.FromResult(completed.Contains((string)a[1]!)),
            "GetDungeonRunByDungeonIdAsync"=>Task.FromResult<DungeonRun?>((Guid)a[0]==run.Id?run:null),
            "AddPendingRewardAsync"=>AddPending(a),"MarkDungeonCompletedAsync"=>Mark(a),_=>throw new InvalidDataException(m.Name)});
        var publisher=Boundary<IPublisher>((m,a)=>{if(m.Name!="Publish"||a[0] is not ProphecyProgressNotification e)throw new InvalidDataException(m.Name);events.Add(e);return Task.CompletedTask;});
        var mastery=Boundary<IDungeonMasteryService>((m,a)=>m.Name=="AwardRunMasteryAsync"&&ReferenceEquals(a[0],run)
            ?Task.FromResult(new DungeonMasteryAwardResult(run.DungeonDefinitionId,0,0,0,0,0,[],true)):throw new InvalidDataException(m.Name));
        var factory=new InventoryItemFactory();
        var applier=new DungeonCompletionRewardApplier(definitions,repo,bases,Boundary<IRewardRoller>(),new DungeonPendingRewardWriter(repo),factory,mastery,publisher);
        var inventory=Boundary<IInventoryService>((m,a)=>{
            if(m.Name!="AddItemsToInventory"||(Guid)a[0]!=run.CharacterId)throw new InvalidDataException("Foreign claim.");
            var incoming=(List<InventoryItem>)a[1]!;if(incoming.Any(i=>i.InventoryId!=run.CharacterId))throw new InvalidDataException("Wrong inventory owner.");
            bag.AddRange(incoming);return Task.CompletedTask;});
        var claimer=new DungeonRunRewardClaimer(Boundary<IExperienceRewardWriter>(),Boundary<ICurrencyRewardWriter>(),bases,factory,inventory);
        var results=new List<object>();
        foreach(var family in new[] {"forgotten_catacombs","goblin_mines"})
        {
            var d=definitions.GetByKey(family);var grant=DungeonRewardCatalog.GetFirstCompletionGrants(d).Single();
            if(d.Grade!=DungeonGrade.GradeI||d.Region!=1||d.EntryCosts.Count!=1||d.EntryCosts[0].Amount!=1||d.EntryCosts[0].ItemId!="sigil_"+family
                ||d.CompletionRewardTableIds.Count!=0||d.TierRewardTableIds.Count!=0||d.RewardTable.CompletionRewards.Count!=0
                ||grant.ItemId!=item.Id||grant.MinAmount!=6||grant.MaxAmount!=6||grant.Chance!=1)
                throw new InvalidDataException("Changed grade-I source rules.");
            for(var ordinal=0;ordinal<2;ordinal++)
            {
                run=new() {Id=Guid.NewGuid(),CharacterId=Guid.NewGuid(),DungeonDefinitionId=family,Status=DungeonRunStatus.Completed,CompletedAt=TowerJourneyProgression.Epoch.AddMinutes(results.Count+1)};
                // Both runs in a family belong to one disposable owner, so the second is a repeat.
                run.CharacterId=Common.Randomness.StableRandom.Guid("tower-core-probe-owner",family);
                bag.Clear();events.Clear();await applier.ApplyAsync(run,ct);
                var pending=run.PendingRewards.Select(r=>new {r.ItemId,r.Quantity,r.Source}).ToArray();var total=pending.Sum(p=>p.Quantity);var first=ordinal==0?6:0;
                if(total<3+first||total>6+first||pending.Count(p=>p.Source=="Grade I First Completion")!=(ordinal==0?1:0)
                    ||pending.Where(p=>p.Source=="Grade I First Completion").Sum(p=>p.Quantity)!=first
                    ||events.Count!=1||events[0].ProgressEvent.Amount!=total||events[0].ProgressEvent.OccurredAt!=run.CompletedAt)
                    throw new InvalidDataException("Native core/treasure range or first-completion drift.");
                var items=await claimer.ClaimAsync(run,ct);
                if(items.Sum(i=>i.Quantity)!=total||bag.Sum(i=>i.Quantity)!=total)throw new InvalidDataException("Native core claim lost rewards.");
                // The run service owns this persisted guard. Do not reset an old claimed run to supplement it.
                run.RewardsClaimedAt=run.CompletedAt;run.Status=DungeonRunStatus.RewardsClaimed;
                if((await claimer.ClaimAsync(run,ct)).Count!=0||bag.Sum(i=>i.Quantity)!=total)throw new InvalidDataException("Repeated claim duplicated cores.");
                results.Add(new {family,ordinal,firstCompletion=ordinal==0,pending,total,treasureProgress=events[0].ProgressEvent.Amount,
                    claimed=bag.Sum(i=>i.Quantity),duplicateClaimRejected=true,probeRetained=false});
            }
        }
        return new {results,completionMarks=marks,nativeRandomDraws="fresh disposable Random.Shared draws; not historical observations or a distribution estimate",probeRetained=false};
    }
    public static async Task<object> Ascension(string root,OfflineContent content,TowerReturnOwner owner,CancellationToken ct)
    {
        var before=HarnessJson.Hash(owner);var id=owner.Point.Character.Id;
        var owned=owner.Runtime.Growth.OwnedEssences.Select(e=>new PlayerEssence {Id=e.Id,CharacterId=id,EssenceDefinitionId=e.Definition,Level=e.Level,CurrentXp=e.CurrentXp,AscensionTier=e.AscensionTier}).ToList();
        if(owned.Count is <1 or >5||owned.Any(e=>e.AscensionTier!=0||e.Level>=10))throw new InvalidDataException("Unexpected current ascension cohort.");
        var stock=30;int debits=0,notifications=0;
        var repo=Boundary<IEssenceRepository>((m,a)=>m.Name switch {
            "GetPlayerEssenceAsync"=>Task.FromResult(owned.SingleOrDefault(e=>(Guid)a[0]==e.CharacterId&&(Guid)a[1]==e.Id)),
            "GetPlayerEssencesAsync" when (Guid)a[0]==id=>Task.FromResult(owned),_=>throw new InvalidDataException(m.Name)});
        var inventory=Boundary<IInventoryRepository>((m,a)=>{
            if(m.Name!="TryRemoveItemsByBaseIdAsync"||(Guid)a[0]!=id)throw new InvalidDataException("Foreign core debit.");
            var required=(Dictionary<string,int>)a[1]!;
            if(required.Count!=1||required.GetValueOrDefault(EssenceProgressionConstants.LesserMonsterCoreItemId)!=6)throw new InvalidDataException("Unexpected ascension cost.");
            if(stock<6)return Task.FromResult(false);stock-=6;debits++;return Task.FromResult(true);
        });
        var outbox=Boundary<IGameEventOutbox>((m,a)=>{if(m.Name!="EnqueueAsync")throw new InvalidDataException(m.Name);notifications++;return Task.CompletedTask;});
        var progression=new EssenceProgressionService();
        var service=new EssenceSystemService(repo,inventory,Boundary<IItemBaseRepository>(),content.Essences,Boundary<ICreatureEssenceLootTableRepository>(),progression,
            new EssenceSlotUnlockService(),new EssenceLoadoutLimitService(),new InventoryItemFactory(),Boundary<IRandomProvider>(),outbox,Boundary<IGuildMissionService>());
        if((await service.AscendEssenceAsync(Guid.NewGuid(),owned[0].Id,ct)).Succeeded||stock!=30)throw new InvalidDataException("Cross-owner ascension.");
        var rows=new List<object>();
        foreach(var (e,saved) in owned.Zip(owner.Runtime.Growth.OwnedEssences))
        {
            stock=30;var premature=await service.AscendEssenceAsync(id,e.Id,ct);
            if(premature.Succeeded||stock!=30)throw new InvalidDataException("Untrained ascension accepted.");
            var xp=TowerCoreSourceStudy.XpToFirstAscension(saved);var definition=content.Essences.GetById(e.EssenceDefinitionId)!;
            var almost=progression.GrantXp(e,definition,xp-1);
            var justShort=await service.AscendEssenceAsync(id,e.Id,ct);
            if(e.Level!=9||justShort.Succeeded||stock!=30||almost.XpGained!=xp-1)throw new InvalidDataException("Training boundary drift.");
            var final=progression.GrantXp(e,definition,1);
            if(e.Level!=10||e.CurrentXp!=0||final.XpGained!=1)throw new InvalidDataException("Exact first ascension XP drift.");
            stock=5;var insufficient=await service.AscendEssenceAsync(id,e.Id,ct);
            if(insufficient.Succeeded||stock!=5||e.AscensionTier!=0)throw new InvalidDataException("Unfunded ascension accepted.");
            stock=6;var funded=await service.AscendEssenceAsync(id,e.Id,ct);
            if(!funded.Succeeded||stock!=0||e.AscensionTier!=1)throw new InvalidDataException("Funded native ascension failed.");
            var retry=await service.AscendEssenceAsync(id,e.Id,ct);
            if(retry.Succeeded||stock!=0||e.Level!=10||e.CurrentXp!=0)throw new InvalidDataException("Ascension retry changed state.");
            rows.Add(new {saved.Id,saved.Definition,saved.Level,saved.CurrentXp,xpRequired=xp,
                currentRejected=true,oneXpShortRejected=true,fiveCoresRejected=true,sixCoresConsumed=6,ascensionTier=e.AscensionTier,retryRejected=true});
        }
        if(debits!=owned.Count||notifications!=owned.Count||before!=HarnessJson.Hash(owner))throw new InvalidDataException("Probe conservation failed.");
        return new {essences=rows,foreignOwnerRejected=true,debits,notifications,probeRetained=false,
            xpSource="hypothetical boundary probe only; no earned activity or player time credited",retainedOwnerHash=before};
    }
}
