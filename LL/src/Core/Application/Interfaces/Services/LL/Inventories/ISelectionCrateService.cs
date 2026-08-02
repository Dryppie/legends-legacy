using Domain.Models.Inventories;

namespace Application.Interfaces.Services.LL.Inventories;

public sealed record SelectionCrateOpenResult(
    bool IsSuccess,
    string? ErrorMessage,
    IReadOnlyList<InventoryItem> Rewards);

public interface ISelectionCrateService
{
    Task<SelectionCrateOpenResult> OpenCatalystSelectionCrateAsync(
        Guid characterId,
        Guid crateItemInstanceId,
        string optionId,
        CancellationToken cancellationToken);
}
