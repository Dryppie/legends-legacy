namespace Application.BackgroundJobs;

public interface IBackgroundJobExecutionService
{
    Task<bool> RunOnceAsync(
        string jobName,
        string businessKey,
        Func<CancellationToken, Task> execute,
        CancellationToken cancellationToken);
}
