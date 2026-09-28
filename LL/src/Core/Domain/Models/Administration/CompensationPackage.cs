using Domain.Models.Items.Equipments.Progression;
namespace Domain.Models.Administration;

public sealed record CompensationPackageLine(string ItemBaseId, int Quantity, EquipmentGrantRequest? Equipment = null);
/// <summary>Immutable, server-owned package revision. No client-supplied names or quantities are trusted at grant time.</summary>
public sealed class CompensationPackageVersion
{
    public Guid Id { get; set; }
    public Guid PackageId { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public bool Archived { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public string RequestHash { get; set; } = "";
    public string ActorSubject { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed record CompensationPackageDefinition(Guid PackageId, int Version, string Name, string Purpose,
    bool Archived, IReadOnlyList<CompensationPackageLine> Items, DateTimeOffset CreatedAt);
public sealed record CompensationPackageEdit(Guid OperationId, Guid PackageId, int ExpectedVersion, Guid CharacterId,
    string Name, string Purpose, bool Archived, IReadOnlyList<CompensationPackageLine> Items);
public sealed record CompensationPackagePlan(CompensationPackageDefinition Package, PlayerAdministrationSnapshot Player,
    IReadOnlyList<CompensationGrantPlan> Items);
public sealed record CompensationPackageOperation(AdminAction Action, IReadOnlyList<ItemGrantOperation> Grants, bool WasAlreadyProcessed);
public sealed class CompensationPackageConflictException(string message) : Exception(message);
