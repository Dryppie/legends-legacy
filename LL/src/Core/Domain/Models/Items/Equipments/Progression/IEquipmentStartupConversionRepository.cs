namespace Domain.Models.Items.Equipments.Progression;

public interface IEquipmentStartupConversionRepository
{
    Task<IAsyncDisposable> AcquireRunnerLockAsync(CancellationToken ct);
    Task<IReadOnlyList<EquipmentMigrationTarget>> GetTargetsAsync(int targetVersion, int limit, CancellationToken ct);
    Task LockCharactersAsync(EquipmentMigrationTarget target, CancellationToken ct);
    Task<bool> IsCandidateAsync(EquipmentMigrationTarget target, int targetVersion, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetArenaDefensesAsync(int targetVersion, int limit, CancellationToken ct);
    Task<EquipmentStartupConversionAudit> AuditAsync(int targetVersion, CancellationToken ct);
}

public sealed record EquipmentStartupConversionAudit(int RemainingItems, int UnsupportedPendingRewards,
    int OutdatedArenaDefenses, int ActiveLegacyTournamentSnapshots, int RetainedUnreferencedItems)
{
    public bool Complete => RemainingItems == 0 && UnsupportedPendingRewards == 0
        && OutdatedArenaDefenses == 0 && ActiveLegacyTournamentSnapshots == 0;
}
