namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogV2Diagnostics
{
    AbilityCatalogV2DiagnosticReport RunTrainingEncounter();
}

public sealed record AbilityCatalogV2DiagnosticReport(
    int AbilityCount,
    int StatusCount,
    int SummonCount,
    int IndexedAbilityTags,
    int IndexedStatusTags,
    int IndexedSummonTags,
    int IndexedTriggerEvents,
    int TimedSummonCount,
    int PersistentSummonCount,
    int SummonAbilityReferenceCount,
    IReadOnlyList<AbilityCatalogV2SummonDiagnostic> Summons,
    string Outcome,
    int Duration,
    int EventLogCount,
    bool DirectDamageObserved,
    bool BarrierObserved,
    bool DamageOverTimeObserved,
    bool ReflectObserved,
    IReadOnlyList<string> Failures);

public sealed record AbilityCatalogV2SummonDiagnostic(
    string Id,
    string Name,
    string ImagePath,
    int DurationTicks,
    int MaxActive,
    bool HasTimedDuration,
    bool ExpiresOnOwnerDeath,
    IReadOnlyList<string> AbilityIds,
    IReadOnlyList<string> Tags);
