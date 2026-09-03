using Domain.Models.Items.Equipments.Progression;

namespace Application.Interfaces.Services.LL.Items;
public interface ICombatAcquisitionService
{
    Task<IReadOnlyList<CombatAcquisitionView>> GetAsync(Guid characterId, CancellationToken ct);
    Task<CombatAcquisitionSelectionResult> SelectAsync(Guid characterId, Guid operationId, string poolId, string? definitionId, string? sigilFamilyId, CancellationToken ct);
}
