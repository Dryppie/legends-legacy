using Application.BackgroundJobs;
using Application.UseCases.Nobility.Commands.SettleNobilityDailyRewards;
using Domain.Models.Nobility;
using MediatR;
using Quartz;

namespace Worker.LL.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class NobilityDailyRewardsJob(IBackgroundJobExecutionService execution, IServiceScopeFactory scopes,
    TimeProvider time) : IJob
{
    public Task Execute(IJobExecutionContext context) => execution.RunOnceAsync(
        "economy.nobility-daily-rewards", $"nobility:{context.ScheduledFireTimeUtc ?? context.FireTimeUtc:yyyyMMddHHmm}",
        async ct =>
        {
            await using var discovery = scopes.CreateAsyncScope();
            var accounts = await discovery.ServiceProvider.GetRequiredService<INobilityRepository>()
                .GetDueAccountsAsync(DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime), 100, ct);
            foreach (var (account, character) in accounts)
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IMediator>()
                    .Send(new SettleNobilityDailyRewardsCommand(account, character), ct);
            }
        }, context.CancellationToken);
}
