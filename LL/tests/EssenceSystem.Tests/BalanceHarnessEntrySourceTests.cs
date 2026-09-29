using BalanceHarness;
using Domain.Models.Prophecies;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessEntrySourceTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static readonly Guid Owner = Guid.Parse("448219de-9df5-49ae-aafa-d01776360dac");
    private static readonly DateTimeOffset Monday = new(2026,9,28,0,0,0,TimeSpan.Zero);

    [Fact]
    public async Task Unfunded_assembly_and_incomplete_claim_preserve_personal_stock()
    {
        var model = new TowerEntrySources(Root, Owner);
        var daily = model.AcceptConditionalOffer("daily.combat.kills.common", Monday, Monday);
        await model.TrackKills(299, Monday.AddHours(1), default);
        Assert.False(await model.Claim(daily, Monday.AddHours(1), default));
        var before = HarnessJson.Hash(model.State());
        Assert.False(await model.Assemble("goblin_mines", 1, default));
        Assert.False(await model.Assemble("goblin_mines", 0, default));
        Assert.Equal(before, HarnessJson.Hash(model.State()));
        await model.TrackKills(1, Monday.AddHours(2), default);
        Assert.True(await model.Claim(daily, Monday.AddHours(2), default));
        Assert.Equal(2, model.State().Items["sigil_fragment"]);
        Assert.Equal(1, model.DuplicateClaimsRejected);
        Assert.False(await model.Claim(daily, Monday.AddHours(3), default));
        Assert.Empty(new TowerEntrySources(Root, Guid.NewGuid()).State().Items);
    }

    [Fact]
    public async Task Progress_is_not_retroactive_and_expires_at_the_UTC_boundary()
    {
        var model = new TowerEntrySources(Root, Owner);
        var daily = model.AcceptConditionalOffer("daily.combat.kills.common", Monday, Monday.AddHours(1));
        await model.TrackKills(300, Monday.AddMinutes(30), default);
        Assert.Equal(0, daily.CurrentValue);
        await model.TrackKills(299, Monday.AddHours(2), default);
        await model.TrackKills(1, Monday.AddDays(1), default);
        Assert.Equal(299, daily.CurrentValue);
        Assert.False(await model.Claim(daily, Monday.AddDays(1), default));
        Assert.Throws<InvalidDataException>(() => model.AcceptConditionalOffer("daily.combat.kills.common", Monday, Monday));
        Assert.Throws<InvalidDataException>(() => model.AcceptConditionalOffer("weekly.combat.kills", Monday.AddDays(1), Monday.AddDays(1)));
    }

    [Theory]
    [InlineData("perfect", "daily-common", 3, 4)]
    [InlineData("four-of-five", "daily-common", 3, 4)]
    [InlineData("perfect", "daily-common-weekly-kills", 4, 2)]
    [InlineData("four-of-five", "daily-common-weekly-kills", 4, 2)]
    public async Task Shared_activity_funds_whole_sigils_without_spending_caches_or_double_counting_kills(
        string outcome, string policy, int sigils, int fragments)
    {
        var plan = TowerEntrySourceStudy.Read(Fixtures);
        var result = await TowerEntrySourceStudy.Schedule(Root, Owner, outcome, policy, 86400, plan, default);
        var final = result.Checkpoints.Last();
        Assert.Equal(sigils, final.State.Items["sigil_goblin_mines"]);
        Assert.Equal(fragments, final.State.Items["sigil_fragment"]);
        Assert.Equal(result.Claims.Count, result.DuplicateClaimsRejected);
        Assert.Equal(10, result.Claims.Count(c => c.Source == plan.DailyDefinition));
        Assert.All(result.Checkpoints.Take(3), c => Assert.Equal(0, c.AssembledNow));
        Assert.Equal(2, final.State.Items["revelation_cache_small"]);
        Assert.Equal(1, final.State.Items["revelation_cache_greater"]);
        Assert.Equal(1, final.State.Items["revelation_cache_perfect_week"]);
        Assert.True(final.State.UnappliedCharacterExperience > 0);
        if (policy == "daily-common-weekly-kills")
        {
            var weekly = Assert.Single(result.Claims.Where(c => c.Source == plan.WeeklyDefinition));
            Assert.Equal(35000, weekly.RequiredKills);
            Assert.Equal(1, final.State.Items["greater_prophecy_cache"]);
            Assert.Equal(Monday.AddSeconds(((outcome == "perfect" ? 35000 : 43749) - 1L) * 10), weekly.At);
        }
        else Assert.DoesNotContain("greater_prophecy_cache", final.State.Items.Keys);
    }

    private sealed class ExportFactAttribute : FactAttribute
    {
        public ExportFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_ENTRY_SOURCES"))) Skip = "Requires frozen source ledger request; zero combat."; }
    }
    [ExportFact]
    public async Task Export_costed_sources_without_transferring_changed_combat_outcomes()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await TowerEntrySourceStudy.Run(HarnessJson.Read<TowerEntrySourceRequest>(Environment.GetEnvironmentVariable("LL_TOWER_ENTRY_SOURCES")!), deadline.Token);
    }
}
