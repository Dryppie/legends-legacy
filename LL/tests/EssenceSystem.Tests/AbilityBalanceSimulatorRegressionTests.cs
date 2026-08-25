using Application.Interfaces.Services.LL.Essences;
using Services.LL.Combat.Engine;
using System.Reflection;

namespace EssenceSystem.Tests;

public sealed class AbilityBalanceSimulatorRegressionTests
{
    [Fact]
    public void Adjusted_regression_uses_the_number_of_team_copies()
    {
        var duplicateA = Combination(
            "duplicate-a",
            [
                new AbilityBalanceParticipantLoadout(["essence-a", "essence-b"]),
                new AbilityBalanceParticipantLoadout(["essence-a"])
            ],
            wins: 70);
        var duplicateB = Combination(
            "duplicate-b",
            [
                new AbilityBalanceParticipantLoadout(["essence-a", "essence-b"]),
                new AbilityBalanceParticipantLoadout(["essence-b"])
            ],
            wins: 30);
        var method = typeof(AbilityBalanceSimulator).GetMethod(
            "CalculateAdjustedEssenceDeltas",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = Assert.IsAssignableFrom<IReadOnlyDictionary<string, double>>(
            method!.Invoke(null, new object[] { new[] { duplicateA, duplicateB } }));

        Assert.True(result["essence-a"] > 0.05d);
        Assert.True(result["essence-b"] < -0.05d);
    }

    private static AbilityBalanceCombinationResult Combination(
        string signature,
        IReadOnlyList<AbilityBalanceParticipantLoadout> participants,
        int wins) =>
        new(
            signature,
            signature,
            participants,
            100,
            wins,
            100 - wins,
            0,
            wins / 100d,
            (100 - wins) / 100d,
            0,
            100,
            100,
            100);
}
