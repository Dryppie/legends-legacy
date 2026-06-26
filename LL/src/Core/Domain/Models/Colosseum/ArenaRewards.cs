using Domain.Models.Combat;

namespace Domain.Models.Colosseum;

public static class ArenaRewards
{
    public const int DailyFirstWinGlory = 20;

    public static (int BaseGlory, int DailyFirstWinBonus) CalculateAttackGlory(BattleOutcome outcome, bool canReceiveDailyFirstWinBonus)
    {
        var baseGlory = outcome switch
        {
            BattleOutcome.Victory => 12,
            BattleOutcome.Draw => 8,
            _ => 5
        };

        var firstWinBonus = outcome == BattleOutcome.Victory && canReceiveDailyFirstWinBonus
            ? DailyFirstWinGlory
            : 0;

        return (baseGlory, firstWinBonus);
    }
}
