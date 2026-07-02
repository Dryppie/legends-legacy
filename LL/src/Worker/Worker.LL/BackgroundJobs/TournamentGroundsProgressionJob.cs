using Application.BackgroundJobs;
using Application.Interfaces.Services.LL.Colosseum;
using Microsoft.Extensions.Options;
using Quartz;
using Services.LL.Colosseum.Tournaments;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class TournamentGroundsProgressionJob : IJob
{
    private readonly ITournamentGroundsService _tournamentGrounds;
    private readonly IBackgroundJobExecutionService _executionService;
    private readonly IOptions<TournamentGroundsOptions> _options;
    private readonly ILogger<TournamentGroundsProgressionJob> _logger;

    public TournamentGroundsProgressionJob(
        ITournamentGroundsService tournamentGrounds,
        IBackgroundJobExecutionService executionService,
        IOptions<TournamentGroundsOptions> options,
        ILogger<TournamentGroundsProgressionJob> logger)
    {
        _tournamentGrounds = tournamentGrounds;
        _executionService = executionService;
        _options = options;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogDebug("Skipping Tournament Grounds progression because the feature is disabled.");
            return;
        }

        try
        {
            var scheduled = context.ScheduledFireTimeUtc ?? context.FireTimeUtc;
            var businessKey = $"tournament-grounds-progression:{scheduled:yyyyMMddHHmmss}";

            await _executionService.RunOnceAsync(
                BackgroundJobNames.TournamentGroundsRollover,
                businessKey,
                async cancellationToken =>
                {
                    _logger.LogInformation(
                        "Running Tournament Grounds progression. JobKey: {JobKey}, TriggerKey: {TriggerKey}, FireInstanceId: {FireInstanceId}, Recovering: {Recovering}",
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        context.FireInstanceId,
                        context.Recovering);

                    await _tournamentGrounds.EnsureUpcomingTournamentsAsync(cancellationToken);
                    await _tournamentGrounds.AdvanceDueTournamentsAsync(cancellationToken);
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
