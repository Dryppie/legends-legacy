using Domain.Models.Items.Equipments.Progression;

namespace Application.Interfaces.Services.LL.Items;

public interface IEquipmentUpgradeService
{
    Task<IReadOnlyList<EquipmentBlueprintOption>> GetBlueprintsAsync(Guid characterId, Guid itemInstanceId, CancellationToken ct);
    Task<EquipmentUpgradeQuote> PreviewAsync(
        Guid characterId,
        EquipmentUpgradeRequest request,
        CancellationToken cancellationToken);

    Task<EquipmentUpgradeResult> ExecuteAsync(
        Guid characterId,
        Guid operationId,
        EquipmentUpgradeRequest request,
        string expectedQuote,
        CancellationToken cancellationToken);
}
