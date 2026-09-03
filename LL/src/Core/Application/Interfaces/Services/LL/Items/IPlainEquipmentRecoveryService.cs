using Domain.Models.Items.Equipments.Progression;
namespace Application.Interfaces.Services.LL.Items;

public interface IPlainEquipmentRecoveryService
{
    Task<IReadOnlyList<PlainEquipmentRecoveryOption>> GetOptionsAsync(Guid characterId, CancellationToken ct);
    Task<PlainEquipmentRecoveryResult> RecoverAsync(Guid characterId, Guid operationId, string definitionId, int tier, CancellationToken ct);
}
