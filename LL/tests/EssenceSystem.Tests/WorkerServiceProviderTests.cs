using Application;
using Application.BackgroundJobs;
using Application.Interfaces.WebSockets;
using Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.LL;
using Quartz;
using Quartz.Impl.Triggers;
using Services.AdminDashboard;
using Services.LL;
using Worker.LL.BackgroundJobs;

namespace EssenceSystem.Tests;

public sealed class WorkerServiceProviderTests
{
    [Fact]
    public void WorkerServiceProvider_CanResolveBackgroundJobInfrastructure()
    {
        var builder = CreateWorkerBuilder("Host=localhost;Port=5432;Database=legends_legacy_test;Username=postgres");

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBackgroundJobExecutionService>());
        Assert.NotNull(host.Services.GetRequiredService<ISchedulerFactory>());
        Assert.Contains(
            host.Services.GetServices<IHostedService>(),
            hostedService => hostedService.GetType().FullName?.Contains("Quartz", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task WorkerQuartzScheduler_PersistsTournamentGroundsJobAndTrigger_WhenPostgresConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_TOURNAMENT_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var schemaName = $"ll_worker_quartz_tests_{Guid.NewGuid():N}";
        await using var adminDb = CreatePostgresDbContext(connectionString);
        var createSchemaSql = $"CREATE SCHEMA \"{schemaName}\"";
        var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
        await adminDb.Database.ExecuteSqlRawAsync(createSchemaSql);

        try
        {
            var isolatedConnectionString = WithSearchPath(connectionString, schemaName);
            await using (var setupDb = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                await setupDb.Database.MigrateAsync();
                var quartzSchema = await File.ReadAllTextAsync(
                    Path.GetFullPath(Path.Combine("LL", "database", "quartz", "tables_postgres.sql")));
                await setupDb.Database.ExecuteSqlRawAsync(quartzSchema);
            }

            var builder = CreateWorkerBuilder(isolatedConnectionString, tournamentGroundsEnabled: true);
            using var host = builder.Build();
            await host.StartAsync();

            try
            {
                var scheduler = await host.Services.GetRequiredService<ISchedulerFactory>().GetScheduler();
                var jobKey = new JobKey(BackgroundJobNames.TournamentGroundsRollover, BackgroundJobGroups.PvP);
                var triggerKey = new TriggerKey("pvp.tournament-grounds-progression.trigger", BackgroundJobGroups.PvP);

                var job = await scheduler.GetJobDetail(jobKey);
                Assert.NotNull(job);
                Assert.Equal(typeof(TournamentGroundsProgressionJob), job.JobType);
                Assert.True(job.Durable);
                Assert.True(job.RequestsRecovery);

                var trigger = Assert.Single(
                    await scheduler.GetTriggersOfJob(jobKey),
                    candidate => candidate.Key.Equals(triggerKey));
                var simpleTrigger = Assert.IsAssignableFrom<ISimpleTrigger>(trigger);
                Assert.Equal(TimeSpan.FromSeconds(10), simpleTrigger.RepeatInterval);
                Assert.Equal(SimpleTriggerImpl.RepeatIndefinitely, simpleTrigger.RepeatCount);
            }
            finally
            {
                await host.StopAsync();
            }
        }
        finally
        {
            await adminDb.Database.ExecuteSqlRawAsync(dropSchemaSql);
        }
    }

    private static HostApplicationBuilder CreateWorkerBuilder(
        string connectionString,
        bool tournamentGroundsEnabled = false)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "Worker.LL.Tests",
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = Environments.Development
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LegendsLegacyDB"] = connectionString,
            ["Database:TimeoutInSeconds"] = "30",
            ["BackgroundJobs:MaxConcurrency"] = "2",
            ["BackgroundJobs:RunningExecutionTimeoutMinutes"] = "30",
            ["BackgroundJobs:SmokeJob:Enabled"] = "false",
            ["Colosseum:TournamentGrounds:Enabled"] = tournamentGroundsEnabled.ToString(),
            ["Colosseum:TournamentGrounds:ProgressionIntervalSeconds"] = "10",
            ["Colosseum:TournamentGrounds:DefaultRoundIntervalMinutes"] = "0",
            ["Content:Root"] = "Data",
            ["Jwt:SigningKey"] = "TestSigningKeyTestSigningKeyTestSigningKeyTestSigningKey",
            ["Google:ClientId"] = "test-client-id"
        });

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddRepositories();
        builder.Services.AddApplication();
        builder.Services.AddCommonServices();
        builder.Services.AddServices(builder.Configuration, builder.Environment.ContentRootPath);
        builder.Services.AddAdminDashboardServices();
        builder.Services.AddBackgroundJobInfrastructure(builder.Configuration, builder.Environment);

        return builder;
    }

    private static LLDbContext CreatePostgresDbContext(string connectionString, string? migrationsSchema = null)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql(connectionString, postgres =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsSchema))
                {
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema);
                }
            })
            .Options;

        return new LLDbContext(options);
    }

    private static string WithSearchPath(string connectionString, string schemaName)
        => $"{connectionString.Trim().TrimEnd(';')};Search Path={schemaName}";
}
