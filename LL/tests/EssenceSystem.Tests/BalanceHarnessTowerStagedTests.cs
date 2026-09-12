using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessTowerStagedTests
{
    private static string Root=>TestContentPaths.FindApiRoot();
    private static readonly Lazy<TowerBossDiscoveryDefinition> Discovery=new(()=>BalanceHarnessTowerBossDiscoveryContractTests.Definition());
    private static TowerStagedDefinition Plan(int cells=3)
    {
        var source=Discovery.Value; var scenario=BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
        var cohort=new TowerBalanceCohort("floor-1",source.Budget,source.RequiredPartySize,"fixed-equipment",TowerBossDiscovery.EquipmentBudgetHash(scenario.Party));
        return new(1,"staged-test",TowerStagedBalance.Policy,source.ContentHashes,source.SettingsHash,source.ExecutionHash,[cohort],
            Enumerable.Range(0,cells).Select(i=>new TowerStagedCell($"team-{i}",cohort.Id,i==0?"reference":"generated",scenario with {
                Id="staged-context",Seeds=[],Party=scenario.Party.Select(p=>p with { Build=p.Build with { Id=$"team-{i}-actor-{p.PartySlot}" } }).ToArray() })).ToArray(),
            ["team-0"],Enumerable.Range(101,32).ToArray(),Enumerable.Range(1001,256).ToArray(),cells,[999999],500000);
    }
    private static TowerBalanceEvidence Evidence(TowerStagedDefinition d,int cell,int stage,int wins,int draws=0)
    {
        var c=d.Cells[cell]; var scenario=TowerStagedBalance.Scenario(d,c,stage);
        return new(c.Id,"Complete",HarnessJson.Hash(scenario),HarnessJson.Hash(d.ContentHashes),d.SettingsHash,d.ExecutionHash,
            d.Cohorts.Single(x=>x.Id==c.CohortId).RequiredPartySize,
            scenario.Seeds.Select((s,i)=>new TowerBalanceTrial(s,i<wins?BattleOutcome.Victory:i<wins+draws?BattleOutcome.Draw:BattleOutcome.Defeat)).ToArray(),new string('a',64));
    }
    [Fact]
    public void Weak_cells_stop_anchors_skip_first_look_and_unresolved_cells_use_fresh_second_samples()
    {
        var d=Plan(); var first=new[]{Evidence(d,1,1,0),Evidence(d,2,1,12)};
        var selection=TowerStagedBalance.Select(d,first);
        Assert.Equal("Proceed",selection.Status); Assert.Equal(new[]{"team-0","team-2"},selection.SecondStageIds);
        var result=TowerStagedBalance.Evaluate(d,first,[Evidence(d,0,2,80),Evidence(d,2,2,60)]);
        Assert.Equal(GoalOutcome.Pass,result.Assessment); Assert.Equal(576,result.LogicalTrials);
        Assert.Equal(32,result.Cells[1].Samples); Assert.Equal(256,result.Cells[2].Samples);
        Assert.Equal(TowerStagedBalance.Interval(60,256,2),result.Cells[2].Adjusted);
        Assert.Equal(TowerStagedBalance.Interval(0,32,2),result.Cells[1].Adjusted);
    }
    [Fact]
    public void Stage_capacity_never_shrinks_the_family_into_a_pass()
    {
        var d=Plan() with { MaximumSecondStageCells=1 };
        var result=TowerStagedBalance.Evaluate(d,[Evidence(d,1,1,0),Evidence(d,2,1,12)],[]);
        Assert.Equal(GoalOutcome.Inconclusive,result.Assessment); Assert.Equal(3,result.Cells.Count);
        Assert.Equal("SecondStageCapacityExceeded",result.Selection.Status); Assert.Null(result.Cells[2].Adjusted);
        Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Evaluate(d,[Evidence(d,1,1,0),Evidence(d,2,1,12)],[Evidence(d,0,2,80)]));
    }
    [Fact]
    public void First_stage_breach_is_preserved_and_cannot_be_replaced_by_better_second_stage_outcomes()
    {
        var d=Plan(); var first=new[]{Evidence(d,1,1,32),Evidence(d,2,1,0)};
        var result=TowerStagedBalance.Evaluate(d,first,[]);
        Assert.Equal(GoalOutcome.Fail,result.Assessment); Assert.True(result.Cells[1].ObservedAboveCeiling);
        Assert.Contains("team-1",result.Selection.FirstStageBreaches);
        Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Evaluate(d,first,[Evidence(d,0,2,80),Evidence(d,1,2,60)]));
    }
    [Theory]
    [InlineData(0,GoalOutcome.Inconclusive)]
    [InlineData(80,GoalOutcome.Pass)]
    [InlineData(128,GoalOutcome.Inconclusive)]
    [InlineData(129,GoalOutcome.Fail)]
    [InlineData(256,GoalOutcome.Fail)]
    public void Complete_final_samples_still_require_viability_ceiling_and_uncertainty(int wins,GoalOutcome expected)
    {
        var d=Plan(); var first=new[]{Evidence(d,1,1,0),Evidence(d,2,1,0)};
        Assert.Equal(expected,TowerStagedBalance.Evaluate(d,first,[Evidence(d,0,2,wins)]).Assessment);
    }
    [Fact]
    public void Missing_duplicate_extra_reused_or_changed_evidence_cannot_pass()
    {
        var d=Plan(); var first=new[]{Evidence(d,1,1,0),Evidence(d,2,1,0)}; var second=Evidence(d,0,2,80);
        Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Evaluate(d,first,[]));
        Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Evaluate(d,[first[0],first[0]],[second]));
        Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Evaluate(d,first,[second,second]));
        foreach(var invalid in new[]{second with { Status="Incomplete" },second with { SettingsHash=new string('b',64) },
            second with { Trials=Evidence(d,0,1,8).Trials },second with { ScenarioHash=new string('b',64) },
            second with { Trials=second.Trials.Reverse().ToArray() },second with { Trials=[..second.Trials.Take(255)] },second with { ArtifactHash="" }})
            Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Evaluate(d,first,[invalid]));
    }
    [Fact]
    public void Contract_rejects_reused_seeds_duplicate_recipes_missing_anchors_and_underreserved_work()
    {
        var d=Plan();
        foreach(var invalid in new[]{d with { FirstSeeds=[1001] },d with { ExcludedCombatSeeds=[101] },d with { AnchorIds=[] },
            d with { AnchorIds=["missing"] },d with { AnchorIds=["team-0","team-0"] },d with { MaximumBattles=10 },
            d with { Cells=[d.Cells[0],d.Cells[1] with { Scenario=d.Cells[0].Scenario },d.Cells[2]] },
            d with { Cells=[d.Cells[0] with { Scenario=d.Cells[0].Scenario with { Seeds=[1] } },d.Cells[1],d.Cells[2]] }})
            Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Validate(invalid));
        Assert.True(TowerStagedBalance.Validate(Plan(1001) with { MaximumSecondStageCells=1 })>0);
    }
    [Fact]
    public void Every_cohort_requires_a_fresh_anchor_and_its_own_viable_final_team()
    {
        var d=Plan(2); var secondCohort=d.Cohorts[0] with { Id="second",Context="other-context" };
        d=d with { Cohorts=[d.Cohorts[0],secondCohort],Cells=[d.Cells[0],d.Cells[1] with { CohortId=secondCohort.Id }],AnchorIds=["team-0","team-1"] };
        Assert.Throws<InvalidDataException>(()=>TowerStagedBalance.Validate(d with { AnchorIds=["team-0"] }));
        Assert.Equal(GoalOutcome.Fail,TowerStagedBalance.Evaluate(d,[],[Evidence(d,0,2,80),Evidence(d,1,2,0)]).Assessment);
    }
    [Theory]
    [InlineData(0,32,2678,0,0.380359214407483)]
    [InlineData(80,256,384,0.2107120257469235,0.43627940009782107)]
    [InlineData(128,256,1,0.430633501151987,0.569366498848013)]
    [InlineData(1,32,10000,0.0012968829500159818,0.44485510998171585)]
    public void Numerical_intervals_match_independent_normal_quantiles(int wins,int samples,int family,double low,double high)
    {
        var interval=TowerStagedBalance.Interval(wins,samples,family);
        Assert.InRange(Math.Abs(interval.Lower-low),0,1e-8); Assert.InRange(Math.Abs(interval.Upper-high),0,1e-8);
    }
    [Fact]
    public async Task Cancel_resume_reconstructs_selection_and_committed_work_without_accepting_tampering()
    {
        using var temp=new DiscoveryTemp(); var path=Path.Combine(temp.Path,"staged");
        var d=Plan(2) with { AnchorIds=["team-0","team-1"],FirstSeeds=[81001,81002],SecondSeeds=[82001,82002] };
        var options=new TowerBulkOptions(ChunkSize:1,RetryReserve:4);
        using var cancel=new CancellationTokenSource(); var completed=0;
        using(new TowerPerformanceTrace(done=>{ if(done && ++completed==2) cancel.Cancel(); }).Activate())
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>TowerStagedBalanceRun.RunAsync(Root,path,d,options,token:cancel.Token));
        var resumed=await TowerStagedBalanceRun.RunAsync(Root,path,d,options,resume:true);
        Assert.Equal(4,resumed.LogicalTrials);
        Assert.Equal(HarnessJson.Hash(resumed),HarnessJson.Hash(await TowerStagedBalanceRun.VerifyAsync(path)));
        var accounting=HarnessJson.Read<TowerBulkAccounting>(Path.Combine(path,"campaign-accounting.json"));
        Assert.Equal(5,accounting.ChargedAttempts);
        Assert.Equal(resumed.ExitCode,await BalanceHarness.Program.Main(["tower-staged-balance-verify","--run",path]));
        File.AppendAllText(Path.Combine(path,"stage-selection.json")," ");
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerStagedBalanceRun.VerifyAsync(path));
    }
}
