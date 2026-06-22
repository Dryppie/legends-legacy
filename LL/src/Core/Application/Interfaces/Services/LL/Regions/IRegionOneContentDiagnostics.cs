namespace Application.Interfaces.Services.LL.Regions;

public interface IRegionOneContentDiagnostics
{
    Task<RegionOneContentDiagnosticReport> AnalyzeAsync(CancellationToken cancellationToken);
}

public sealed record RegionOneContentDiagnosticReport(
    int ManifestEntryCount,
    int CompleteEntryCount,
    int MissingEntryCount,
    int AwaitingManaCount,
    int StaleAreaCount,
    bool IsComplete,
    IReadOnlyList<RegionOneContentEntryDiagnostic> Entries,
    IReadOnlyList<string> Warnings);

public sealed record RegionOneContentEntryDiagnostic(
    string Name,
    string CreatureKey,
    string SourceType,
    string SourceName,
    string ExpectedTier,
    string? EssenceId,
    string? ActiveAbilityId,
    string? PassiveAbilityId,
    bool RequiresMana,
    bool CreatureResolved,
    bool EssenceResolved,
    bool ActiveAbilityResolved,
    bool PassiveAbilityResolved,
    bool EssenceItemResolved,
    bool SourcePlacementResolved,
    bool BehaviorCovered,
    bool IsComplete,
    IReadOnlyList<string> Missing);
