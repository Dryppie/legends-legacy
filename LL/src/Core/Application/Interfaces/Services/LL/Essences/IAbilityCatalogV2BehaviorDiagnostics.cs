namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogV2BehaviorDiagnostics
{
    AbilityCatalogV2BehaviorDiagnosticReport Analyze();
}

public sealed record AbilityCatalogV2BehaviorDiagnosticReport(
    int ScenarioCount,
    int PassedCount,
    int FailedCount,
    IReadOnlyList<AbilityCatalogV2BehaviorScenarioResult> Scenarios,
    int AbilityCount,
    int CoveredAbilityCount,
    IReadOnlyList<string> MissingAbilityIds)
{
    public bool IsComplete => FailedCount == 0;
    public bool HasFullAbilityCoverage => MissingAbilityIds.Count == 0;
}

public sealed record AbilityCatalogV2BehaviorScenarioResult(
    string BehaviorId,
    string AbilityId,
    bool Passed,
    string? Outcome,
    int Duration,
    int EventCount,
    IReadOnlyList<string> Failures);
