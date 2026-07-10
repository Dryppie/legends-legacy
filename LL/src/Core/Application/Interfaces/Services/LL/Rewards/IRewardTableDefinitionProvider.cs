using Domain.Models.Rewards;

namespace Application.Interfaces.Services.LL.Rewards;

public interface IRewardTableDefinitionProvider
{
    RewardTableDefinition GetById(string id);
    RewardTableDefinition? FindById(string id);
    IReadOnlyList<RewardTableDefinition> GetAll();
}
