namespace Domain.Models.Administration;
public interface ICompensationPackageRepository
{
    Task LockAsync(Guid packageId, CancellationToken ct);
    Task<CompensationPackageVersion?> FindOperationAsync(Guid operationId, CancellationToken ct);
    Task<CompensationPackageVersion?> LatestAsync(Guid packageId, CancellationToken ct);
    Task<IReadOnlyList<CompensationPackageVersion>> ListAsync(CancellationToken ct);
    void Add(CompensationPackageVersion version);
}
