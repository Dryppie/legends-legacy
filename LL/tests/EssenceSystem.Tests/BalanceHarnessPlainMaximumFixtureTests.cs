using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPlainMaximumFixtureTests
{
    [Fact]
    public void Maximum_rule_keeps_references_drawing_and_passes_every_generated_nominee()
    {
        foreach (var index in Enumerable.Range(0, 60))
        {
            Assert.Equal(BattleOutcome.Draw, PlainMaximumFixtureHost.Outcome(false, true, true, index));
            Assert.Equal(index < 5 ? BattleOutcome.Victory : BattleOutcome.Draw, PlainMaximumFixtureHost.Outcome(false, false, true, index));
            Assert.Equal(BattleOutcome.Victory, PlainMaximumFixtureHost.Outcome(true, true, false, index));
        }
        Assert.Equal(BattleOutcome.Draw, PlainMaximumFixtureHost.Outcome(false, false, true, -1));
        Assert.Equal(BattleOutcome.Draw, PlainMaximumFixtureHost.Outcome(false, true, false, 0));
        Assert.Equal(BattleOutcome.Victory, PlainMaximumFixtureHost.Outcome(false, false, false, 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Captured_context_reaches_all_36_distinct_members_before_any_heldout_observation(bool nomination)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Literal maximum fixture entered combat.")).Activate();
        var source = Environment.GetEnvironmentVariable("LL_PLAIN_MAXIMUM_SOURCE");
        var (context, plan, _) = source is null ? BalanceHarnessProposalStudyTests.Fixture(placement: true) : (
            HarnessJson.Read<TowerProposalContext>(Path.Combine(source, "runtime-context.json")),
            HarnessJson.Read<TowerProposalComparisonPlan>(Path.Combine(source, "runtime-plan.json")), Array.Empty<int>());
        if (nomination) plan = TowerProposalComparison.CreateNominationPlan(context,
            TowerProposalPolicies.BenchmarkAffinityCreation(plan.Control.CreatedDamageAffinityIds!));
        (context, plan, _) = PlainMaximumFixtureHost.BindHistory(context, plan);
        var references = context.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
        var benchmark = context.Scope.Starts.Single(s => s.ReferenceId == context.BenchmarkReferenceId).Party.Id;
        var completed = 0; var measured = 0;
        var result = await S.Execute(plan, context, PlainMaximumFixtureHost.LiteralAllocation(context).Selected,
            pair => TowerProposalComparison.ExecutePairAsync(plan, pair, (_, _) => { }, async (_, p) => {
                var search = await TowerProposalPolicies.RunAsync(p, (r, _) => Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(r),
                    "trial-" + r.Ordinal, r.Seed, nomination
                        ? PlainMaximumFixtureHost.NominationOutcome(false, references.Contains(r.PartyId), r.PartyId == benchmark,
                            r.Role == TowerBenchmarkValidation.NominationRole, r.Role == TowerBenchmarkValidation.ValidationRole,
                            r.Scenario.Seeds.ToList().IndexOf(r.Seed))
                        : PlainMaximumFixtureHost.Outcome(false, references.Contains(r.PartyId),
                            r.Role == TowerBenchmarkValidation.ValidationRole, r.Scenario.Seeds.ToList().IndexOf(r.Seed)), 0, 100, 1)),
                    token: default, checkpoint: null, panelFreezesOnly: true);
                completed++; return search;
            }), (freeze, r) => {
                Assert.Equal(24, completed); Assert.Equal(12, freeze.Families.Count);
                Assert.All(freeze.Families, f => Assert.Equal(3, f.Members.Count));
                measured++;
                return Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(r), "heldout-" + r.Ordinal, r.Seed, BattleOutcome.Victory, 0, 100, 1));
            }, (_, _) => { }, () => new string('a', 64), _ => { }, default);
        Assert.Equal((21888, 12672, 9216, 9216, 12), (result.Fights, result.SearchFights, result.HeldoutFights, measured, result.DifferingRoots));
    }

    [Fact]
    public void History_binding_adds_declared_values_without_changing_physical_context()
    {
        var (context, plan, _) = BalanceHarnessProposalStudyTests.Fixture(placement: true);
        context = context with { Scope = context.Scope with { ExcludedCombatSeeds = [] } };
        var bound = PlainMaximumFixtureHost.BindHistory(context, plan);
        var declared = S.LegacySeeds(context).Concat(context.Scope.Generation.Seeds).Append(context.RootSeed)
            .Concat(context.Scope.References.SelectMany(r => r.Scenario.Seeds));
        Assert.All(declared, seed => Assert.Contains(seed, bound.History));
        Assert.Equal(bound.History.Except(S.LegacySeeds(context)).Order(), bound.Context.Scope.ExcludedCombatSeeds);
        Assert.Equal(HarnessJson.Hash(context), HarnessJson.Hash(bound.Context with {
            Scope = bound.Context.Scope with { ExcludedCombatSeeds = context.Scope.ExcludedCombatSeeds } }));
        Assert.Equal(HarnessJson.Hash(bound.Plan), HarnessJson.Hash(TowerProposalComparison.Recreate(plan, bound.Context)));
        var again = PlainMaximumFixtureHost.BindHistory(bound.Context, bound.Plan);
        Assert.Equal(HarnessJson.Hash(bound.Context), HarnessJson.Hash(again.Context));
        Assert.Equal(HarnessJson.Hash(bound.Plan), HarnessJson.Hash(again.Plan));
        Assert.Equal(bound.History, again.History);
    }
}
