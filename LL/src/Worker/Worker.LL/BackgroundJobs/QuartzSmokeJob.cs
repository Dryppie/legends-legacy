using Application.BackgroundJobs;
using Quartz;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class QuartzSmokeJob : IJob
{
    private readonly IBackgroundJobExecutionService _executionService;
    private readonly ILogger<QuartzSmokeJob> _logger;

    public QuartzSmokeJob(
        IBackgroundJobExecutionService executionService,
        ILogger<QuartzSmokeJob> logger)
    {
        _executionService = executionService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var scheduled = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
            var businessKey = $"smoke:{scheduled:yyyyMMddHHmm}";

            await _executionService.RunOnceAsync(
                BackgroundJobNames.QuartzSmoke,
                businessKey,
                cancellationToken =>
                {
                    _logger.LogInformation(
                        "Quartz smoke job executed. JobKey: {JobKey}, TriggerKey: {TriggerKey}, FireInstanceId: {FireInstanceId}, Recovering: {Recovering}",
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        context.FireInstanceId,
                        context.Recovering);

                    return Task.CompletedTask;
                },
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job {JobKey} failed.", context.JobDetail.Key);
            throw;
        }
    }
}
