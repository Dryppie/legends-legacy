using Domain.Models.Colosseum;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

public sealed class ColosseumMatchResultTests
{
    [Fact]
    public void SetCombatResult_round_trips_the_final_summary()
    {
        var match = new ColosseumMatchResult();
        match.SetCombatResult(new CombatResult
        {
            Outcome = BattleOutcome.Victory,
            Duration = 125,
            EntityStats =
            [
                new EntityStats(
                    Guid.NewGuid().ToString(),
                    "Hero",
                    [],
                    DamageDone: 321)
            ]
        });

        var summary = match.CombatResult;

        Assert.NotNull(match.CombatResultJson);
        Assert.NotNull(summary);
        Assert.Equal(BattleOutcome.Victory, summary.Outcome);
        Assert.Equal(125, summary.Duration);
        Assert.Equal(321, Assert.Single(summary.EntityStats).DamageDone);
    }
}
