using Domain.Models.Combat;
using Services.LL.Interfaces;

namespace Services.LL.Colosseum;
public class Elo32Calculator : IRatingCalculator
{
    private const int K = 32;
    public (int newA, int newB) Calculate(int ratingA, int ratingB, BattleOutcome outcome)
    {
        double expectedA = 1 / (1 + Math.Pow(10, (ratingB - ratingA) / 400.0));
        double expectedB = 1 - expectedA;

        // outcome: 1 = A wins, 0.5 = draw, 0 = B wins
        double scoreA = outcome switch
        {
            BattleOutcome.Victory => 1,
            BattleOutcome.Draw => 0.5,
            _ => 0
        };
        double scoreB = 1 - scoreA;

        int newA = (int)Math.Round(ratingA + K * (scoreA - expectedA));
        int newB = (int)Math.Round(ratingB + K * (scoreB - expectedB));

        return (newA, newB);
    }

}