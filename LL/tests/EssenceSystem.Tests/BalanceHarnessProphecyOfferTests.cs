using BalanceHarness;
using Domain.Models.Prophecies;

namespace EssenceSystem.Tests;

[Trait("Category","BalanceHarness")]
public sealed class BalanceHarnessProphecyOfferTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root,"../../../tools/BalanceHarness/Fixtures"));
    private static readonly Guid Owner = Guid.Parse("448219de-9df5-49ae-aafa-d01776360dac");
    private static readonly DateTimeOffset Monday = new(2026,9,28,0,0,0,TimeSpan.Zero);

    [Fact]
    public async Task Native_offers_persist_and_accept_only_one_visible_daily()
    {
        var model = new TowerEntrySources(Root,Owner);
        var first = await model.Overview(Monday,default);
        var replay = await model.Overview(Monday.AddHours(1),default);
        Assert.Equal(first.DailyProphecies.Select(p => p.Id),replay.DailyProphecies.Select(p => p.Id));
        Assert.Equal(3,first.DailyProphecies.Select(p => p.ProphecyDefinitionId).Distinct().Count());
        Assert.Equal(ProphecyStatus.Accepted,first.GreaterProphecy.Status);
        Assert.DoesNotContain(first.DailyProphecies.Append(first.GreaterProphecy),p => p.ProphecyDefinition!.MinPlayerLevel > 30);
        Assert.False(await model.AcceptGeneratedOffer(Guid.Empty,Monday,default));
        var selected = first.DailyProphecies.First();
        Assert.True(await model.AcceptGeneratedOffer(selected.Id,Monday,default));
        Assert.False(await model.AcceptGeneratedOffer(first.DailyProphecies.Last().Id,Monday,default));
        Assert.Equal(2,first.DailyProphecies.Count(p => p.Status == ProphecyStatus.Declined));
        var next = await model.Overview(Monday.AddDays(1),default);
        Assert.DoesNotContain(next.DailyProphecies,p => p.Id == selected.Id);
        Assert.Equal(first.GreaterProphecy.Id,next.GreaterProphecy.Id);
        Assert.False(await model.AcceptGeneratedOffer(selected.Id,Monday.AddDays(1),default));
        var week = await model.Overview(Monday.AddDays(7),default);
        Assert.NotEqual(first.GreaterProphecy.Id,week.GreaterProphecy.Id);
        Assert.Equal(0,week.WeeklyRevelation.PropheticFavor);
    }

    [Theory]
    [InlineData("perfect")]
    [InlineData("four-of-five")]
    public async Task Native_schedule_replays_with_fixed_offers_shared_activity_and_no_unfunded_objectives(string outcome)
    {
        var plan = TowerProphecyOffers.Read(Fixtures);
        var result = await TowerProphecyOffers.Schedule(Root,Owner,outcome,86400,plan,default);
        var replay = await TowerProphecyOffers.Schedule(Root,Owner,outcome,86400,plan,default);
        Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(replay));
        Assert.Equal(10,result.Offers!.Count);
        Assert.Equal(result.Claims.Count,result.DuplicateClaimsRejected);
        Assert.All(result.Claims,c => Assert.Contains(c.ObjectiveType,new[] {"KillCreatures","WinEncounters","Favor"}));
        foreach (var day in result.Offers)
        {
            var expected = day.Daily.Where(d => d.Objective == "KillCreatures")
                .OrderByDescending(d => d.Reward.SigilFragments).ThenBy(d => d.Target).ThenBy(d => d.Definition,StringComparer.Ordinal).FirstOrDefault();
            Assert.Equal(expected?.Definition,day.Selected);
            Assert.All(day.FinalDaily.Where(d => d.Definition != day.Selected),p => Assert.Equal(0,p.Progress));
            if (day.Weekly.Objective is not ("KillCreatures" or "WinEncounters")) Assert.Equal(0,day.FinalWeekly.Progress);
        }
    }

    private sealed class ProjectionFactAttribute : FactAttribute
    {
        public ProjectionFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_PROPHECY_PROJECTION"))) Skip = "Requires frozen native-offer projection; zero new combat."; }
    }
    [ProjectionFact]
    public async Task Project_native_offers_before_changed_entries()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await TowerEntrySourceStudy.RunOffers(HarnessJson.Read<TowerEntrySourceRequest>(Environment.GetEnvironmentVariable("LL_TOWER_PROPHECY_PROJECTION")!),deadline.Token);
    }
    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_PROPHECY_COMBAT"))) Skip = "Requires frozen bounded combat owner."; }
    }
    [StudyFact]
    public async Task Frozen_native_offer_comparison()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerActivityStudy.RunProphecyOffersAsync(HarnessJson.Read<TowerActivityRequest>(Environment.GetEnvironmentVariable("LL_TOWER_PROPHECY_COMBAT")!),deadline.Token);
    }
}
