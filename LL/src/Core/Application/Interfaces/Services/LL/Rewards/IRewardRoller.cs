using Domain.Models.Rewards;

namespace Application.Interfaces.Services.LL.Rewards;

public interface IRewardRoller
{
    RewardRollResult Roll(string rewardTableId, RewardRollContext context);
    RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context);
}
