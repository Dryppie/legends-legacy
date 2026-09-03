using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items.Equipments.Progression;

namespace Application.Interfaces.Services.LL.Items;

public interface IEquipmentAcquisitionEligibility
{
    Task<string?> GetErrorAsync(Guid characterId, string dungeonId, CancellationToken ct);
}
public interface IEquipmentAcquisitionService
{
    Task<IReadOnlyList<EquipmentProtectionPoolView>> GetPoolsAsync(Guid characterId, CancellationToken ct);
    Task<EquipmentProgressionTargetSelectionResult> SelectAsync(Guid characterId, string poolId, string? definitionId, CancellationToken ct);
    Task FreezeAsync(DungeonRun run, DungeonDefinition dungeon, CancellationToken ct);
    Task CompleteAsync(DungeonRun run, bool firstCompletion, CancellationToken ct);
    Task MarkClaimedAsync(DungeonRun run, CancellationToken ct);
    Task<IReadOnlyList<BaselineEquipmentRecoveryOption>> GetRecoveryOptionsAsync(Guid characterId, CancellationToken ct);
    Task<BaselineEquipmentRecoveryResult> RecoverAsync(Guid characterId, Guid operationId, StarterEquipmentGrantKind kind, CancellationToken ct);
}
