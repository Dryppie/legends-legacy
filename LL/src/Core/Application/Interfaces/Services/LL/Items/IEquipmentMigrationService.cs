using Domain.Models.Items.Equipments.Progression;

namespace Application.Interfaces.Services.LL.Items;

public interface IEquipmentMigrationService
{
    Task<EquipmentMigrationChoice?> GetChoiceAsync(Guid characterId, Guid itemId, CancellationToken ct);
    Task<EquipmentMigrationPreview> PreviewAsync(EquipmentMigrationTarget target, string? definitionId, CancellationToken ct, int? targetBalanceVersion = null);
    Task<EquipmentMigrationReceipt> ApplyAsync(Guid operationId, EquipmentMigrationTarget target, string sourceHash, string definitionId, string actorId, CancellationToken ct, int? targetBalanceVersion = null, string? expectedResultHash = null);
    Task<EquipmentMigrationReceipt> RollbackAsync(Guid operationId, string actorId, CancellationToken ct);
    Task<EquipmentData> ChooseAsync(Guid characterId, Guid migrationId, Guid choiceOperationId, string definitionId, CancellationToken ct);
}
