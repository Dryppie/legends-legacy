namespace Domain.Models.Analytics;

public sealed class AccountActivityDay
{
    public Guid AccountId { get; set; }
    public DateOnly ActivityDateUtc { get; set; }
    public DateTimeOffset FirstSeenAtUtc { get; set; }
}

// DungeonRun is removed after a claim or dismissal. This small fact survives that cleanup.
public sealed class DungeonAttemptHistory
{
    public Guid RunId { get; set; }
    public Guid CharacterId { get; set; }
    public string DungeonDefinitionId { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public string Outcome { get; set; } = "Active";
}

public sealed class DailyTelemetryReport
{
    public DateOnly ReportDateUtc { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public sealed record PopulationMetrics(int Dau, int Wau, int Mau, int NewActive, int ReturningActive,
    int D1Cohort, int D1Returned, int D7Cohort, int D7Returned);
public sealed record ContentOutcomeMetric(string Kind, string Key, int Started, int Completed,
    int Failed, int UniqueCharacters);
public sealed record AdoptionMetric(int CohortDays, string Kind, string Key, string LevelBand,
    int CohortCharacters, int ObservedCharacters);
public sealed record EconomyMetric(int CohortDays, string Resource, string LevelBand,
    int CharacterCount, int ZeroCount, long P50Balance, long P90Balance);
public sealed record TelemetrySnapshot(DateOnly ReportDateUtc, DateTimeOffset GeneratedAtUtc,
    DateTimeOffset SnapshotAtUtc,
    PopulationMetrics Population, IReadOnlyList<ContentOutcomeMetric> Outcomes,
    IReadOnlyList<AdoptionMetric> Adoption, IReadOnlyList<EconomyMetric> Economy);

public interface ITelemetryRepository
{
    Task RecordActivityAsync(Guid accountId, DateTimeOffset seenAtUtc, CancellationToken ct);
    Task GenerateDailyReportsAsync(DateOnly yesterdayUtc, CancellationToken ct);
    Task<IReadOnlyList<TelemetrySnapshot>> GetReportsAsync(int days, CancellationToken ct);
}
