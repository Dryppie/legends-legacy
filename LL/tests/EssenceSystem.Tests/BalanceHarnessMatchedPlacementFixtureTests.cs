using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessMatchedPlacementFixtureTests
{
    [Fact]
    public void Both_profiles_bind_identical_v5_plans_for_all_twelve_roots()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture entered combat.")).Activate();
        var (context, source, _) = BalanceHarnessProposalStudyTests.Fixture(placement: true);
        var before = HarnessJson.Hash(context);
        var baseline = ProposalStudyFixtureHost.FixturePlan(context, source, ProposalStudyFixtureHost.MatchedBaseline);
        var placement = ProposalStudyFixtureHost.FixturePlan(context, source, ProposalStudyFixtureHost.MatchedPlacement);
        Assert.Equal(TowerProposalComparison.AlliedActionVersion, baseline.Version);
        Assert.Equal(HarnessJson.Hash(source), HarnessJson.Hash(placement));
        Assert.Equal(before, HarnessJson.Hash(context));
        var values = Enumerable.Range(200000, 4380).ToArray();
        var a = TowerProposalComparison.Bind(baseline, context, values);
        var b = TowerProposalComparison.Bind(placement, context, values);
        Assert.Equal(12, a.Pairs.Count);
        Assert.Equal(a.Pairs.Select(p => HarnessJson.Hash(p.Candidate)), b.Pairs.Select(p => HarnessJson.Hash(p.Control)));
        Assert.Equal(a.Pairs.Select(p => HarnessJson.Hash(p.HeldoutSeeds)), b.Pairs.Select(p => HarnessJson.Hash(p.HeldoutSeeds)));
        Assert.Equal(HarnessJson.Hash(source), HarnessJson.Hash(ProposalStudyFixtureHost.FixturePlan(context, source, null)));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("actors")]
    [InlineData("slots")]
    [InlineData("source")]
    public void Mismatched_or_unknown_profile_is_rejected_before_preparation(string field)
    {
        var (context, source, _) = BalanceHarnessProposalStudyTests.Fixture(placement: true);
        if (field == "actors") context = context with { Scope = context.Scope with { RequiredPartySize = 5 } };
        if (field == "slots") context = context with { Scope = context.Scope with { Budget = context.Scope.Budget with { EssenceSlots = 4 } } };
        if (field == "source") source = source with { Version = TowerProposalComparison.AlliedActionVersion };
        Assert.Throws<InvalidDataException>(() => ProposalStudyFixtureHost.FixturePlan(context, source,
            field == "unknown" ? field : ProposalStudyFixtureHost.MatchedBaseline));
    }

    [Fact]
    public void Literal_rules_cover_training_nomination_validation_boundaries_and_heldout()
    {
        foreach (var root in Enumerable.Range(1, 12))
        {
            Assert.Equal(BattleOutcome.Draw, ProposalStudyFixtureHost.LiteralOutcome(root, false, true, true, 0));
            Assert.Equal(BattleOutcome.Victory, ProposalStudyFixtureHost.LiteralOutcome(root, false, false, false, 0));
            var outcomes = Enumerable.Range(0, 60).Select(i => ProposalStudyFixtureHost.LiteralOutcome(root, true, false, false, i));
            Assert.Equal(root % 2 == 1 ? 5 : 4, outcomes.Count(o => o == BattleOutcome.Victory));
            Assert.All(Enumerable.Range(0, 60), i => Assert.Equal(BattleOutcome.Draw,
                ProposalStudyFixtureHost.LiteralOutcome(root, true, true, true, i)));
        }
        Assert.Equal(BattleOutcome.Victory, ProposalStudyFixtureHost.LiteralOutcome(0, false, true, true, 0));
        Assert.Equal(BattleOutcome.Draw, ProposalStudyFixtureHost.LiteralOutcome(1, true, false, false, -1));
    }
}
