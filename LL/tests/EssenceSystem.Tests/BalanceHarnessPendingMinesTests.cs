using System.Text.Json;
using BalanceHarness;
using Domain.Models.Dungeons.Runs;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessPendingMinesTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static async Task<(OfflineContent Content,TowerFifthResult Pending,TowerReturnOwner After,DungeonAcquisitionRun Run,TowerJourneyDungeonReceipt Receipt)> Setup(bool success)
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var j=new TowerJourneyProgression(Root,content,c,true);j.Growth.Attune(4);
        var xp=new Services.LL.Levels.JsonCharacterExperienceProgressionProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),Root,HarnessJson.Options);
        await j.Growth.AwardIdle(checked((int)Enumerable.Range(30,10).Sum(l=>xp.GetRequiredExperience(l))),default);
        j.WaitUntil(TowerJourneyProgression.Epoch.AddDays(1));await j.Observe(default);var r=j.ExportRuntime();
        r=r with {Sources=r.Sources with {ResourcesReconciled=true,State=r.Sources.State with {Items=new Dictionary<string,int>{{"sigil_goblin_mines",1}}}}};
        var p=new TowerEarnedPoint("unit","earned-progression","perfect","guardian",0,25920,25920,0,j.Now,j.Growth.Snapshot(c),c.Equipment.Select(e=>e.Data).ToArray());
        var o=new TowerReturnOwner("unit",c,p,r,[],"unit","unit");
        var credit=new TowerQuestCredit(c.Id,"unit",1,TowerJourneyProgression.Epoch.AddSeconds(10),new string('a',64),null,null,null,
            Enumerable.Range(1,6).Select(i=>TowerJourneyProgression.Epoch.AddHours(i)).ToArray(),j.Now,"unit scenario");
        var pending=await QuestSystemTests.RetainTowerFifth(Root,content,o,credit,"thornback_boar",default);
        j=TowerJourneyProgression.RestoreRuntime(Root,content,c,r);j.Sources.SpendEntry(new Dictionary<string,int>{{"sigil_goblin_mines",1}});j.AdvanceCombat(10);
        var after=o with {Runtime=j.ExportRuntime(),Point=o.Point with {AvailableAt=j.Now}};
        var run=new DungeonAcquisitionRun(c.Name,"goblin_mines",1,success?DungeonRunStatus.Completed:DungeonRunStatus.Failed,null,success?1:0,1,new Dictionary<string,int>{{"sigil_goblin_mines",1}},[],[],[],JsonSerializer.SerializeToElement(new {}),10,1);
        var receipt=new TowerJourneyDungeonReceipt(TowerReturnStudy.At(r),j.Now,0,[],success?1:0,success?0:1,success?1:0,0,0,null!);
        return(content,pending,after,run,receipt);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pending_quest_requires_new_paid_success_after_the_saved_forest_wins(bool success)
    {
        var(content,pending,after,run,receipt)=await Setup(success);var before=HarnessJson.Hash(pending);
        var r=await QuestSystemTests.ResumeTowerPendingFifth(Root,content,pending,after,run,receipt,"run.json.gz",new string('a',64),"thornback_boar",default);
        Assert.Equal(before,HarnessJson.Hash(pending));Assert.Equal(success,r.Acquisition is not null);
        if(success)
        {
            Assert.Equal(receipt.Ended,r.Credit.MinesCompletedAt);Assert.Equal(0,r.Owner.Runtime.Growth.OwnedEssences[4].CurrentXp);
            Assert.Equal(pending.Quest.GetProperty("objectives")[0].GetProperty("completedAt").GetDateTimeOffset(),r.Quest.GetProperty("objectives")[0].GetProperty("completedAt").GetDateTimeOffset());
            Assert.Equal(r.Acquisition!.EssenceId,r.Owner.Point.Character.MaterializeEssences()[4].Id);
        }
        else Assert.Equal(HarnessJson.Hash(pending.Quest),HarnessJson.Hash(r.Quest));
        foreach(var invalid in new[] {run with {SigilsConsumed=0},run with {Dungeon="forgotten_catacombs"},run with {Character="foreign"}})
            await Assert.ThrowsAsync<InvalidDataException>(()=>QuestSystemTests.ResumeTowerPendingFifth(Root,content,pending,after,invalid,receipt,"run",new string('a',64),"thornback_boar",default));
        await Assert.ThrowsAsync<InvalidDataException>(()=>QuestSystemTests.ResumeTowerPendingFifth(Root,content,pending,pending.Owner,run,receipt,"run",new string('a',64),"thornback_boar",default));
    }
    [Fact]
    public async Task Current_level40_stock_passes_native_grade_one_entry_without_debit()
    {
        var(content,pending,_,_,_)=await Setup(true);var o=pending.Owner;var before=HarnessJson.Hash(o);
        var preview=JsonSerializer.SerializeToElement(await TowerUpgradeEntryStudy.Entry(Root,content,o.Point,o.Runtime.Sources.State.Items,4,TowerReturnStudy.At(o.Runtime),default),HarnessJson.Options);
        Assert.Equal("Enter",preview.GetProperty("reason").GetString());Assert.Equal("goblin_mines",preview.GetProperty("selectedDungeon").GetString());
        Assert.Equal(0,preview.GetProperty("inventoryAfterHypotheticalEntry").GetProperty("sigil_goblin_mines").GetInt32());Assert.Equal(before,HarnessJson.Hash(o));
    }
    private sealed class OwnedFactAttribute:FactAttribute {public OwnedFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_PENDING_MINES")))Skip="Requires a frozen pending Mines owner.";}}
    [OwnedFact]
    public async Task Frozen_pending_mines()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(840));var q=HarnessJson.Read<TowerPendingMinesRequest>(Environment.GetEnvironmentVariable("LL_TOWER_PENDING_MINES")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        if(q.Mode=="prepare")await TowerPendingMinesStudy.Prepare(q,deadline.Token);
        else await TowerPendingMinesStudy.Run(q,(pending,after,run,receipt,file,hash,option,ct)=>QuestSystemTests.ResumeTowerPendingFifth(q.ApiRoot,content,pending,after,run,receipt,file,hash,option,ct),deadline.Token);
    }
}
