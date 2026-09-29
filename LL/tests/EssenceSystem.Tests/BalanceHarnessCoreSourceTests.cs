using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessCoreSourceTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static (TowerReturnOwner Owner,OfflineContent Content) Owner()
    {
        var fixtures=Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
        var content=OfflineContent.ForTower(Root,TowerBundle.ReadSettings(Root));
        var c=TowerBootstrapCohorts.Create(Root,fixtures,TowerBootstrapCohorts.Read(Path.Combine(fixtures,"tower-bootstrap.json")),content)
            .First(c=>c.Gear=="common"&&c.EssenceLevel==1).Character;
        var journey=new TowerJourneyProgression(Root,content,c,true);journey.WaitUntil(TowerJourneyProgression.Epoch.AddDays(1));
        var point=new TowerEarnedPoint("unit","earned-progression","perfect","guardian",0,25920,25920,0,journey.Now,c,c.Equipment.Select(e=>e.Data).ToArray());
        return(new("unit",c,point,journey.ExportRuntime(),[],"unit","unit"),content);
    }
    [Fact]
    public void Grade_one_projection_uses_nonuniform_native_distribution_and_one_first_bonus()
    {
        Assert.Equal(new[] {9,10,11,12},TowerCoreSourceStudy.Distribution(1,1).Select(p=>p.Cores));
        Assert.Equal(new[] {3d/12,4d/12,4d/12,1d/12},TowerCoreSourceStudy.Distribution(1,1).Select(p=>p.Probability));
        var repeated=TowerCoreSourceStudy.Distribution(2,1);
        Assert.Equal(12,repeated.First().Cores);Assert.Equal(18,repeated.Last().Cores);
        Assert.Equal(1,repeated.Sum(p=>p.Probability),12);Assert.Equal(14.5,repeated.Sum(p=>p.Probability*p.Cores),12);
        Assert.Throws<ArgumentOutOfRangeException>(()=>TowerCoreSourceStudy.Distribution(1,2));
    }
    [Theory]
    [InlineData(1,0,1296004)]
    [InlineData(8,73964,234318)]
    [InlineData(9,155666,1)]
    [InlineData(10,0,0)]
    public void Training_uses_current_XP_without_charging_past_levels(int level,int xp,int expected)
        =>Assert.Equal(expected,TowerCoreSourceStudy.XpToFirstAscension(new(Guid.NewGuid(),"unit",level,xp,0)));
    [Fact]
    public void Duplicate_foreign_mastery_future_or_unpaid_sources_are_rejected()
    {
        var(o,_)=Owner();var start=TowerJourneyProgression.Epoch;
        var failed=new TowerCoreRun("unit","file",new string('a',64),Guid.NewGuid(),"goblin_mines","Failed",start,start.AddMinutes(1),60,1,false);
        TowerCoreSourceStudy.ValidateRuns(o,[failed]);
        foreach(var runs in new[] {new[] {failed,failed},new[] {failed with {SigilsPaid=0}},new[] {failed with {FirstCompletion=true}},
            new[] {failed with {Ended=start.AddDays(2)}},new[] {failed with {Status="Completed",FirstCompletion=true}},new[] {failed with {Status="Active"}}})
            Assert.Throws<InvalidDataException>(()=>TowerCoreSourceStudy.ValidateRuns(o,runs));
    }
    [Fact]
    public async Task Native_completion_claim_and_first_family_guards_conserve_probe_cores()
        =>Assert.NotNull(await TowerCoreSourceProbe.Completions(Root,default));
    [Fact]
    public async Task Native_training_and_ascension_need_exact_XP_owned_Essences_and_six_cores()
    {
        var(o,c)=Owner();var before=HarnessJson.Hash(o);
        Assert.NotNull(await TowerCoreSourceProbe.Ascension(Root,c,o,default));Assert.Equal(before,HarnessJson.Hash(o));
    }
    private sealed class OwnedFactAttribute:FactAttribute {public OwnedFactAttribute(){if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_CORE_SOURCE")))Skip="Requires a frozen core-source owner.";}}
    [OwnedFact]
    public async Task Frozen_core_source_qualification()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerCoreSourceStudy.Run(HarnessJson.Read<TowerCoreSourceRequest>(Environment.GetEnvironmentVariable("LL_TOWER_CORE_SOURCE")!),deadline.Token);
    }
}
