namespace Application.Interfaces.Services.LL.CombatProfiles;

public interface ICombatCharacterProfileCatalogService
{
    Task<CombatCharacterProfileCatalogValidationReport> GetApprovedAsync(
        CancellationToken cancellationToken);

    Task<CombatCharacterProfileCatalogValidationReport> ValidateAsync(
        CombatCharacterProfileCatalogDocument catalog,
        CancellationToken cancellationToken);
}

public sealed record CombatCharacterProfileCatalogDocument(
    int SchemaVersion,
    int CatalogVersion,
    IReadOnlyList<CombatCharacterProfileGenerationReport> ProfileSets);

public sealed record CombatCharacterProfileCatalogValidationReport(
    bool IsValid,
    string CurrentContentHash,
    CombatCharacterProfileCatalogDocument NormalizedCatalog,
    IReadOnlyList<CombatCharacterProfileCatalogValidationIssue> Issues);

public sealed record CombatCharacterProfileCatalogValidationIssue(
    string Severity,
    string Code,
    string Path,
    string Message);
