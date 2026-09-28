using Domain.Models.Administration;
namespace Application.UseCases.Administration.Dtos;
public sealed record CompensationPackageDto(Guid PackageId, int Version, string Name, string Purpose,
    bool Archived, IReadOnlyList<CompensationPackageLine> Items, DateTimeOffset CreatedAt);
