using Domain.Models.Colosseum;
using Domain.Models.Combat;

public sealed class ColosseumRewardTests
{
    [Theory]
    [InlineData(BattleOutcome.Victory, 12)]
    [InlineData(BattleOutcome.Draw, 8)]
    [InlineData(BattleOutcome.Defeat, 5)]
    public void CalculateAttackGlory_ReturnsBaseGloryByOutcome(BattleOutcome outcome, int expectedBaseGlory)
    {
        var reward = ArenaRewards.CalculateAttackGlory(outcome, canReceiveDailyFirstWinBonus: false);

        Assert.Equal(expectedBaseGlory, reward.BaseGlory);
        Assert.Equal(0, reward.DailyFirstWinBonus);
    }

    [Fact]
    public void CalculateAttackGlory_AddsDailyFirstWinBonusOnlyForVictory()
    {
        var victory = ArenaRewards.CalculateAttackGlory(BattleOutcome.Victory, canReceiveDailyFirstWinBonus: true);
        var draw = ArenaRewards.CalculateAttackGlory(BattleOutcome.Draw, canReceiveDailyFirstWinBonus: true);

        Assert.Equal(20, victory.DailyFirstWinBonus);
        Assert.Equal(0, draw.DailyFirstWinBonus);
    }
}
