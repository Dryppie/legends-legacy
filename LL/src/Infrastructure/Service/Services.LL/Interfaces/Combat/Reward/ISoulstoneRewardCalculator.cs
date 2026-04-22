namespace Services.LL.Interfaces.Combat.Reward;

public interface ISoulstoneRewardCalculator
{
    int Calculate(
        int durationInSeconds,
        double dropRatePercent,
        double doubleDropChancePercent);
}