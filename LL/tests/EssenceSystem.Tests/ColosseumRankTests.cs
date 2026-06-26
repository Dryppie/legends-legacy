using Domain.Models.Colosseum;

public sealed class ColosseumRankTests
{
    [Theory]
    [InlineData(1099, "bronze")]
    [InlineData(1100, "silver")]
    [InlineData(1249, "silver")]
    [InlineData(1250, "gold")]
    [InlineData(1449, "gold")]
    [InlineData(1450, "platinum")]
    [InlineData(1699, "platinum")]
    [InlineData(1700, "diamond")]
    [InlineData(1999, "diamond")]
    [InlineData(2000, "champion")]
    [InlineData(2299, "champion")]
    [InlineData(2300, "ascendant")]
    public void GetTier_UsesExpectedRatingBoundaries(int rating, string expectedTierId)
    {
        var tier = ArenaRank.GetTier(rating);

        Assert.Equal(expectedTierId, tier.Id);
    }

    [Fact]
    public void GetProgress_ReturnsRemainingRatingUntilNextTier()
    {
        var progress = ArenaRank.GetProgress(1240);

        Assert.Equal("silver", progress.CurrentTierId);
        Assert.Equal("Gold", progress.NextTierName);
        Assert.Equal(10, progress.RatingUntilNextTier);
    }
}
