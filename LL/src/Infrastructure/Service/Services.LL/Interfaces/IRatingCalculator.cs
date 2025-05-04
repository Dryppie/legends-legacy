using Domain.Models.Combat;

namespace Services.LL.Interfaces;
public interface IRatingCalculator
{
    (int newA, int newB) Calculate(int ratingA, int ratingB, BattleOutcome outcome);
}