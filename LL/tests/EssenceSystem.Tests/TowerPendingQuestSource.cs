using System.Text.Json;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Inventories.SelectionCrates;
using BalanceHarness;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Quests;
using Microsoft.Extensions.Configuration;
using Services.LL.Inventories;
using Services.LL.Interfaces;
using Services.LL.Quests;
using static BalanceHarness.TowerGrowthProgression;

namespace EssenceSystem.Tests;

public sealed partial class QuestSystemTests
{
    public static async Task<TowerFifthResult> ResumeTowerPendingFifth(string root,OfflineContent content,TowerFifthResult pending,TowerReturnOwner after,
        DungeonAcquisitionRun run,TowerJourneyDungeonReceipt completion,string file,string hash,string option,CancellationToken ct)
    {
        var original=HarnessJson.Hash(pending);var id=after.Point.Character.Id;
        if(pending.Acquisition is not null||pending.Owner.Point.Character.Id!=id||pending.Owner.History!=after.History||after.Runtime.Growth.OwnedEssences.Length!=4
            ||run.Character!=pending.Owner.Point.Character.Name||run.Dungeon!="goblin_mines"||run.SigilsConsumed!=1
            ||run.EntryItems.Count!=1||run.EntryItems.GetValueOrDefault("sigil_goblin_mines")!=1
            ||pending.Owner.Runtime.Sources.State.Items.GetValueOrDefault("sigil_goblin_mines")-after.Runtime.Sources.State.Items.GetValueOrDefault("sigil_goblin_mines")!=1
            ||completion.Started!=TowerReturnStudy.At(pending.Owner.Runtime)||completion.Ended!=TowerReturnStudy.At(after.Runtime)||completion.Ended<completion.Started)
            throw new InvalidDataException("Foreign, unpaid or repeated pending quest completion.");
        var quest=pending.Quest.Deserialize<CharacterQuestProgress>(HarnessJson.Options)!;
        if(quest.CharacterId!=id||quest.QuestId!=QuestConstants.RootsRemember||quest.Status!=QuestStatus.Active||quest.RewardsGrantedAt.HasValue
            ||quest.Objectives.Count!=3||quest.Objectives.Single(o=>o.ObjectiveKey=="break_the_goblin_gate").CurrentAmount!=0
            ||quest.Objectives.Where(o=>o.ObjectiveKey!="break_the_goblin_gate").Any(o=>!o.CompletedAt.HasValue||o.CompletedAt>completion.Started))
            throw new InvalidDataException("Invalid persisted pending quest.");
        if(run.Status!=DungeonRunStatus.Completed)
        {
            if(run.Status!=DungeonRunStatus.Failed||run.CompletionCallbacks!=0||completion.AppliedExperience!=0)throw new InvalidDataException("Invalid failed run.");
            return pending with {Owner=after};
        }
        if(run.CompletionCallbacks!=1||completion.AppliedExperience<=0)throw new InvalidDataException("Unclaimed completion cannot grant quest credit.");
        var definitions=new JsonQuestDefinitionProvider(new ConfigurationBuilder().Build(),root,HarnessJson.Options);var repo=new RecordingQuestRepository(after.Point.Character.Level);
        var prerequisite=CreateCompletedProgress(id,definitions.Get(QuestConstants.BetweenDayAndNight));prerequisite.CompletedAt=pending.Credit.ActivatedAt;
        foreach(var o in prerequisite.Objectives)o.CompletedAt=pending.Credit.ActivatedAt;
        repo.Progresses.Add(prerequisite);repo.Progresses.Add(quest);
        var clock=new RetainedQuestClock(completion.Ended);var loot=new RecordingLootRewardWriter();var tokenBase=ItemMetadata(root,"item.essence_token.old_forest").Deserialize<ItemBase>(HarnessJson.Options)!;
        var bases=Boundary<IItemBaseRepository>((m,_)=>m.Name=="GetItemBasesByIdsAsync"
            ?Task.FromResult<IReadOnlyDictionary<string,ItemBase>>(new Dictionary<string,ItemBase>{{tokenBase.Id,tokenBase}}):throw new InvalidDataException(m.Name));
        var service=new QuestService(repo,definitions,bases,new InventoryItemFactory(),loot,clock);
        await service.ProcessAsync(id,QuestTrigger.DungeonRunCompleted("goblin_mines"),null,"new-paid-mines-completion",ct);
        await service.TurnInAsync(id,quest.QuestId,ct);await service.TurnInAsync(id,quest.QuestId,ct);
        var token=loot.GrantedItems.Single();if(token.Quantity!=1||token.ItemInstance.ItemBaseId!=tokenBase.Id)throw new InvalidDataException("Quest reward replay/drift.");
        var credit=pending.Credit with {RunFile=file,RunHash=hash,MinesCompletedAt=completion.Ended,ClaimAt=completion.Ended};
        var selected=ShenicEssenceTokenCatalog.Definitions.Single(t=>t.ItemBaseId==tokenBase.Id).Options.Single(o=>o.Id==option);
        var (essence,receipt,native)=await RetainAbsorption(root,content,after,credit,token,option,selected.ItemId,ct);
        var journey=TowerJourneyProgression.RestoreRuntime(root,content,after.Origin,after.Runtime);journey.Growth.RetainAcquisition(receipt,essence);
        await journey.Observe(ct);await journey.Event(Application.Interfaces.Services.LL.Prophecies.ProphecyProgressKind.EssenceAbsorbed,1,ct);await journey.Claim(ct);
        var retained=after with {Point=after.Point with {Character=journey.Growth.Snapshot(after.Point.Character)},Runtime=journey.ExportRuntime()};
        if(HarnessJson.Hash(pending)!=original)throw new InvalidDataException("Prior quest receipt mutated.");
        return new(retained,credit,JsonSerializer.SerializeToElement(quest,HarnessJson.Options),receipt,native);
    }
}
