using BalanceHarness;
using System.Text.Json;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessTowerUnlockTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static (OfflineContent Content,TowerEarnedParty Party,TowerEarnedPoint Outsider) Example()
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));var inventory=new TowerActivityInventory(Root,content);
        var reference=TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character;
        var at=TowerJourneyProgression.Epoch.AddHours(72);
        var points=Enumerable.Range(0,6).Select(i=> {
            var owner=Guid.NewGuid();var owned=new[] {inventory.QuestMace(owner),inventory.QuestArmor(owner,i)};
            return new TowerEarnedPoint("unit","earned-progression","perfect","guardian",i,25920,25920,0,at,
                reference with {Id=owner,Equipment=inventory.Select(owned)},owned);
        }).ToArray();
        var floor=TowerContentProviders.Floors(Path.Combine(Root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options).GetFloor(1)!;
        return(content,new("unit",0,"earned-progression","perfect",25920,at,floor,points.Take(5).ToArray()),points[5]);
    }
    [Fact]
    public async Task Native_first_clears_preserve_Tower_rewards_without_enabling_withdrawn_supplies()
    {
        var (content,party,outsider)=Example();await using var server=await WorldTowerServiceTests.UnlockServer.Create(Root,content,party,outsider,0,default);
        var initial=await server.State(default);
        var floors=TowerContentProviders.Floors(Path.Combine(Root,"Data",TowerBattleRunner.FloorFile),HarnessJson.Options);
        await Assert.ThrowsAsync<InvalidDataException>(()=>server.Apply(party with {Floor=floors.GetFloor(3)!},0,0,true,100,default));
        var failed=await server.Apply(party,0,0,false,100,default);
        Assert.False(failed.GetProperty("after").GetProperty("floors")[0].GetProperty("isCleared").GetBoolean());
        Assert.Empty(failed.GetProperty("after").GetProperty("titles").EnumerateArray());
        var now=failed.GetProperty("after").GetProperty("at").GetDateTimeOffset();
        int expectedTokens=0;JsonElement last=default;
        foreach(var number in Enumerable.Range(1,3))
        {
            var floor=floors.GetFloor(number)!;
            last=await server.Apply(party with {Floor=floor,StartsAt=now},number==1?1:0,number,true,100,default);
            now=last.GetProperty("after").GetProperty("at").GetDateTimeOffset();expectedTokens+=floor.FirstClearTowerTokens;
        }
        var after=last.GetProperty("after");
        Assert.All(after.GetProperty("probes").EnumerateArray(),p=> {
            Assert.Null(p.GetProperty("hypotheticalNextCompletedDungeonChest").GetString());
            Assert.Null(p.GetProperty("oldCompletedDecisionChest").GetString());
            Assert.False(p.GetProperty("towerEquipmentSupplyProcessed").GetBoolean());
        });
        Assert.Equal(15,after.GetProperty("titles").GetArrayLength());
        Assert.All(after.GetProperty("tokens").EnumerateArray(),p=> {
            Assert.Equal(p.GetProperty("owner").GetGuid()==outsider.Character.Id?0:expectedTokens,p.GetProperty("towerTokens").GetInt32());
            Assert.Equal(30,p.GetProperty("level").GetInt32());Assert.Equal(0,p.GetProperty("experience").GetInt32());
        });
        Assert.Equal(0,after.GetProperty("earnedNewEquipment").GetInt32());
    }
    [Fact]
    public async Task Native_failed_attempt_cap_grants_three_scouting_increments_and_never_unlocks_the_next_floor()
    {
        var (content,party,outsider)=Example();await using var server=await WorldTowerServiceTests.UnlockServer.Create(Root,content,party,outsider,0,default);
        var now=party.StartsAt;JsonElement receipt=default;
        foreach(var i in Enumerable.Range(0,4))
        {
            receipt=await server.Apply(party with {StartsAt=now},i,i,false,100,default);
            now=receipt.GetProperty("after").GetProperty("at").GetDateTimeOffset();
        }
        var after=receipt.GetProperty("after");
        Assert.Equal(30,after.GetProperty("floors")[0].GetProperty("scoutingProgress").GetInt32());
        Assert.Equal(JsonValueKind.Null,after.GetProperty("floors")[1].GetProperty("unlockedAt").ValueKind);
        Assert.Empty(after.GetProperty("titles").EnumerateArray());
    }
    private sealed class QualificationFactAttribute:FactAttribute
    {
        public QualificationFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_UNLOCK_PREPARATION")))Skip="Requires a frozen seed-free unlock qualification owner.";}
    }
    [QualificationFact]
    public async Task Frozen_unlock_preparation()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        await TowerUnlockStudy.Qualify(HarnessJson.Read<TowerUnlockRequest>(Environment.GetEnvironmentVariable("LL_TOWER_UNLOCK_PREPARATION")!),deadline.Token);
    }
    private sealed class StudyFactAttribute:FactAttribute
    {
        public StudyFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_UNLOCK_STUDY")))Skip="Requires independently audited preparations and a bounded unlock owner.";}
    }
    [StudyFact]
    public async Task Frozen_native_unlock_journeys()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        var request=HarnessJson.Read<TowerUnlockRequest>(Environment.GetEnvironmentVariable("LL_TOWER_UNLOCK_STUDY")!);
        var content=OfflineContent.ForTower(request.ApiRoot,TowerBundle.ReadSettings(request.ApiRoot));
        var points=HarnessJson.Read<TowerEarnedPoint[]>(Path.Combine(request.Archive,"points.json"));
        await TowerUnlockStudy.Run(request,async (party,path,ct)=> {
            var outsider=points.First(p=>p.Policy==party.Policy&&p.Outcome==party.Outcome&&p.Horizon==party.Horizon
                &&party.Members.All(m=>m.Character.Id!=p.Character.Id));
            var xp=party.Members.Append(outsider).ToDictionary(p=>p.Character.Id,p=> {
                var h=HarnessJson.Read<JsonElement>(Path.Combine(request.GrowthArchive!,p.History+"--history.json"));
                var state=h.GetProperty("progression").GetProperty("checkpoints").EnumerateArray().Single(c=>c.GetProperty("encounter").GetInt32()==p.Encounter).GetProperty("state");
                var entries=h.GetProperty("steps").EnumerateArray().Where(s=>s.GetProperty("encounter").GetInt32()==p.Encounter).ToArray();
                if(entries.Length>0)
                {
                    var ordinal=entries.Last().GetProperty("ordinal").GetInt32();
                    state=h.GetProperty("progression").GetProperty("entries").EnumerateArray().Single(e=>e.GetProperty("ordinal").GetInt32()==ordinal).GetProperty("after");
                }
                if(state.GetProperty("growth").GetProperty("level").GetInt32()!=p.Character.Level)throw new InvalidDataException("Earned XP does not match checkpoint level.");
                return state.GetProperty("growth").GetProperty("experience").GetInt32();
            });
            return await WorldTowerServiceTests.UnlockServer.Create(request.ApiRoot,content,party,outsider,path,ct,xp);
        },deadline.Token);
    }
}
