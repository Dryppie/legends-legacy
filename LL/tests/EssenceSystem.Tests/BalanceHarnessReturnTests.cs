using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessReturnTests
{
    [Fact]
    public async Task Restored_failures_keep_native_weekly_cap_and_restored_victory_keeps_rewards()
    {
        var root=TestContentPaths.FindApiRoot();var fixtures=Path.GetFullPath(Path.Combine(root,"../../../tools/BalanceHarness/Fixtures"));
        var content=OfflineContent.ForTower(root,TowerBundle.ReadSettings(root));var inventory=new TowerActivityInventory(root,content);
        var reference=TowerBootstrapCohorts.Create(root,fixtures,TowerBootstrapCohorts.Read(Path.Combine(fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var at=TowerJourneyProgression.Epoch.AddHours(72);
        var points=Enumerable.Range(0,6).Select(i=> {
            var id=Guid.NewGuid();var owned=new[] {inventory.QuestMace(id),inventory.QuestArmor(id,i)};
            return new TowerEarnedPoint("unit","earned-progression","perfect","guardian",i,25920,25920,0,at,
                reference with {Id=id,Equipment=inventory.Select(owned)},owned);
        }).ToArray();
        var floors=TowerContentProviders.Floors(Path.Combine(root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var party=new TowerEarnedParty("perfect--0--0",0,"earned-progression","perfect",25920,at,floors.GetFloor(1)!,points.Take(5).ToArray());
        var owners=points.Select(p=>new TowerReturnOwner("unit",p.Character,p,
            new TowerJourneyRuntime(new(p.Character.Id,new(p.Character.Level,0,0,0,[]),[],0,0,0,[]),null!,[],[],[],0,0,0,null,null),[],"unit","unit")).ToArray();
        var history=new List<JsonElement>();
        await using var initial=await WorldTowerServiceTests.UnlockServer.Create(root,content,party,points[5],0,default);
        JsonElement last=default;
        for(var i=0;i<4;i++)
        {
            last=await initial.Apply(party,i,i,false,100,default);
            history.Add(JsonSerializer.SerializeToElement(new {floor=1,attempt=i,battle=new {succeeded=false},receipt=last},HarnessJson.Options));
            party=party with {StartsAt=last.GetProperty("after").GetProperty("at").GetDateTimeOffset()};
        }
        var proof=new TowerReturnProof(party.Id,0,last.GetProperty("after"),history.ToArray(),owners,party,party,party,new Dictionary<string,JsonElement>(),default);
        await using var restored=await WorldTowerServiceTests.UnlockServer.RestoreReturn(root,content,proof,default);
        var fifth=await restored.Apply(party,4,4,false,100,default);
        Assert.Equal(30,fifth.GetProperty("after").GetProperty("floors")[0].GetProperty("scoutingProgress").GetInt32());
        history.Add(JsonSerializer.SerializeToElement(new {floor=1,attempt=4,battle=new {succeeded=false},receipt=fifth},HarnessJson.Options));
        party=party with {StartsAt=fifth.GetProperty("after").GetProperty("at").GetDateTimeOffset()};
        var victory=await restored.Apply(party,5,5,true,100,default);
        history.Add(JsonSerializer.SerializeToElement(new {floor=1,attempt=5,battle=new {succeeded=true},receipt=victory},HarnessJson.Options));
        party=party with {Floor=floors.GetFloor(2)!,StartsAt=victory.GetProperty("after").GetProperty("at").GetDateTimeOffset()};
        proof=proof with {PreviousState=victory.GetProperty("after"),HistoricalAttempts=history.ToArray(),Party=party};
        await using var continued=await WorldTowerServiceTests.UnlockServer.RestoreReturn(root,content,proof,default);
        TowerRuntimeCopy.Equal(proof.PreviousState,await continued.State(default),"restored victory rewards");
        var next=await continued.Apply(party,0,6,true,100,default);
        Assert.Equal(10,next.GetProperty("after").GetProperty("titles").GetArrayLength());
    }
    [Fact]
    public void Waiting_preserves_personal_state_and_rejects_rewind()
    {
        var runtime=new TowerJourneyRuntime(null!,null!,[],[],[],123,456,789,null,null);
        var now=TowerReturnStudy.At(runtime);var waited=TowerReturnStudy.Wait(runtime,now.AddSeconds(15));
        Assert.Equal(runtime with {WaitingTicks=150000789},waited);
        Assert.Equal(now.AddSeconds(15),TowerReturnStudy.At(waited));
        Assert.Throws<InvalidDataException>(()=>TowerReturnStudy.Wait(runtime,now.AddTicks(-1)));
    }
    private sealed class QualificationFactAttribute:FactAttribute
    {
        public QualificationFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_RETURN_PREPARATION")))Skip="Requires bounded frozen return qualification.";}
    }
    [QualificationFact]
    public async Task Frozen_return_preparation()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var q=HarnessJson.Read<TowerReturnRequest>(Environment.GetEnvironmentVariable("LL_TOWER_RETURN_PREPARATION")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        await TowerReturnStudy.Qualify(q,async (proof,ct)=>await WorldTowerServiceTests.UnlockServer.RestoreReturn(q.ApiRoot,content,proof,ct),deadline.Token);
    }
    private sealed class CombatFactAttribute:FactAttribute
    {
        public CombatFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_RETURN_COMBAT")))Skip="Requires independently audited qualification and fresh reserved seeds.";}
    }
    [CombatFact]
    public async Task Frozen_return_combat()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        var q=HarnessJson.Read<TowerReturnRequest>(Environment.GetEnvironmentVariable("LL_TOWER_RETURN_COMBAT")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        await TowerReturnStudy.Run(q,async (proof,ct)=>await WorldTowerServiceTests.UnlockServer.RestoreReturn(q.ApiRoot,content,proof,ct),deadline.Token);
    }
}
