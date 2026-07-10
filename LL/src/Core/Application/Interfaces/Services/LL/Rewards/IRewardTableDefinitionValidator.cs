using Domain.Models.Rewards;

namespace Application.Interfaces.Services.LL.Rewards;

public interface IRewardTableDefinitionValidator
{
    IReadOnlyList<string> Validate(
        IReadOnlyList<RewardTableDefinition> definitions,
        IReadOnlySet<string>? itemIds = null);

    void ThrowIfInvalid(
        IReadOnlyList<RewardTableDefinition> definitions,
        IReadOnlySet<string>? itemIds = null);
}
