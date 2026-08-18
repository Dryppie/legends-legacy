using System.Reflection;
using System.Diagnostics;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Queries.GetAdministrationAudit;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Persistence.LL;

namespace API.LiveOps.Health;

public interface ILiveOpsRecentActivityReader
{
    Task<Response<AdministrationAuditPageDto>> GetAsync(
        bool includeGame,
        CancellationToken cancellationToken);
}

public sealed class LiveOpsRecentActivityReader(MediatR.ISender sender)
    : ILiveOpsRecentActivityReader
{
    public Task<Response<AdministrationAuditPageDto>> GetAsync(
        bool includeGame,
        CancellationToken cancellationToken) =>
        sender.Send(new GetAdministrationAuditQuery(
            null,
            10,
            null,
            null,
            includeGame ? "All" : "Chat",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false), cancellationToken);
}

public sealed class LiveOpsOperationalStatusService(
    HealthCheckService healthChecks,
    IDbContextFactory<LLDbContext> contextFactory,
    ILiveOpsRecentActivityReader recentActivity,
    IWebHostEnvironment environment,
    IConfiguration configuration,
    TimeProvider timeProvider)
{
    private static readonly DateTimeOffset ProcessStartedAtUtc =
        new(Process.GetCurrentProcess().StartTime.ToUniversalTime());

    public async Task<OperationalStatusDto> GetAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var warnings = new List<string>();
        HealthReport? healthReport = null;
        try
        {
            healthReport = await healthChecks.CheckHealthAsync(
                registration => registration.Tags.Contains("ready"),
                cancellationToken);
        }
        catch (Exception)
        {
            warnings.Add("Dependency readiness could not be evaluated.");
        }

        var game = Dependency(
            healthReport,
            "game_database",
            "Game database",
            ["Player lookup", "Account moderation", "Compensation", "Game audit", "Outbox metrics"]);
        var chat = Dependency(
            healthReport,
            "chat_moderation",
            "Chat moderation",
            ["Chat mute and unmute", "Chat audit"]);

        var outbox = new OperationalOutboxStatus(false, "Unavailable", 0, 0, null);
        var restrictions = new OperationalRestrictionStatus(false, 0, null);
        var permanentActions = 0;
        var highValueActions = 0;
        if (game.Status == "Healthy")
        {
            try
            {
                await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
                var activeOutbox = database.GameEventOutboxDeliveries.AsNoTracking().Where(x =>
                    x.Status == GameEventOutboxDeliveryStatus.Pending ||
                    x.Status == GameEventOutboxDeliveryStatus.Processing);
                var pending = await activeOutbox.CountAsync(cancellationToken);
                var failed = await database.GameEventOutboxDeliveries.AsNoTracking()
                    .CountAsync(x => x.Status == GameEventOutboxDeliveryStatus.Failed, cancellationToken);
                var oldestPending = await activeOutbox
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => (DateTimeOffset?)x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                var delayed = oldestPending.HasValue && oldestPending.Value < now.AddMinutes(-5);
                outbox = new OperationalOutboxStatus(
                    true,
                    failed > 0 || delayed ? "Degraded" : "Healthy",
                    pending,
                    failed,
                    oldestPending);

                var expiring = database.AccountRestrictions.AsNoTracking().Where(x =>
                    x.RevokedAt == null &&
                    x.ExpiresAt > now &&
                    x.ExpiresAt <= now.AddDays(7));
                restrictions = new OperationalRestrictionStatus(
                    true,
                    await expiring.CountAsync(cancellationToken),
                    await expiring.OrderBy(x => x.ExpiresAt)
                        .Select(x => x.ExpiresAt)
                        .FirstOrDefaultAsync(cancellationToken));

                var lastDay = now.AddHours(-24);
                permanentActions = await database.AdminActions.AsNoTracking().CountAsync(
                    x => x.OccurredAt >= lastDay &&
                        x.RiskLevel == AdministrationRiskLevel.Permanent,
                    cancellationToken);
                highValueActions = await database.AdminActions.AsNoTracking().CountAsync(
                    x => x.OccurredAt >= lastDay &&
                        x.RiskLevel == AdministrationRiskLevel.HighValue,
                    cancellationToken);
            }
            catch (Exception)
            {
                warnings.Add("Game operational metrics are temporarily unavailable.");
            }
        }

        IReadOnlyList<AdministrationAuditEntryDto> recentActions = [];
        if (game.Status == "Healthy" || chat.Status == "Healthy")
        {
            try
            {
                var audit = await recentActivity.GetAsync(
                    game.Status == "Healthy",
                    cancellationToken);
                if (audit.IsSuccess && audit.Data is not null)
                {
                    recentActions = audit.Data.Entries;
                    foreach (var source in audit.Data.UnavailableSources)
                    {
                        warnings.Add($"{source} recent activity is temporarily unavailable.");
                    }
                }
                else
                {
                    warnings.Add("Recent privileged activity is temporarily unavailable.");
                }
            }
            catch (Exception)
            {
                warnings.Add("Recent privileged activity is temporarily unavailable.");
            }
        }

        var dependencies = new[] { game, chat };
        var overall = game.Status == "Unhealthy" || game.Status == "Unavailable"
            ? "Unhealthy"
            : dependencies.Any(x => x.Status != "Healthy") ||
                !outbox.IsAvailable ||
                outbox.Status != "Healthy" ||
                !restrictions.IsAvailable
                ? "Degraded"
                : "Healthy";

        return new OperationalStatusDto(
            overall,
            environment.EnvironmentName,
            now,
            BuildStatus(),
            dependencies,
            outbox,
            restrictions,
            permanentActions,
            highValueActions,
            recentActions,
            warnings.Distinct(StringComparer.Ordinal).ToArray());
    }

    private OperationalBuildStatus BuildStatus()
    {
        var assemblyVersion = typeof(LiveOpsOperationalStatusService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";
        var releaseVersion = Value("Version") ?? assemblyVersion;
        return new OperationalBuildStatus(
            releaseVersion,
            Value("FrontendVersion") ?? releaseVersion,
            Value("GameVersion") ?? "not reported",
            Value("ChatVersion") ?? "not reported",
            Value("CommitSha"),
            DateTimeOffset.TryParse(Value("DeployedAtUtc"), out var deployedAt)
                ? deployedAt.ToUniversalTime()
                : null,
            ProcessStartedAtUtc);
    }

    private string? Value(string key)
    {
        var value = configuration[$"LiveOps:Build:{key}"];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OperationalDependencyStatus Dependency(
        HealthReport? report,
        string key,
        string name,
        IReadOnlyList<string> affectedCapabilities)
    {
        if (report is null || !report.Entries.TryGetValue(key, out var entry))
        {
            return new OperationalDependencyStatus(
                key,
                name,
                "Unavailable",
                "Readiness was not reported.",
                affectedCapabilities);
        }

        var status = entry.Status.ToString();
        var message = string.IsNullOrWhiteSpace(entry.Description)
            ? status == "Healthy" ? "Ready." : "Readiness check did not pass."
            : entry.Description;
        return new OperationalDependencyStatus(key, name, status, message, affectedCapabilities);
    }
}
