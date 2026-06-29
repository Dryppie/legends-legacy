using Application;
using Application.BackgroundJobs;
using Application.Interfaces.WebSockets;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.LL;
using Quartz;
using Services.AdminDashboard;
using Services.LL;
using Worker.LL.BackgroundJobs;
using Worker.LL.Realtime;

namespace EssenceSystem.Tests;

public sealed class WorkerServiceProviderTests
{
    [Fact]
    public void WorkerServiceProvider_CanResolveBackgroundJobInfrastructure()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "Worker.LL.Tests",
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = Environments.Development
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LegendsLegacyDB"] = "Host=localhost;Port=5432;Database=legends_legacy_test;Username=postgres",
            ["Database:TimeoutInSeconds"] = "30",
            ["BackgroundJobs:MaxConcurrency"] = "2",
            ["BackgroundJobs:RunningExecutionTimeoutMinutes"] = "30",
            ["BackgroundJobs:SmokeJob:Enabled"] = "false",
            ["Content:Root"] = "Data",
            ["Jwt:SigningKey"] = "TestSigningKeyTestSigningKeyTestSigningKeyTestSigningKey",
            ["Google:ClientId"] = "test-client-id"
        });

        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddRepositories();
        builder.Services.AddApplication();
        builder.Services.AddCommonServices();
        builder.Services.AddScoped<IGameEventPublisher, NoOpGameEventPublisher>();
        builder.Services.AddScoped<IGameRealtimeBroadcaster, NoOpGameRealtimeBroadcaster>();
        builder.Services.AddServices(builder.Configuration, builder.Environment.ContentRootPath);
        builder.Services.AddAdminDashboardServices();
        builder.Services.AddBackgroundJobInfrastructure(builder.Configuration, builder.Environment);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBackgroundJobExecutionService>());
        Assert.NotNull(host.Services.GetRequiredService<ISchedulerFactory>());
        Assert.Contains(
            host.Services.GetServices<IHostedService>(),
            hostedService => hostedService.GetType().FullName?.Contains("Quartz", StringComparison.OrdinalIgnoreCase) == true);
    }
}
