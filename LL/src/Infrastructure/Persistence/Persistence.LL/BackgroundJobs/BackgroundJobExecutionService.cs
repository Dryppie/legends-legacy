using Application.BackgroundJobs;
using Domain.Models.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Persistence.LL.BackgroundJobs;

public sealed class BackgroundJobExecutionService : IBackgroundJobExecutionService
{
    private readonly IDbContextFactory<LLDbContext> _dbContextFactory;
    private readonly ILogger<BackgroundJobExecutionService> _logger;
    private readonly TimeSpan _runningExecutionTimeout;

    public BackgroundJobExecutionService(
        IDbContextFactory<LLDbContext> dbContextFactory,
        IOptions<BackgroundJobOptions> options,
        ILogger<BackgroundJobExecutionService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _runningExecutionTimeout = TimeSpan.FromMinutes(options.Value.RunningExecutionTimeoutMinutes);
    }

    public async Task<bool> RunOnceAsync(
        string jobName,
        string businessKey,
        Func<CancellationToken, Task> execute,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessKey);
        ArgumentNullException.ThrowIfNull(execute);

        await using var executionContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var execution = await TryStartExecutionAsync(
            executionContext,
            jobName,
            businessKey,
            cancellationToken);
        if (execution is null)
        {
            return false;
        }

        var startedAt = Stopwatch.GetTimestamp();
        _logger.LogInformation(
            "Starting background job {JobName} for business key {BusinessKey}. ExecutionId: {ExecutionId}, Attempt: {Attempt}",
            jobName,
            businessKey,
            execution.Id,
            execution.Attempt);

        try
        {
            await execute(cancellationToken);
        }
        catch (Exception ex)
        {
            var failedAt = DateTimeOffset.UtcNow;
            execution.Status = BackgroundJobExecutionStatus.Failed;
            execution.FailedAt = failedAt;
            execution.ErrorMessage = ex.Message;
            execution.ErrorDetails = ex.ToString();
            execution.ConcurrencyStamp = Guid.NewGuid();
            execution.UpdatedAt = failedAt;

            try
            {
                await executionContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception persistenceException)
            {
                _logger.LogError(
                    persistenceException,
                    "Could not mark failed background job {JobName} for business key {BusinessKey}. ExecutionId: {ExecutionId}, Attempt: {Attempt}",
                    jobName,
                    businessKey,
                    execution.Id,
                    execution.Attempt);
            }

            _logger.LogError(
                ex,
                "Background job {JobName} failed for business key {BusinessKey}. ExecutionId: {ExecutionId}, Attempt: {Attempt}, ElapsedMs: {ElapsedMs}",
                jobName,
                businessKey,
                execution.Id,
                execution.Attempt,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            throw;
        }

        var completedAt = DateTimeOffset.UtcNow;
        execution.Status = BackgroundJobExecutionStatus.Completed;
        execution.CompletedAt = completedAt;
        execution.FailedAt = null;
        execution.ErrorMessage = null;
        execution.ErrorDetails = null;
        execution.ConcurrencyStamp = Guid.NewGuid();
        execution.UpdatedAt = completedAt;

        await executionContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed background job {JobName} for business key {BusinessKey}. ExecutionId: {ExecutionId}, Attempt: {Attempt}, ElapsedMs: {ElapsedMs}",
            jobName,
            businessKey,
            execution.Id,
            execution.Attempt,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        return true;
    }

    private async Task<BackgroundJobExecution?> TryStartExecutionAsync(
        LLDbContext dbContext,
        string jobName,
        string businessKey,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var execution = await dbContext.BackgroundJobExecutions
            .SingleOrDefaultAsync(
                x => x.JobName == jobName && x.BusinessKey == businessKey,
                cancellationToken);

        if (execution is not null)
        {
            return await TryReuseExecutionAsync(dbContext, execution, now, cancellationToken);
        }

        execution = new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            BusinessKey = businessKey,
            Status = BackgroundJobExecutionStatus.Running,
            StartedAt = now,
            Attempt = 1,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.BackgroundJobExecutions.Add(execution);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return execution;
        }
        catch (DbUpdateException)
        {
            dbContext.GetEntry(execution).State = EntityState.Detached;

            _logger.LogInformation(
                "Background job {JobName} for business key {BusinessKey} lost the insert race; loading existing execution row.",
                jobName,
                businessKey);

            var existing = await dbContext.BackgroundJobExecutions
                .SingleOrDefaultAsync(
                    x => x.JobName == jobName && x.BusinessKey == businessKey,
                    cancellationToken);

            if (existing is null)
            {
                throw;
            }

            return await TryReuseExecutionAsync(dbContext, existing, now, cancellationToken);
        }
    }

    private async Task<BackgroundJobExecution?> TryReuseExecutionAsync(
        LLDbContext dbContext,
        BackgroundJobExecution execution,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (execution.Status == BackgroundJobExecutionStatus.Completed)
        {
            _logger.LogInformation(
                "Skipping completed background job {JobName} for business key {BusinessKey}. ExecutionId: {ExecutionId}, Attempt: {Attempt}",
                execution.JobName,
                execution.BusinessKey,
                execution.Id,
                execution.Attempt);

            return null;
        }

        if (execution.Status == BackgroundJobExecutionStatus.Running
            && now - execution.StartedAt < _runningExecutionTimeout)
        {
            _logger.LogWarning(
                "Skipping running background job {JobName} for business key {BusinessKey}. ExecutionId: {ExecutionId}, Attempt: {Attempt}, AgeSeconds: {AgeSeconds}, TimeoutMinutes: {TimeoutMinutes}",
                execution.JobName,
                execution.BusinessKey,
                execution.Id,
                execution.Attempt,
                (now - execution.StartedAt).TotalSeconds,
                _runningExecutionTimeout.TotalMinutes);

            return null;
        }

        var previousAttempt = execution.Attempt;
        execution.Status = BackgroundJobExecutionStatus.Running;
        execution.StartedAt = now;
        execution.CompletedAt = null;
        execution.FailedAt = null;
        execution.ErrorMessage = null;
        execution.ErrorDetails = null;
        execution.Attempt += 1;
        execution.ConcurrencyStamp = Guid.NewGuid();
        execution.UpdatedAt = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.GetEntry(execution).State = EntityState.Detached;

            _logger.LogWarning(
                "Skipping background job {JobName} for business key {BusinessKey} because another worker claimed it first. ExecutionId: {ExecutionId}, PreviousAttempt: {PreviousAttempt}",
                execution.JobName,
                execution.BusinessKey,
                execution.Id,
                previousAttempt);

            return null;
        }

        _logger.LogInformation(
            "Retrying background job {JobName} for business key {BusinessKey}. ExecutionId: {ExecutionId}, PreviousAttempt: {PreviousAttempt}, Attempt: {Attempt}",
            execution.JobName,
            execution.BusinessKey,
            execution.Id,
            previousAttempt,
            execution.Attempt);

        return execution;
    }
}
