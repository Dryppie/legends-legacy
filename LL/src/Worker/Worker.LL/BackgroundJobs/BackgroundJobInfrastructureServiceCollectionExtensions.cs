using Application.BackgroundJobs;
using Quartz;

namespace Worker.LL.BackgroundJobs;

public static class BackgroundJobInfrastructureServiceCollectionExtensions
{
    private const string SchedulerName = "LegendsLegacy.Background";

    public static IServiceCollection AddBackgroundJobInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = GetBackgroundJobOptions(configuration);

        services.AddQuartz(q =>
        {
            q.SchedulerName = SchedulerName;
            q.SchedulerId = "AUTO";

            q.UseDefaultThreadPool(threadPool =>
            {
                threadPool.MaxConcurrency = options.MaxConcurrency;
            });

            q.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UseSystemTextJsonSerializer();

                store.UsePostgres(postgres =>
                {
                    var connectionString = configuration.GetConnectionString("LegendsLegacyDB");
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        throw new InvalidOperationException("Missing LegendsLegacyDB connection string for Quartz job store.");
                    }

                    postgres.ConnectionString = connectionString;
                });

                store.UseClustering(cluster =>
                {
                    cluster.CheckinInterval = TimeSpan.FromSeconds(20);
                    cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(60);
                });
            });

            q.RegisterBackgroundJobs(configuration, environment);
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }

    private static BackgroundJobOptions GetBackgroundJobOptions(IConfiguration configuration)
    {
        var options = configuration
            .GetSection(BackgroundJobOptions.SectionName)
            .Get<BackgroundJobOptions>() ?? new BackgroundJobOptions();

        if (options.MaxConcurrency <= 0)
        {
            throw new InvalidOperationException("BackgroundJobs:MaxConcurrency must be greater than zero.");
        }

        if (options.RunningExecutionTimeoutMinutes <= 0)
        {
            throw new InvalidOperationException("BackgroundJobs:RunningExecutionTimeoutMinutes must be greater than zero.");
        }

        return options;
    }
}
