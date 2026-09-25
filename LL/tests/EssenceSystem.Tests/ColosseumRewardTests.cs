using Domain.Models.Colosseum;
using Domain.Models.Combat;

public sealed class ColosseumRewardTests
{
    [Theory]
    [InlineData(BattleOutcome.Victory)]
    [InlineData(BattleOutcome.Draw)]
    [InlineData(BattleOutcome.Defeat)]
    public void AwardBattleGlory_GrantsTenToBothParticipantsForEveryOutcome(BattleOutcome outcome)
    {
        var now = new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);
        var attacker = new CharacterArenaProfile { Glory = 25, LastFirstWinBonusAt = now };
        var defender = new CharacterArenaProfile { Glory = 40 };

        var reward = ArenaRewards.AwardBattleGlory(attacker, defender, outcome, now);

        Assert.Equal(10, reward.BaseGlory);
        Assert.Equal(0, reward.DailyFirstWinBonus);
        Assert.Equal(35, attacker.Glory);
        Assert.Equal(50, defender.Glory);
    }

    [Fact]
    public void AwardBattleGlory_AddsFirstWinBonusOncePerUtcDay()
    {
        var now = new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);
        var attacker = new CharacterArenaProfile();
        var defender = new CharacterArenaProfile();

        var firstWin = ArenaRewards.AwardBattleGlory(attacker, defender, BattleOutcome.Victory, now);
        var secondWin = ArenaRewards.AwardBattleGlory(attacker, defender, BattleOutcome.Victory, now.AddHours(1));
        var nextDayWin = ArenaRewards.AwardBattleGlory(attacker, defender, BattleOutcome.Victory, now.AddDays(1));

        Assert.Equal(20, firstWin.DailyFirstWinBonus);
        Assert.Equal(0, secondWin.DailyFirstWinBonus);
        Assert.Equal(20, nextDayWin.DailyFirstWinBonus);
        Assert.Equal(70, attacker.Glory);
        Assert.Equal(30, defender.Glory);
        Assert.Equal(now.AddDays(1), attacker.LastFirstWinBonusAt);
    }

    [Theory]
    [InlineData(BattleOutcome.Draw)]
    [InlineData(BattleOutcome.Defeat)]
    public void AwardBattleGlory_DoesNotConsumeFirstWinBonusOnNonVictory(BattleOutcome outcome)
    {
        var now = new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);
        var attacker = new CharacterArenaProfile();
        var defender = new CharacterArenaProfile();

        var reward = ArenaRewards.AwardBattleGlory(attacker, defender, outcome, now);

        Assert.Equal(0, reward.DailyFirstWinBonus);
        Assert.Null(attacker.LastFirstWinBonusAt);
        Assert.True(ArenaRewards.CanReceiveDailyFirstWinBonus(attacker, now));
        Assert.Equal(10, attacker.Glory);
        Assert.Equal(10, defender.Glory);
    }
}
