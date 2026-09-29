using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessFifthReturnTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Restoring_population_growth_retains_rosters_rewards_and_weekly_failure_history(bool nextWeek)
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));var inventory=new TowerActivityInventory(Root,content);
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var at=TowerJourneyProgression.Epoch.AddHours(72);
        var points=Enumerable.Range(0,16).Select(i=> {
            var id=Guid.NewGuid();var owned=new[] {inventory.QuestMace(id),inventory.QuestArmor(id,i)};
            return new TowerEarnedPoint("unit","earned-progression","perfect","guardian",i,25920,25920,0,at,c with {Id=id,Equipment=inventory.Select(owned)},owned);
        }).ToArray();
        TowerReturnOwner Owner(TowerEarnedPoint p)=>new("unit",p.Character,p,
            new(new(p.Character.Id,new(p.Character.Level,0,0,0,[]),[],0,0,0,[]),null!,[],[],[],0,0,0,null,null),[],"unit","unit");
        var owners=points.Select(Owner).ToArray();
        var floors=TowerContentProviders.Floors(Path.Combine(Root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var party=new TowerEarnedParty("perfect--0--0",0,"earned-progression","perfect",25920,at,floors.GetFloor(1)!,points.Take(5).ToArray());
        await using var initial=await WorldTowerServiceTests.UnlockServer.Create(Root,content,party,points[15],0,default);
        var history=new List<JsonElement>();JsonElement last=default;
        void Record(int floor,int attempt,bool succeeded,JsonElement receipt)=>history.Add(JsonSerializer.SerializeToElement(new {floor,attempt,battle=new {succeeded},receipt},HarnessJson.Options));
        for(var floor=1;floor<=4;floor++)
        {
            party=party with {Floor=floors.GetFloor(floor)!};last=await initial.Apply(party,0,floor,true,100,default);Record(floor,0,true,last);
            party=party with {StartsAt=last.GetProperty("after").GetProperty("at").GetDateTimeOffset()};
        }
        // Deliberately differs from population enumeration order, while retaining the original five seats.
        party=party with {Floor=floors.GetFloor(5)!,Members=points.Take(5).Concat(points.Skip(5).Take(5).Reverse()).ToArray()};
        var continuation=new TowerReturnProof("perfect--0--0",0,last.GetProperty("after"),history.ToArray(),owners,party,party,party,new Dictionary<string,JsonElement>(),default);
        var expanded=new TowerExpansionProof("unit","unit","unit","unit",3,continuation,default,[]);
        await using(var server=await WorldTowerServiceTests.UnlockServer.RestoreExpansion(Root,content,expanded,default))
        {
            for(var index=0;index<4;index++)
            {
                last=await server.Apply(party,index,100+index,false,100,default);Record(5,index,false,last);
                party=party with {StartsAt=last.GetProperty("after").GetProperty("at").GetDateTimeOffset()};
            }
        }
        var before=history[3].GetProperty("receipt").GetProperty("after");var after=history[4].GetProperty("receipt").GetProperty("before");
        WorldTowerServiceTests.UnlockServer.ValidatePopulationBoundary(before,after);
        foreach(var change in new Action<JsonNode>[] {
            n=>n["tokens"]![0]!["towerTokens"]=999,
            n=>n["tokens"]!.AsArray().First(t=>!before.GetProperty("tokens").EnumerateArray().Any(o=>o.GetProperty("owner").GetGuid()==t!["owner"]!.GetValue<Guid>()))!["towerTokens"]=1,
            n=>n["floors"]![0]!["scoutingProgress"]=17,
            n=>n["at"]=at.AddDays(2),
            n=>n["tokens"]!.AsArray().RemoveAt(0)
        })
        {
            var bad=JsonNode.Parse(after.GetRawText())!;change(bad);
            Assert.Throws<InvalidDataException>(()=>WorldTowerServiceTests.UnlockServer.ValidatePopulationBoundary(before,bad.Deserialize<JsonElement>(HarnessJson.Options)));
        }
        owners=points.Select(p=>Owner(p with {Character=p.Character with {Level=40}})).ToArray();var ids=party.Members.Select(m=>m.Character.Id).ToArray();
        party=party with {StartsAt=party.StartsAt.AddDays(nextWeek?5:0).AddHours(1),Members=ids.Select(id=>owners.Single(o=>o.Point.Character.Id==id).Point).ToArray()};
        continuation=continuation with {PreviousState=last.GetProperty("after"),HistoricalAttempts=history.ToArray(),Owners=owners,Party=party,Baseline=party,Supplied=party};
        var proof=new TowerFifthReturnProof("unit","unit",3,4,8,continuation,default,[]);
        await using var restored=await WorldTowerServiceTests.UnlockServer.RestoreFifthReturn(Root,content,proof,default);
        var admission=await restored.QualifyExpansionRally(default);
        Assert.All(admission.GetProperty("historicalRosters").EnumerateArray().Where(r=>r.GetProperty("floorNumber").GetInt32()==5),r=>Assert.Equal(ids,r.GetProperty("owners").EnumerateArray().Select(v=>v.GetGuid())));
        var retry=await restored.Apply(party,4,104,false,100,default);
        Assert.Equal(nextWeek?40:30,retry.GetProperty("after").GetProperty("floors")[4].GetProperty("scoutingProgress").GetInt32());
        Assert.Equal(HarnessJson.Hash(last.GetProperty("after").GetProperty("titles")),HarnessJson.Hash(retry.GetProperty("after").GetProperty("titles")));
        Assert.Equal(HarnessJson.Hash(last.GetProperty("after").GetProperty("unlocks")),HarnessJson.Hash(retry.GetProperty("after").GetProperty("unlocks")));
    }
    private sealed class OwnedFactAttribute:FactAttribute {public OwnedFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_FIFTH_RETURN")))Skip="Requires a frozen fifth return owner.";}}
    [OwnedFact]
    public async Task Frozen_fifth_return()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(840));var file=Environment.GetEnvironmentVariable("LL_TOWER_FIFTH_RETURN")!;
        var q=HarnessJson.Read<TowerFifthReturnRequest>(file);var mode=HarnessJson.Read<JsonElement>(file).GetProperty("mode").GetString();
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        if(mode=="prepare")await TowerFifthReturnStudy.Qualify(q,async(p,ct)=> {
            await using var server=await WorldTowerServiceTests.UnlockServer.RestoreFifthReturn(q.ApiRoot,content,p,ct);
            return(await server.State(ct),await server.QualifyExpansionRally(ct));
        },deadline.Token);
        else await TowerFifthReturnStudy.Run(q,async(p,ct)=>await WorldTowerServiceTests.UnlockServer.RestoreFifthReturn(q.ApiRoot,content,p,ct),deadline.Token);
    }
}
