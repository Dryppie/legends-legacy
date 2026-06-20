namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogV2CoverageAnalyzer
{
    AbilityCatalogV2CoverageReport Analyze();
}

public sealed record AbilityCatalogV2CoverageReport(
    int EssenceCount,
    int RequiredSlotCount,
    int CoveredSlotCount,
    int CurrentReferenceCoveredSlotCount,
    IReadOnlyList<AbilityCatalogV2SlotCoverage> Slots,
    IReadOnlyList<AbilityCatalogV2CoverageGap> Gaps,
    IReadOnlyList<string> UnownedAbilityIds,
    IReadOnlyList<AbilityCatalogV2RuntimeLoadoutCheck> RuntimeLoadoutChecks)
{
    public bool IsComplete => Gaps.Count == 0 && RuntimeLoadoutChecks.All(x => x.IsReady);
}

public sealed record AbilityCatalogV2SlotCoverage(
    string EssenceId,
    string Slot,
    string LegacyAbilityId,
    string? V2AbilityId,
    bool HasOwnedV2Ability,
    bool CurrentReferenceExistsInV2,
    bool KindMatches);

public sealed record AbilityCatalogV2CoverageGap(
    string EssenceId,
    string Slot,
    string LegacyAbilityId,
    string Reason);

public sealed record AbilityCatalogV2RuntimeLoadoutCheck(
    string EssenceId,
    IReadOnlyList<string> AbilityIds,
    bool IsReady,
    string? Outcome,
    int Duration,
    int EventCount,
    string? Failure);
