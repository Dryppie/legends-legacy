using System.Text.Json;
using BalanceHarness;
using Domain.Models.Essences;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessExpansionTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    [Fact]
    public async Task Native_fifth_slot_requires_level_and_owned_essence_without_grants()
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var journey=new TowerJourneyProgression(Root,content,c,true);journey.Growth.Attune(4);
        var runtime=journey.ExportRuntime();var p=new TowerEarnedPoint("unit","earned-progression","perfect","guardian",0,25920,25920,0,TowerJourneyProgression.Epoch,c,[]);
        var owner=new TowerReturnOwner("unit",c,p,runtime,[],"unit","unit");
        var proof=await TowerEssenceEligibility.Inspect(Root,content,owner,default);
        Assert.True(proof.GetProperty("actualAccepted").GetBoolean());
        Assert.Equal("Loadout contains a locked Essence slot.",proof.GetProperty("fifthAtCurrentLevel").GetProperty("message").GetString());
        Assert.Equal("A loadout can only use absorbed Essences.",proof.GetProperty("fifthAtHypotheticalLevel40").GetProperty("message").GetString());
        Assert.Equal(4,EssenceSlotProgression.GetUnlockedSlotCount(39));Assert.Equal(5,EssenceSlotProgression.GetUnlockedSlotCount(40));
    }
    [Fact]
    public async Task Expanding_roster_preserves_prior_five_and_only_new_victory_rewards_newcomers()
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));var inventory=new TowerActivityInventory(Root,content);
        var c=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var at=TowerJourneyProgression.Epoch.AddHours(72);
        var points=Enumerable.Range(0,16).Select(i=> {
            var id=Guid.NewGuid();var owned=new[] {inventory.QuestMace(id),inventory.QuestArmor(id,i)};
            return new TowerEarnedPoint("unit","earned-progression","perfect","guardian",i,25920,25920,0,at,c with {Id=id,Equipment=inventory.Select(owned)},owned);
        }).ToArray();
        var owners=points.Select(p=>new TowerReturnOwner("unit",p.Character,p,
            new(new(p.Character.Id,new(30,0,0,0,[]),[],0,0,0,[]),null!,[],[],[],0,0,0,null,null),[],"unit","unit")).ToArray();
        var floors=TowerContentProviders.Floors(Path.Combine(Root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        var party=new TowerEarnedParty("perfect--0--0",0,"earned-progression","perfect",25920,at,floors.GetFloor(1)!,points.Take(5).ToArray());
        await using var initial=await WorldTowerServiceTests.UnlockServer.Create(Root,content,party,points[15],0,default,population:points.Select(p=>p.Character).ToArray());
        var history=new List<JsonElement>();JsonElement last=default;
        for(var floor=1;floor<=4;floor++)
        {
            party=party with {Floor=floors.GetFloor(floor)!};last=await initial.Apply(party,0,floor,true,100,default);
            history.Add(JsonSerializer.SerializeToElement(new {floor,attempt=0,battle=new {succeeded=true},receipt=last},HarnessJson.Options));
            party=party with {StartsAt=last.GetProperty("after").GetProperty("at").GetDateTimeOffset()};
        }
        party=party with {Floor=floors.GetFloor(5)!,Members=points.Take(10).ToArray()};
        var continuation=new TowerReturnProof("perfect--0--0",0,last.GetProperty("after"),history.ToArray(),owners,party,party,party,new Dictionary<string,JsonElement>(),default);
        var proof=new TowerExpansionProof("unit","unit","unit","unit",4,continuation,default,[]);
        await using(var admission=await WorldTowerServiceTests.UnlockServer.RestoreExpansion(Root,content,proof,default))
        {
            var eligibility=await admission.QualifyExpansionRally(default);Assert.True(eligibility.GetProperty("accepted").GetBoolean());
            Assert.All(eligibility.GetProperty("historicalRosters").EnumerateArray(),r=>Assert.Equal(5,r.GetProperty("owners").GetArrayLength()));
        }
        await using var server=await WorldTowerServiceTests.UnlockServer.RestoreExpansion(Root,content,proof,default);
        var receipt=await server.Apply(party,0,5,true,100,default);var after=receipt.GetProperty("after");
        foreach(var newcomer in points.Skip(5).Take(5))
        {
            var token=after.GetProperty("tokens").EnumerateArray().Single(t=>t.GetProperty("owner").GetGuid()==newcomer.Character.Id);
            Assert.Equal(party.Floor.FirstClearTowerTokens,token.GetProperty("towerTokens").GetInt32());
            Assert.Single(after.GetProperty("titles").EnumerateArray().Where(t=>t.GetProperty("owner").GetGuid()==newcomer.Character.Id));
        }
    }
    private sealed class PreparationFactAttribute:FactAttribute {public PreparationFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_EXPANSION_PREPARATION")))Skip="Requires a frozen expansion qualification owner.";}}
    [PreparationFact]
    public async Task Frozen_expansion_preparation()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var q=HarnessJson.Read<TowerExpansionRequest>(Environment.GetEnvironmentVariable("LL_TOWER_EXPANSION_PREPARATION")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        await TowerExpansionStudy.Qualify(q,async (proof,ct)=> {
            await using var server=await WorldTowerServiceTests.UnlockServer.RestoreExpansion(q.ApiRoot,content,proof,ct);
            return (await server.State(ct),await server.QualifyExpansionRally(ct));
        },deadline.Token);
    }
    private sealed class CombatFactAttribute:FactAttribute {public CombatFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_EXPANSION_COMBAT")))Skip="Requires independently audited preparations and fresh reserved seeds.";}}
    [CombatFact]
    public async Task Frozen_expansion_combat()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        var q=HarnessJson.Read<TowerExpansionRequest>(Environment.GetEnvironmentVariable("LL_TOWER_EXPANSION_COMBAT")!);
        var content=OfflineContent.ForTower(q.ApiRoot,TowerBundle.ReadSettings(q.ApiRoot));
        await TowerExpansionStudy.Run(q,async (proof,ct)=>await WorldTowerServiceTests.UnlockServer.RestoreExpansion(q.ApiRoot,content,proof,ct),deadline.Token);
    }
}
