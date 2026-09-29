using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessFifthEssenceTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static async Task<(TowerReturnOwner Owner,TowerQuestCredit Credit,OfflineContent Content)> Setup(string recipe)
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe==recipe&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var j=new TowerJourneyProgression(Root,content,c,true);j.Growth.Attune(4);
        var xp=new Services.LL.Levels.JsonCharacterExperienceProgressionProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),Root,HarnessJson.Options);
        await j.Growth.AwardIdle(checked((int)Enumerable.Range(30,10).Sum(l=>xp.GetRequiredExperience(l))),default);
        j.WaitUntil(TowerJourneyProgression.Epoch.AddDays(1));await j.Observe(default);
        var p=new TowerEarnedPoint("unit","earned-progression","perfect",recipe,0,25920,25920,0,j.Now,j.Growth.Snapshot(c),c.Equipment.Select(e=>e.Data).ToArray());
        var o=new TowerReturnOwner("unit",c,p,j.ExportRuntime(),[],"unit","unit");
        var credit=new TowerQuestCredit(c.Id,"unit",1,TowerJourneyProgression.Epoch.AddSeconds(10),new string('a',64),"unit",new string('b',64),
            TowerJourneyProgression.Epoch.AddMinutes(1),Enumerable.Range(1,6).Select(i=>TowerJourneyProgression.Epoch.AddHours(i)).ToArray(),j.Now,"unit scenario");
        return(o,credit,content);
    }
    [Theory]
    [InlineData("guardian","thornback_boar")]
    [InlineData("restorer","forest_spirit")]
    [InlineData("striker","glade_panther")]
    [InlineData("controller","hollow_stag")]
    public async Task Retained_native_fifth_keeps_its_identity_and_trains_only_after_absorption(string recipe,string option)
    {
        var(o,credit,content)=await Setup(recipe);var before=HarnessJson.Hash(o);
        var r=await QuestSystemTests.RetainTowerFifth(Root,content,o,credit,option,default);
        Assert.NotNull(r.Acquisition);Assert.Equal(before,HarnessJson.Hash(o));
        Assert.Equal(o.Point.Owned,r.Owner.Point.Owned);Assert.Equal(o.Runtime.Growth.OwnedEssences,r.Owner.Runtime.Growth.OwnedEssences.Take(4));
        Assert.Equal(r.Acquisition.EssenceId,r.Owner.Point.Character.MaterializeEssences()[4].Id);
        Assert.Equal(1,r.Owner.Runtime.Growth.OwnedEssences[4].Level);Assert.Equal(0,r.Owner.Runtime.Growth.OwnedEssences[4].CurrentXp);
        var gate=await TowerEssenceEligibility.Inspect(Root,content,r.Owner,default);
        Assert.True(gate.GetProperty("actualAccepted").GetBoolean());
        Assert.Equal("Loadout contains a locked Essence slot.",gate.GetProperty("at39").GetProperty("message").GetString());
        Assert.Equal("A loadout can only use absorbed Essences.",gate.GetProperty("foreignAt40").GetProperty("message").GetString());
        Assert.Equal(r.Acquisition.EssenceId,gate.GetProperty("savedOwnedIds")[4].GetGuid());
        var j=TowerJourneyProgression.RestoreRuntime(Root,content,o.Origin,r.Owner.Runtime);
        await j.Growth.AwardIdle(1,default);var saved=j.ExportRuntime();
        Assert.Equal(1,saved.Growth.OwnedEssences[4].CurrentXp);
        Assert.Equal(r.Acquisition.EssenceId,TowerJourneyProgression.RestoreRuntime(Root,content,o.Origin,saved).Growth.Snapshot(o.Point.Character).MaterializeEssences()[4].Id);
        await Assert.ThrowsAsync<InvalidDataException>(()=>QuestSystemTests.RetainTowerFifth(Root,content,r.Owner,credit,option,default));
        foreach(var invalid in new[] {
            r.Owner.Runtime with {Growth=r.Owner.Runtime.Growth with {Acquisitions=null}},
            r.Owner.Runtime with {Growth=r.Owner.Runtime.Growth with {Acquisitions=[r.Acquisition with {Owner=Guid.NewGuid()}]}},
            r.Owner.Runtime with {Growth=r.Owner.Runtime.Growth with {Acquisitions=[r.Acquisition with {EssenceId=Guid.NewGuid()}]}},
            r.Owner.Runtime with {Growth=r.Owner.Runtime.Growth with {Acquisitions=[r.Acquisition with {At=credit.ClaimAt.AddDays(1)}]}}
        })Assert.Throws<InvalidDataException>(()=>TowerJourneyProgression.RestoreRuntime(Root,content,o.Origin,invalid));
    }
    [Fact]
    public async Task Absent_completion_leaves_owner_unchanged_and_quest_pending()
    {
        var(o,c,content)=await Setup("guardian");var r=await QuestSystemTests.RetainTowerFifth(Root,content,o,c with {MinesCompletedAt=null,RunFile=null,RunHash=null},"thornback_boar",default);
        Assert.Null(r.Acquisition);Assert.Equal(HarnessJson.Hash(o),HarnessJson.Hash(r.Owner));
        Assert.Equal("Active",r.Quest.GetProperty("status").GetString());
    }
    [Fact]
    public async Task Cross_owner_or_future_completion_cannot_grant_a_fifth()
    {
        var(o,c,content)=await Setup("guardian");
        foreach(var bad in new[] {c with {Owner=Guid.NewGuid()},c with {MinesCompletedAt=c.ActivatedAt.AddSeconds(-1)},c with {MinesCompletedAt=c.ClaimAt.AddDays(1)}})
            await Assert.ThrowsAsync<InvalidDataException>(()=>QuestSystemTests.RetainTowerFifth(Root,content,o,bad,"thornback_boar",default));
    }
    private sealed class OwnedFactAttribute:FactAttribute {public OwnedFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_FIFTH")))Skip="Requires a frozen retained-acquisition owner.";}}
    [OwnedFact]
    public async Task Frozen_fifth_acquisition()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        var q=HarnessJson.Read<TowerFifthRequest>(Environment.GetEnvironmentVariable("LL_TOWER_FIFTH")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        await TowerFifthEssenceStudy.Run(q,(o,c,option,ct)=>QuestSystemTests.RetainTowerFifth(q.ApiRoot,content,o,c,option,ct),deadline.Token);
    }
}
