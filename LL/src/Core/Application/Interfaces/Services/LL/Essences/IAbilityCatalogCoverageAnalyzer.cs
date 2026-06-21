namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogCoverageAnalyzer
{
    AbilityCatalogCoverageReport Analyze();
}

public sealed record AbilityCatalogCoverageReport(
    int EssenceCount,
    int RequiredSlotCount,
    int CoveredSlotCount,
    int CurrentReferenceCoveredSlotCount,
    IReadOnlyList<AbilityCatalogSlotCoverage> Slots,
    IReadOnlyList<AbilityCatalogCoverageGap> Gaps,
    IReadOnlyList<string> UnownedAbilityIds,
    IReadOnlyList<AbilityCatalogRuntimeLoadoutCheck> RuntimeLoadoutChecks)
{
    public bool IsComplete => Gaps.Count == 0 && RuntimeLoadoutChecks.All(x => x.IsReady);
}

public sealed record AbilityCatalogSlotCoverage(
    string EssenceId,
    string Slot,
    string ReferencedAbilityId,
    string? AbilityId,
    bool HasOwnedAbility,
    bool CurrentReferenceExists,
    bool KindMatches);

public sealed record AbilityCatalogCoverageGap(
    string EssenceId,
    string Slot,
    string ReferencedAbilityId,
    string Reason);

public sealed record AbilityCatalogRuntimeLoadoutCheck(
    string EssenceId,
    IReadOnlyList<string> AbilityIds,
    bool IsReady,
    string? Outcome,
    int Duration,
    int EventCount,
    string? Failure);
