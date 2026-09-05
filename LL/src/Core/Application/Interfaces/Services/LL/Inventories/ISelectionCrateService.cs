using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Inventories;

public sealed record SelectionCrateOpenResult(
    bool IsSuccess,
    string? ErrorMessage,
    IReadOnlyList<InventoryItem> Rewards,
    string? ContainerName = null,
    bool RewardsAlreadyPublished = false);

public interface ISelectionCrateService
{
    Task<SelectionCrateOpenResult> OpenSelectionContainerAsync(
        Guid characterId,
        Guid containerItemInstanceId,
        string optionId,
        CancellationToken cancellationToken);
}
