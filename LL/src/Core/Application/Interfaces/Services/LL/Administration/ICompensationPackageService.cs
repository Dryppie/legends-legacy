using Common.Primitives;
using Domain.Models.Administration;
namespace Application.Interfaces.Services.LL.Administration;
public interface ICompensationPackageService
{
    Task<IReadOnlyList<CompensationPackageDefinition>> ListAsync(CancellationToken ct);
    Task<Response<CompensationPackageDefinition>> SaveAsync(CompensationPackageEdit edit, AdministrationActor actor, CancellationToken ct);
    Task<Response<CompensationPackagePlan>> PrepareAsync(Guid operationId, Guid characterId, Guid packageId, int version, CancellationToken ct);
    Task<Response<CompensationPackageOperation>> GrantAsync(Guid operationId, Guid characterId, Guid packageId, int version,
        AdministrationActor actor, string reason, string? notes, CancellationToken ct);
}
