namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogBehaviorDiagnostics
{
    AbilityCatalogBehaviorDiagnosticReport Analyze();
}

public sealed record AbilityCatalogBehaviorDiagnosticReport(
    int ScenarioCount,
    int PassedCount,
    int FailedCount,
    IReadOnlyList<AbilityCatalogBehaviorScenarioResult> Scenarios,
    int AbilityCount,
    int CoveredAbilityCount,
    IReadOnlyList<string> MissingAbilityIds)
{
    public bool IsComplete => FailedCount == 0;
    public bool HasFullAbilityCoverage => MissingAbilityIds.Count == 0;
}

public sealed record AbilityCatalogBehaviorScenarioResult(
    string BehaviorId,
    string AbilityId,
    bool Passed,
    string? Outcome,
    int Duration,
    int EventCount,
    IReadOnlyList<string> Failures);
