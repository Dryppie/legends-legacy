using BalanceHarness;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Prophecies;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessRuntimeTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static string Fixtures=>Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static (OfflineContent Content,FixtureCharacter Character) Example()
    {
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        return(content,TowerBootstrapCohorts.Create(Root,Fixtures,TowerBootstrapCohorts.Read(Path.Combine(Fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Recipe=="guardian"&&c.Gear=="common"&&c.EssenceLevel==1).Character);
    }
    [Fact]
    public async Task Restored_native_progress_is_detached_and_matches_live_claims_and_rollover()
    {
        var (content,c)=Example();var live=new TowerJourneyProgression(Root,content,c,true);
        await live.Idle("region_01_area_06",true,10,default);
        var snapshot=live.ExportRuntime();var originalHash=HarnessJson.Hash(snapshot);
        var restored=TowerJourneyProgression.RestoreRuntime(Root,content,c,snapshot);
        foreach(var j in new[]{live,restored})
        {
            await j.Event(ProphecyProgressKind.CreatureDefeated,100000,default);await j.Claim(default);
            j.WaitUntil(TowerJourneyProgression.Epoch.AddDays(1));await j.Idle("region_01_area_06",true,10,default);
        }
        TowerRuntimeCopy.Equal(live.ExportRuntime(),restored.ExportRuntime(),"native continuation");
        Assert.Equal(originalHash,HarnessJson.Hash(snapshot));Assert.Equal(20,restored.IdleSeconds);
        Assert.True(restored.WaitingTicks>0);Assert.Equal(2,restored.Days.Count);
        await restored.Sources.VerifyClaimGuards(default);
    }
    [Fact]
    public async Task Cross_owner_restore_and_rewind_are_rejected()
    {
        var (content,c)=Example();var j=new TowerJourneyProgression(Root,content,c,true);
        await j.Idle("region_01_area_06",true,10,default);
        Assert.Throws<InvalidDataException>(()=>TowerJourneyProgression.RestoreRuntime(Root,content,c with {Id=Guid.NewGuid()},j.ExportRuntime()));
        Assert.Throws<InvalidDataException>(()=>j.WaitUntil(TowerJourneyProgression.Epoch));
        Assert.Throws<InvalidDataException>(()=>j.Sources.SpendEntry(new Dictionary<string,int>{{"sigil_goblin_mines",1}}));
    }
    private sealed class RuntimeFactAttribute:FactAttribute
    {
        public RuntimeFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_RUNTIME")))Skip="Requires frozen continuation-runtime owner.";}
    }
    [RuntimeFact]
    public async Task Frozen_native_runtime_restoration()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(4));
        await TowerRuntimeStudy.Run(HarnessJson.Read<TowerRuntimeRequest>(Environment.GetEnvironmentVariable("LL_TOWER_RUNTIME")!),deadline.Token);
    }
}
