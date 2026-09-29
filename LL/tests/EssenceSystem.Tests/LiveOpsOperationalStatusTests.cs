using API.LiveOps.Health;
using Application.UseCases.Administration.Dtos;
using Common.Primitives;
using Domain.Models.Administration;
using Domain.Models.Outbox;
using Domain.Models.BackgroundJobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class LiveOpsOperationalStatusTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 18, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Detail_queues_bound_results_and_use_server_time_for_actionable_exceptions()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var db = new LLDbContext(options))
        {
            for (var i = 0; i < 55; i++)
            {
                var message = new GameEventOutboxMessage { Id = Guid.NewGuid(), PayloadJson = "{\"private\":true}", CreatedAt = Now.AddMinutes(-i) };
                db.GameEventOutboxDeliveries.Add(new GameEventOutboxDelivery { Id = Guid.NewGuid(), MessageId = message.Id, Message = message,
                    Consumer = "Inventory", Status = GameEventOutboxDeliveryStatus.Failed, CreatedAt = message.CreatedAt, Attempts = 2 });
            }
            foreach (var days in new[] { -1, 1, 8 })
                db.AccountRestrictions.Add(new AccountRestriction { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), Reason = "Fixture",
                    CreatedBySubject = "staff", CreatedAt = Now.AddDays(-2), ExpiresAt = Now.AddDays(days) });
            foreach (var (status, hours) in new[] { (BackgroundJobExecutionStatus.Failed, 2), (BackgroundJobExecutionStatus.Failed, 200),
                (BackgroundJobExecutionStatus.Running, 2), (BackgroundJobExecutionStatus.Running, 0) })
                db.Set<BackgroundJobExecution>().Add(new BackgroundJobExecution { Id = Guid.NewGuid(), JobName = "Daily reports", BusinessKey = Guid.NewGuid().ToString(),
                    Status = status, StartedAt = Now.AddHours(-hours), CreatedAt = Now.AddHours(-hours), UpdatedAt = Now.AddHours(-hours) });
            await db.SaveChangesAsync();
        }
        var services = new ServiceCollection(); services.AddLogging(); services.AddHealthChecks();
        await using var provider = services.BuildServiceProvider();
        var service = new LiveOpsOperationalStatusService(provider.GetRequiredService<HealthCheckService>(), new TestContextFactory(options),
            new TestRecentActivityReader(null!), new TestEnvironment(), new ConfigurationBuilder().Build(), new FixedTimeProvider(Now));
        var deliveries = (await service.GetDetailsAsync("deliveries", default))!;
        Assert.Equal(55, deliveries.Total); Assert.Equal(50, deliveries.Rows.Count); Assert.Equal(Now, deliveries.AsOf);
        var next = (await service.GetDetailsAsync("deliveries", default, 2))!;
        Assert.Equal(5, next.Rows.Count); Assert.Equal(2, next.Page);
        Assert.Equal(55, deliveries.Rows.Concat(next.Rows).Select(x => x.Id).Distinct().Count());
        Assert.Empty((await service.GetDetailsAsync("deliveries", default, 1, "Processing"))!.Rows);
        Assert.Equal(1, (await service.GetDetailsAsync("restrictions", default))!.Total);
        Assert.Equal(2, (await service.GetDetailsAsync("jobs", default))!.Total);
        Assert.Null(await service.GetDetailsAsync("unrecognized", default));
    }

    [Fact]
    public async Task Status_summarizes_dependencies_backlog_risk_and_expiring_restrictions()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var database = new LLDbContext(options))
        {
            database.GameEventOutboxDeliveries.AddRange(
                new GameEventOutboxDelivery
                {
                    Id = Guid.NewGuid(),
                    MessageId = Guid.NewGuid(),
                    Consumer = "test",
                    Status = GameEventOutboxDeliveryStatus.Pending,
                    CreatedAt = Now.AddMinutes(-10)
                },
                new GameEventOutboxDelivery
                {
                    Id = Guid.NewGuid(),
                    MessageId = Guid.NewGuid(),
                    Consumer = "test",
                    Status = GameEventOutboxDeliveryStatus.Failed,
                    CreatedAt = Now.AddMinutes(-2)
                });
            database.AccountRestrictions.Add(new AccountRestriction
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Reason = "Temporary moderation",
                CreatedBySubject = "operator",
                CreatedAt = Now.AddDays(-1),
                ExpiresAt = Now.AddDays(1)
            });
            database.AdminActions.AddRange(
                Action(AdministrationRiskLevel.Permanent),
                Action(AdministrationRiskLevel.HighValue));
            await database.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("game_database", () => HealthCheckResult.Healthy(), tags: ["ready"])
            .AddCheck("chat_moderation", () => HealthCheckResult.Degraded("Chat is unavailable."), tags: ["ready"]);
        await using var provider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LiveOps:Build:Version"] = "1.2.3",
                ["LiveOps:Build:CommitSha"] = "abc123"
            })
            .Build();
        var recent = new AdministrationAuditEntryDto(
            Guid.NewGuid(),
            "Game",
            "AccountBanned",
            "liveops.accounts.moderate",
            "operator",
            "Operator",
            null,
            null,
            null,
            "Case LL-42",
            null,
            "{}",
            "Permanent",
            "Completed",
            Now);
        var service = new LiveOpsOperationalStatusService(
            provider.GetRequiredService<HealthCheckService>(),
            new TestContextFactory(options),
            new TestRecentActivityReader(recent),
            new TestEnvironment(),
            configuration,
            new FixedTimeProvider(Now));

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal("Degraded", result.OverallStatus);
        Assert.Equal("1.2.3", result.Build.ReleaseVersion);
        Assert.Equal("abc123", result.Build.CommitSha);
        Assert.Equal(1, result.Outbox.PendingDeliveries);
        Assert.Equal(1, result.Outbox.FailedDeliveries);
        Assert.Equal("Degraded", result.Outbox.Status);
        Assert.Equal(1, result.Restrictions.ExpiringWithinSevenDays);
        Assert.Equal(1, result.PermanentActionsLast24Hours);
        Assert.Equal(1, result.HighValueActionsLast24Hours);
        Assert.Equal(recent.OperationId, Assert.Single(result.RecentActions).OperationId);
        Assert.Contains(result.Dependencies, x =>
            x.Key == "chat_moderation" && x.Status == "Degraded");

        await using (var database = new LLDbContext(options))
        {
            var failed = await database.GameEventOutboxDeliveries.SingleAsync(x => x.Status == GameEventOutboxDeliveryStatus.Failed);
            var repair = Action(AdministrationRiskLevel.Normal);
            repair.ActionType = AdminActionType.StateRefreshDeliveryRetried; repair.TargetResourceId = failed.Id;
            database.AdminActions.Add(repair); await database.SaveChangesAsync();
        }
        var afterRepair = await service.GetAsync(default);
        Assert.Equal(0, afterRepair.Outbox.FailedDeliveries);
        Assert.Equal(1, afterRepair.Outbox.PendingDeliveries);
        Assert.Equal("Degraded", afterRepair.Outbox.Status); // The delayed replacement still needs attention.
    }

    private static AdminAction Action(AdministrationRiskLevel riskLevel) => new()
    {
        Id = Guid.NewGuid(),
        ActionType = riskLevel == AdministrationRiskLevel.Permanent
            ? AdminActionType.AccountBanned
            : AdminActionType.CompensationItemsGranted,
        Permission = "liveops.test",
        ActorSubject = "operator",
        ActorDisplayName = "Operator",
        Reason = "Test action",
        RiskLevel = riskLevel,
        OccurredAt = Now.AddHours(-1)
    };

    private sealed class TestContextFactory(DbContextOptions<LLDbContext> options)
        : IDbContextFactory<LLDbContext>
    {
        public LLDbContext CreateDbContext() => new(options);
    }

    private sealed class TestRecentActivityReader(AdministrationAuditEntryDto entry)
        : ILiveOpsRecentActivityReader
    {
        public Task<Response<AdministrationAuditPageDto>> GetAsync(
            bool includeGame,
            CancellationToken cancellationToken) =>
            Task.FromResult(Response<AdministrationAuditPageDto>.Success(
                new AdministrationAuditPageDto([entry], null, [])));
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "API.LiveOps";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
