using Domain.Models.Colosseum.Tournaments;

namespace EssenceSystem.Tests;

public sealed class TournamentGroundsRulesTests
{
    [Theory]
    [InlineData(2, 2, 0)]
    [InlineData(4, 4, 0)]
    [InlineData(5, 8, 3)]
    [InlineData(17, 32, 15)]
    public void Bracket_size_uses_next_power_of_two_and_calculates_byes(
        int participants,
        int expectedBracketSize,
        int expectedByes)
    {
        Assert.Equal(expectedBracketSize, TournamentRules.GetBracketSize(participants));
        Assert.Equal(expectedByes, TournamentRules.GetByeCount(participants));
    }

    [Theory]
    [InlineData(1, 4, "Round 1")]
    [InlineData(2, 4, "Quarter-final")]
    [InlineData(3, 4, "Semi-final")]
    [InlineData(4, 4, "Final")]
    public void Round_names_follow_single_elimination_distance_from_final(
        int roundNumber,
        int roundCount,
        string expectedName)
    {
        Assert.Equal(expectedName, TournamentRules.GetRoundName(roundNumber, roundCount));
    }

    [Theory]
    [InlineData(4, 4, 2)]
    [InlineData(4, 3, 3)]
    [InlineData(4, 2, 5)]
    [InlineData(4, 1, 9)]
    public void Placement_bands_are_derived_from_elimination_round(
        int roundCount,
        int eliminatedRound,
        int expectedPlacement)
    {
        Assert.Equal(expectedPlacement, TournamentRules.CalculatePlacement(roundCount, eliminatedRound));
    }
}
