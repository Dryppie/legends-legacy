using Domain.Models.RegionBosses;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossMatchmakerTests
{
    [Theory]
    [InlineData(1, new[] { 1 })]
    [InlineData(3, new[] { 3 })]
    [InlineData(5, new[] { 5 })]
    [InlineData(6, new[] { 3, 3 })]
    [InlineData(7, new[] { 4, 3 })]
    [InlineData(8, new[] { 4, 4 })]
    [InlineData(9, new[] { 5, 4 })]
    [InlineData(11, new[] { 4, 4, 3 })]
    public void Match_balances_parties_without_exceeding_five(int signupCount, int[] expectedSizes)
    {
        var parties = RegionBossMatchmaker.Match(EventId, CreateSignups(signupCount));

        Assert.Equal(expectedSizes, parties.Select(x => x.Members.Count));
        Assert.All(parties, party => Assert.InRange(party.Members.Count, 1, RegionBossRules.MaximumPartySize));
        Assert.Equal(signupCount, parties.SelectMany(x => x.Members).Select(x => x.CharacterId).Distinct().Count());
    }

    [Fact]
    public void Match_is_deterministic_for_the_same_event()
    {
        var signups = CreateSignups(17);

        var first = RegionBossMatchmaker.Match(EventId, signups);
        var second = RegionBossMatchmaker.Match(EventId, signups.AsEnumerable().Reverse().ToArray());

        Assert.Equal(
            first.SelectMany(x => x.Members).Select(x => x.CharacterId),
            second.SelectMany(x => x.Members).Select(x => x.CharacterId));
    }

    [Fact]
    public void Match_keeps_broad_power_bands_ordered()
    {
        var signups = CreateSignups(10);
        for (var index = 0; index < signups.Count; index++)
            signups[index].PowerRating = index < 5 ? 1_000 + index : 30_000 + index;

        var parties = RegionBossMatchmaker.Match(EventId, signups);

        Assert.Equal(2, parties.Count);
        Assert.All(parties[0].Members, member => Assert.True(member.PowerRating < 5_000));
        Assert.All(parties[1].Members, member => Assert.True(member.PowerRating >= 30_000));
    }

    private static readonly Guid EventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static List<RegionBossSignup> CreateSignups(int count) =>
        Enumerable.Range(1, count).Select(index => new RegionBossSignup
        {
            CharacterId = Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
            PowerRating = 10_000 + index
        }).ToList();
}
