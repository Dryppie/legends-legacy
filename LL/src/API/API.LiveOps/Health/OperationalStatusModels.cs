using Application.UseCases.Administration.Dtos;

namespace API.LiveOps.Health;

public sealed record OperationalDependencyStatus(
    string Key,
    string Name,
    string Status,
    string Message,
    IReadOnlyList<string> AffectedCapabilities);

public sealed record OperationalBuildStatus(
    string ReleaseVersion,
    string FrontendVersion,
    string GameVersion,
    string ChatVersion,
    string? CommitSha,
    DateTimeOffset? DeployedAtUtc,
    DateTimeOffset ProcessStartedAtUtc);

public sealed record OperationalOutboxStatus(
    bool IsAvailable,
    string Status,
    int PendingDeliveries,
    int FailedDeliveries,
    DateTimeOffset? OldestPendingAtUtc);

public sealed record OperationalRestrictionStatus(
    bool IsAvailable,
    int ExpiringWithinSevenDays,
    DateTimeOffset? NextExpiryAtUtc);

public sealed record OperationalStatusDto(
    string OverallStatus,
    string Environment,
    DateTimeOffset ServerTimeUtc,
    OperationalBuildStatus Build,
    IReadOnlyList<OperationalDependencyStatus> Dependencies,
    OperationalOutboxStatus Outbox,
    OperationalRestrictionStatus Restrictions,
    int PermanentActionsLast24Hours,
    int HighValueActionsLast24Hours,
    IReadOnlyList<AdministrationAuditEntryDto> RecentActions,
    IReadOnlyList<string> Warnings);
