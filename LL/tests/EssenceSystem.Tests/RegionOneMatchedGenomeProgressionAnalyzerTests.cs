using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class RegionOneMatchedGenomeProgressionAnalyzerTests
{
    [Theory]
    [InlineData(4, 15)]
    [InlineData(5, 6)]
    [InlineData(6, 1)]
    public void Combinations_enumerate_each_unique_subset_once(int subsetSize, int expectedCount)
    {
        string[] genome = ["A", "B", "C", "D", "E", "F"];

        var combinations = RegionOneMatchedGenomeProgressionAnalyzer.Combinations(genome, subsetSize);

        Assert.Equal(expectedCount, combinations.Count);
        Assert.All(combinations, combination => Assert.Equal(subsetSize, combination.Count));
        Assert.Equal(
            expectedCount,
            combinations.Select(combination => string.Join('|', combination)).Distinct().Count());
        Assert.All(combinations, combination => Assert.True(combination.SequenceEqual(combination.Order())));
    }
}
