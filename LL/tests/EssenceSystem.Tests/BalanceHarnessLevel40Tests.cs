using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessLevel40Tests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    [Theory]
    [InlineData("perfect",1)]
    [InlineData("four-of-five",2)]
    public async Task Forward_growth_stops_at_earned_level_and_keeps_prior_equipment(string outcome,int expectedEncounters)
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));var inventory=new TowerActivityInventory(Root,content);
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var owned=c.Equipment.Select(e=>e.Data).ToArray();c=c with {Equipment=inventory.Select(owned)};
        var j=new TowerJourneyProgression(Root,content,c,true);j.Growth.Attune(4);
        var xp=new Services.LL.Levels.JsonCharacterExperienceProgressionProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),Root,HarnessJson.Options);
        await j.Growth.AwardIdle(checked((int)Enumerable.Range(30,10).Sum(l=>xp.GetRequiredExperience(l))-1),default);
        Assert.Equal(39,j.Growth.Character.Level);
        var r=j.ExportRuntime();r=r with {IdleSeconds=40,Sources=r.Sources with {ResourcesReconciled=true}};
        var p=new TowerEarnedPoint("unit","earned-progression",outcome,"guardian",0,4,4,0,TowerReturnStudy.At(r),j.Growth.Snapshot(c),owned);
        var owner=new TowerReturnOwner("unit",c,p,r,[],"unit","unit");var before=HarnessJson.Hash(owner);
        var g=await TowerLevel40Study.Grow(Root,content,owner,(_,_,_)=>Task.FromResult(JsonSerializer.SerializeToElement(new {testSourceCallback=true})),default);
        Assert.Equal(40,g.Owner.Point.Character.Level);Assert.Equal(expectedEncounters,g.Until-g.From);Assert.Equal(1,g.Victories);
        Assert.Equal(4,g.Owner.Runtime.Growth.OwnedEssences.Length);Assert.Equal(before,HarnessJson.Hash(owner));
        Assert.Equal(owned.Select(e=>e.State.Id),g.Owner.Point.Owned.Take(owned.Length).Select(e=>e.State.Id));
        Assert.Equal(g.Until*10,g.Owner.Runtime.IdleSeconds);
        Assert.Equal(owner.Point.Horizon,g.Owner.Point.Horizon);
    }
    [Theory]
    [InlineData("guardian",5)]
    [InlineData("restorer",4)]
    [InlineData("striker",5)]
    [InlineData("controller",5)]
    public async Task Quest_source_requires_new_Mines_success_and_owned_unique_fifth(string recipe,int eligible)
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe==recipe&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var journey=new TowerJourneyProgression(Root,content,c,true);journey.Growth.Attune(4);
        var point=new TowerEarnedPoint("unit","earned-progression","perfect",recipe,0,25920,25920,0,TowerJourneyProgression.Epoch,c,[]);
        var owner=new TowerReturnOwner("unit",c,point,journey.ExportRuntime(),[],"unit","unit");
        var p=await QuestSystemTests.ProbeTowerQuestSource(Root,content,owner,6,default);
        Assert.True(p.GetProperty("prematureTurnInRejected").GetBoolean());
        Assert.Equal(eligible,p.GetProperty("candidates").EnumerateArray().Count(c=>c.GetProperty("eligibleFifth").GetBoolean()));
        Assert.False(p.GetProperty("probeRetained").GetBoolean());Assert.Equal(0,p.GetProperty("newEssencesGranted").GetInt32());
    }
    private sealed class OwnedFactAttribute:FactAttribute {public OwnedFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_LEVEL40")))Skip="Requires a frozen forward growth owner.";}}
    [OwnedFact]
    public async Task Frozen_level40_growth()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        var q=HarnessJson.Read<TowerLevel40Request>(Environment.GetEnvironmentVariable("LL_TOWER_LEVEL40")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        await TowerLevel40Study.Run(q,(owner,wins,ct)=>QuestSystemTests.ProbeTowerQuestSource(q.ApiRoot,content,owner,wins,ct),deadline.Token);
    }
}
