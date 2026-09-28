using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
namespace Persistence.LL.Repositories.Administration;
public sealed class CompensationPackageRepository(LLDbContext db) : ICompensationPackageRepository
{
    public Task LockAsync(Guid packageId, CancellationToken ct) => db.AcquireStateSyncScopeLockAsync($"compensation-package:{packageId:D}", ct);
    public Task<CompensationPackageVersion?> FindOperationAsync(Guid operationId, CancellationToken ct) => db.Set<CompensationPackageVersion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == operationId, ct);
    public Task<CompensationPackageVersion?> LatestAsync(Guid packageId, CancellationToken ct) => db.Set<CompensationPackageVersion>().AsNoTracking().Where(x => x.PackageId == packageId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<CompensationPackageVersion>> ListAsync(CancellationToken ct) =>
        await db.Set<CompensationPackageVersion>().AsNoTracking().Where(x => !db.Set<CompensationPackageVersion>().Any(y => y.PackageId == x.PackageId && y.Version > x.Version))
            .OrderBy(x => x.Archived).ThenBy(x => x.Name).ThenBy(x => x.PackageId).Take(100).ToListAsync(ct);
    public void Add(CompensationPackageVersion version) => db.Set<CompensationPackageVersion>().Add(version);
}
