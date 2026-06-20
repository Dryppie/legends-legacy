namespace Application.Interfaces.Services.LL.Essences;

public interface IAbilityCatalogDiagnostics
{
    AbilityCatalogDiagnosticReport RunTrainingEncounter();
}

public sealed record AbilityCatalogDiagnosticReport(
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
    IReadOnlyList<AbilityCatalogSummonDiagnostic> Summons,
    string Outcome,
    int Duration,
    int EventLogCount,
    bool DirectDamageObserved,
    bool BarrierObserved,
    bool DamageOverTimeObserved,
    bool ReflectObserved,
    IReadOnlyList<string> Failures);

public sealed record AbilityCatalogSummonDiagnostic(
    string Id,
    string Name,
    string ImagePath,
    int DurationTicks,
    int MaxActive,
    bool HasTimedDuration,
    bool ExpiresOnOwnerDeath,
    IReadOnlyList<string> AbilityIds,
    IReadOnlyList<string> Tags);
