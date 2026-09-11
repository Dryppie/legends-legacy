using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBalanceEvaluatorTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static readonly Lazy<TowerBossDiscoveryDefinition> Discovery = new(() => BalanceHarnessTowerBossDiscoveryContractTests.Definition());

    private static TowerBalanceDefinition Plan(int samples = 1000, int cells = 1)
    {
        var d = Discovery.Value;
        var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
        var cohort = new TowerBalanceCohort("floor-1-budget", d.Budget, d.RequiredPartySize, "fixed-equipment",
            TowerBossDiscovery.EquipmentBudgetHash(scenario.Party));
        return new(1,"confirmation-test",TowerBalanceEvaluator.IntervalPolicy,d.ContentHashes,d.SettingsHash,d.ExecutionHash,[cohort],
            Enumerable.Range(0,cells).Select(i => new TowerBalanceCellDefinition($"team-{i}",cohort.Id,i==0?"generated":"reference",
                scenario with { Id=$"team-{i}",Seeds=Enumerable.Range(1,samples).ToArray(),Party=scenario.Party.Select(p => p with {
                    Build=p.Build with { Id=$"team-{i}-character-{p.PartySlot}" } }).ToArray() }, samples)).ToArray(),[999999],100000);
    }

    private static TowerBalanceEvidence Evidence(TowerBalanceDefinition d,int cell,int wins,int draws=0)
    {
        var c=d.Cells[cell];
        return new(c.Id,"Complete",HarnessJson.Hash(c.Scenario),HarnessJson.Hash(d.ContentHashes),d.SettingsHash,d.ExecutionHash,
            d.Cohorts.Single(cohort=>cohort.Id==c.CohortId).RequiredPartySize,
            c.Scenario.Seeds.Select((seed,i)=>new TowerBalanceTrial(seed,i<wins?BattleOutcome.Victory:i<wins+draws?BattleOutcome.Draw:BattleOutcome.Defeat)).ToArray(),
            new string('a',64));
    }

    [Theory]
    [InlineData(10,10,GoalOutcome.Fail)]
    [InlineData(10,5,GoalOutcome.Inconclusive)]
    [InlineData(10,1,GoalOutcome.Inconclusive)]
    [InlineData(10,0,GoalOutcome.Inconclusive)]
    [InlineData(1000,0,GoalOutcome.Fail)]
    [InlineData(1000,90,GoalOutcome.Inconclusive)]
    [InlineData(1000,100,GoalOutcome.Inconclusive)]
    [InlineData(1000,300,GoalOutcome.Pass)]
    [InlineData(1000,500,GoalOutcome.Inconclusive)]
    [InlineData(1000,501,GoalOutcome.Fail)]
    public void Acceptance_uses_inclusive_bounds_and_uncertainty_without_accepting_saturated_results(int samples,int wins,GoalOutcome expected)
    {
        var d=Plan(samples);var report=TowerBalanceEvaluator.Evaluate(d,[Evidence(d,0,wins)]);
        Assert.Equal(expected,report.Assessment);
        Assert.Equal(wins*2>samples,report.Cells[0].ObservedAboveCeiling);
        Assert.Equal(expected==GoalOutcome.Pass?0:expected==GoalOutcome.Fail?1:3,report.ExitCode);
    }

    [Fact]
    public void One_viable_team_can_pass_alongside_zero_clear_controls_but_high_outlier_cannot_hide_in_average()
    {
        var d=Plan(cells:3);
        var passing=TowerBalanceEvaluator.Evaluate(d,[Evidence(d,0,300),Evidence(d,1,0),Evidence(d,2,0)]);
        Assert.Equal(GoalOutcome.Pass,passing.Assessment);
        Assert.True(passing.Cells[0].LowerSupported);
        Assert.False(passing.Cells[1].LowerSupported);
        Assert.Equal(GoalOutcome.Pass,passing.Cells[1].Outcome); // Upper bound alone; cohort owns viability.
        var failing=TowerBalanceEvaluator.Evaluate(d,[Evidence(d,0,600),Evidence(d,1,0),Evidence(d,2,0)]);
        Assert.Equal(GoalOutcome.Fail,failing.Assessment);
        Assert.True(failing.Cohorts[0].ObservedAboveCeiling);
        Assert.Equal(GoalOutcome.Fail,TowerBalanceEvaluator.Evaluate(d,[Evidence(d,0,0),Evidence(d,1,0),Evidence(d,2,0)]).Assessment);
    }

    [Fact]
    public void Draws_are_non_wins_and_missing_invalid_or_duplicate_trials_never_pass()
    {
        var d=Plan();var row=Evidence(d,0,300,700);
        var report=TowerBalanceEvaluator.Evaluate(d,[row]);
        Assert.Equal(GoalOutcome.Pass,report.Assessment);
        Assert.Equal(700,report.Cells[0].Draws);
        Assert.Equal(.3,report.Cells[0].PointwiseInterval!.Rate);
        var invalid=new[] {
            row with { Status="Cancelled" }, row with { Trials=row.Trials.Take(999).ToArray() },
            row with { Trials=row.Trials.Append(row.Trials[0]).ToArray() },
            row with { Trials=row.Trials.Select((t,i)=>i==0?t with { Seed=100000 }:t).ToArray() },
            row with { Trials=row.Trials.Select((t,i)=>i==0?t with { Outcome=(BattleOutcome)99 }:t).ToArray() },
            row with { ScenarioHash=new string('b',64) }, row with { ContentHash=new string('b',64) },
            row with { SettingsHash=new string('b',64) }, row with { ExecutionHash=new string('b',64) },
            row with { RequiredPartySize=4 }, row with { Error="Preparation mismatch" }, row with { Trials=null! }
        };
        Assert.All(invalid,e=>Assert.Equal(GoalOutcome.Invalid,TowerBalanceEvaluator.Evaluate(d,[e]).Assessment));
        Assert.Equal(GoalOutcome.Invalid,TowerBalanceEvaluator.Evaluate(d,[]).Assessment);
        Assert.Equal(GoalOutcome.Invalid,TowerBalanceEvaluator.Evaluate(d,[row,row]).Assessment);
        Assert.Equal(GoalOutcome.Invalid,TowerBalanceEvaluator.Evaluate(d,[row,row with { CellId="extra" }]).Assessment);
        var partial=Evidence(d,0,1000) with { Trials=Evidence(d,0,1000).Trials.Take(10).ToArray() };
        var incomplete=TowerBalanceEvaluator.Evaluate(d,[partial]);
        Assert.Equal(GoalOutcome.Invalid,incomplete.Assessment);
        Assert.True(incomplete.Cells[0].ObservedAboveCeiling);
        Assert.Null(incomplete.Cells[0].AdjustedInterval);
    }

    [Fact]
    public void Missing_member_does_not_shrink_the_frozen_multiple_comparison_family()
    {
        var d=Plan(cells:5);
        var report=TowerBalanceEvaluator.Evaluate(d,[Evidence(d,0,300)]);
        Assert.Equal(GoalOutcome.Invalid,report.Assessment);
        Assert.Equal(5,report.FamilySize);
        Assert.Equal(.99,report.Cells[0].AdjustedInterval!.Confidence,12);
        Assert.True(report.Cells[0].AdjustedInterval!.Upper>report.Cells[0].PointwiseInterval!.Upper);
        Assert.Equal(4,report.Cells.Count(c=>c.Outcome==GoalOutcome.Invalid));
    }

    [Fact]
    public void Every_declared_floor_budget_and_context_is_assessed_separately()
    {
        var d=Plan(cells:2);
        var next=d.Cohorts[0] with { Id="floor-2-budget",Budget=d.Cohorts[0].Budget with { PriorityFloor=2 } };
        var second=d.Cells[1] with { CohortId=next.Id,Scenario=d.Cells[1].Scenario with { FloorNumber=2 } };
        var separate=d with { Cohorts=[d.Cohorts[0],next],Cells=[d.Cells[0],second] };
        var report=TowerBalanceEvaluator.Evaluate(separate,[Evidence(separate,0,300),Evidence(separate,1,0)]);
        Assert.Equal(GoalOutcome.Pass,report.Cohorts[0].Outcome);
        Assert.Equal(GoalOutcome.Fail,report.Cohorts[1].Outcome);
        Assert.Equal(GoalOutcome.Fail,report.Assessment);
        Assert.Equal(GoalOutcome.Invalid,TowerBalanceEvaluator.Evaluate(separate,[Evidence(separate,0,300)]).Assessment);
        Assert.Throws<InvalidDataException>(()=>TowerBalanceEvaluator.Validate(separate with { Cells=[d.Cells[0]] }));
        var otherContext=next with { Budget=d.Cohorts[0].Budget,Context="alternative-context" };
        var contexts=separate with { Cohorts=[d.Cohorts[0],otherContext],Cells=[d.Cells[0],second with { Scenario=d.Cells[1].Scenario }] };
        Assert.Equal(GoalOutcome.Fail,TowerBalanceEvaluator.Evaluate(contexts,[Evidence(contexts,0,300),Evidence(contexts,1,0)]).Assessment);
    }

    [Fact]
    public void Below_checkpoint_evidence_requires_diagnostic_label_and_cannot_accept_intended_progression()
    {
        var d = Plan();
        var cohort = d.Cohorts[0] with { Budget = d.Cohorts[0].Budget with { PriorityFloor = 11 } };
        var cell = d.Cells[0] with { Scenario = d.Cells[0].Scenario with { FloorNumber = 11 } };
        var intended = d with { Cohorts = [cohort], Cells = [cell] };
        Assert.Throws<InvalidDataException>(() => TowerBalanceEvaluator.Validate(intended));
        var diagnostic = intended with { Cohorts = [cohort with { Purpose = "diagnostic" }] };
        var report = TowerBalanceEvaluator.Evaluate(diagnostic, [Evidence(diagnostic, 0, 300)]);
        Assert.Equal(GoalOutcome.Pass, report.Assessment);
        Assert.Equal("diagnostic", report.Cohorts[0].Purpose);
        Assert.Contains("Diagnostic-budget findings do not accept intended progression", report.Scope);
        Assert.Contains("diagnostic", TowerBalanceEvaluator.Markdown(report));
    }

    [Fact]
    public void Contract_rejects_budget_changes_duplicate_recipes_reused_seeds_and_unknown_fields()
    {
        var d=Plan();
        var invalid=new[] {
            d with { IntervalPolicy="point-estimates-only" }, d with { MaximumBattles=999 },
            d with { ExcludedCombatSeeds=[1] },
            d with { Cells=[d.Cells[0],d.Cells[0] with { Id="duplicate-recipe" }] },
            d with { Cells=[d.Cells[0],d.Cells[0] with { Id="equivalent-recipe",Scenario=d.Cells[0].Scenario with {
                Party=d.Cells[0].Scenario.Party.Select(p=>p with { Build=p.Build with {
                    Equipment=p.Build.Equipment.Reverse().ToArray(),IdentityEssenceIds=p.Build.EssenceIds } }).ToArray() } }] },
            d with { Cells=[d.Cells[0] with { Scenario=d.Cells[0].Scenario with { Party=d.Cells[0].Scenario.Party.Take(4).ToArray() } }] },
            d with { Cohorts=[d.Cohorts[0] with { Budget=d.Cohorts[0].Budget with { CharacterLevel=40 } }] },
        };
        Assert.All(invalid,definition=>Assert.Throws<InvalidDataException>(()=>TowerBalanceEvaluator.Validate(definition)));
        using var temp=new DiscoveryTemp();var path=Path.Combine(temp.Path,"definition.json");
        HarnessJson.WriteNew(path,d);
        Assert.Equal(HarnessJson.Hash(d),HarnessJson.Hash(TowerBalanceEvaluator.Read(path)));
        var node=JsonNode.Parse(File.ReadAllText(path))!.AsObject();node.Remove("maximumBattles");
        File.WriteAllText(path,node.ToJsonString());
        Assert.Throws<JsonException>(()=>TowerBalanceEvaluator.Read(path));
    }

    // Independent fixtures generated with Python statistics.NormalDist.inv_cdf and the Wilson formula.
    [Theory]
    [InlineData(0,1000,1,0,0.003826758485555122)]
    [InlineData(1000,1000,1,0.996173241514445,1)]
    [InlineData(300,1000,1,0.2724068424770048,0.3291238609172172)]
    [InlineData(300,1000,2,0.2685846065411162,0.3334149026273171)]
    [InlineData(300,1000,5,0.2640907919202647,0.3385456740410979)]
    [InlineData(300,1000,16,0.259057680538018,0.3444052818119727)]
    [InlineData(300,1000,100,0.2521988602330657,0.3525893929532516)]
    [InlineData(300,1000,1000,0.2448524598705133,0.3616203192294865)]
    [InlineData(1,10,5,0.01185150341103292,0.5072317712328934)]
    [InlineData(5,10,5,0.1842255182472355,0.8157744817527646)]
    public void Family_adjusted_intervals_match_independent_numerical_fixtures(int wins,int n,int family,double lower,double upper)
    {
        var actual=TowerBalanceEvaluator.Wilson(wins,n,family)!;
        Assert.InRange(Math.Abs(actual.Lower-lower),0,1e-9);
        Assert.InRange(Math.Abs(actual.Upper-upper),0,1e-9);
        Assert.Equal(1-.05/family,actual.Confidence);
    }

    [Fact]
    public async Task Evaluator_CLI_reads_verified_Tower_archives_and_reports_corruption_as_invalid()
    {
        using var temp=new DiscoveryTemp();var d=Plan(samples:1);
        var scenario=Path.Combine(temp.Path,"scenario.json");HarnessJson.WriteNew(scenario,d.Cells[0].Scenario);
        var run=Path.Combine(temp.Path,"run");await TowerBundle.CreateAsync(Root,scenario,run);
        var evidence=TowerBalanceRuns.Read(d.Cells[0].Id,run);
        Assert.Null(evidence.Error);
        Assert.Equal("Complete",evidence.Status);
        var expected=TowerBalanceEvaluator.Evaluate(d,[evidence]);
        var definition=Path.Combine(temp.Path,"definition.json");HarnessJson.WriteNew(definition,d);
        var sources=Path.Combine(temp.Path,"sources.json");HarnessJson.WriteNew(sources,new[] { new TowerBalanceRunSource(d.Cells[0].Id,"run") });
        var output=Path.Combine(temp.Path,"assessment");
        Assert.Equal(expected.ExitCode,await BalanceHarness.Program.Main(["tower-balance-evaluate","--definition",definition,"--sources",sources,"--output",output]));
        var report=HarnessJson.Read<TowerBalanceReport>(Path.Combine(output,"assessment.json"));
        Assert.Equal(HarnessJson.Hash(expected),HarnessJson.Hash(report));
        var reconstructed=TowerBalanceRuns.Evaluate(Path.Combine(output,"definition.json"),Path.Combine(output,"sources.json"),Path.Combine(temp.Path,"reconstructed"));
        Assert.Equal(HarnessJson.Hash(report),HarnessJson.Hash(reconstructed));
        Assert.DoesNotContain("no approved",File.ReadAllText(Path.Combine(output,"assessment.md")),StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Approximate",report.Scope);
        File.AppendAllText(Path.Combine(run,"battles/tower.0001.json")," ");
        var broken=TowerBalanceRuns.Read(d.Cells[0].Id,run);
        Assert.Equal("Invalid",broken.Status);
        Assert.Equal(GoalOutcome.Invalid,TowerBalanceEvaluator.Evaluate(d,[broken]).Assessment);
        Assert.Equal("Invalid",TowerBalanceRuns.Read("missing",Path.Combine(temp.Path,"absent")).Status);
    }
}
