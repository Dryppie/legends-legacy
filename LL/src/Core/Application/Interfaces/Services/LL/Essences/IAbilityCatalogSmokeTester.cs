namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogSmokeTester
{
    AbilityCatalogSmokeTestReport Run();
}

public sealed record AbilityCatalogSmokeTestReport(
    int EssenceDefinitionsChecked,
    int AuthoredAbilitiesChecked,
    int AbilityScenariosChecked,
    int RuntimeAbilitiesCompiled,
    int CombatSimulationsRun,
    IReadOnlyList<AbilityCatalogSmokeTestFailure> Failures)
{
    public bool Passed => Failures.Count == 0;
}

public sealed record AbilityCatalogSmokeTestFailure(
    string EssenceDefinitionId,
    string AbilityId,
    string AbilityRole,
    string Scenario,
    string Message);
