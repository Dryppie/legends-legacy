using Domain.Models.Combat;

namespace Domain.Models.Colosseum;

public static class ArenaRewards
{
    public const int GloryPerBattle = 10;
    public const int DailyFirstWinGlory = 20;

    public static (int BaseGlory, int DailyFirstWinBonus) AwardBattleGlory(
        CharacterArenaProfile attacker,
        CharacterArenaProfile defender,
        BattleOutcome outcome,
        DateTimeOffset now)
    {
        var firstWinBonus = outcome == BattleOutcome.Victory && CanReceiveDailyFirstWinBonus(attacker, now)
            ? DailyFirstWinGlory
            : 0;

        attacker.Glory += GloryPerBattle + firstWinBonus;
        defender.Glory += GloryPerBattle;
        if (firstWinBonus > 0)
        {
            attacker.LastFirstWinBonusAt = now;
        }

        return (GloryPerBattle, firstWinBonus);
    }

    public static bool CanReceiveDailyFirstWinBonus(CharacterArenaProfile arena, DateTimeOffset now) =>
        arena.LastFirstWinBonusAt?.UtcDateTime.Date != now.UtcDateTime.Date;
}
