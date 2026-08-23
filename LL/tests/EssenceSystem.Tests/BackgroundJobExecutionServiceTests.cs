using Application.BackgroundJobs;
using Domain.Models.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL.BackgroundJobs;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class BackgroundJobExecutionServiceTests
{
    [Fact]
    public async Task RunOnceAsync_WhenExecutionDoesNotExist_RunsCallbackAndCompletesExecution()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var callbackRan = false;
        var beforeRun = DateTimeOffset.UtcNow;

        var result = await service.RunOnceAsync(
            "system.test",
            "test:1",
            _ =>
            {
                callbackRan = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        var execution = await context.BackgroundJobExecutions.SingleAsync();
        Assert.True(result);
        Assert.True(callbackRan);
        Assert.Equal(BackgroundJobExecutionStatus.Completed, execution.Status);
        Assert.Equal(1, execution.Attempt);
        Assert.NotNull(execution.CompletedAt);
        Assert.Null(execution.FailedAt);
        Assert.Null(execution.ErrorMessage);
        Assert.Null(execution.ErrorDetails);
        Assert.NotEqual(Guid.Empty, execution.ConcurrencyStamp);
        Assert.True(execution.StartedAt >= beforeRun);
        Assert.True(execution.CompletedAt >= execution.StartedAt);
        Assert.True(execution.UpdatedAt >= execution.CompletedAt);
    }

    [Fact]
    public async Task RunOnceAsync_WhenExecutionAlreadyCompleted_SkipsCallback()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.BackgroundJobExecutions.Add(new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = "system.test",
            BusinessKey = "test:completed",
            Status = BackgroundJobExecutionStatus.Completed,
            StartedAt = now.AddMinutes(-5),
            CompletedAt = now.AddMinutes(-4),
            Attempt = 1,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = now.AddMinutes(-5),
            UpdatedAt = now.AddMinutes(-4)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var callbackRan = false;

        var result = await service.RunOnceAsync(
            "system.test",
            "test:completed",
            _ =>
            {
                callbackRan = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.False(result);
        Assert.False(callbackRan);
        Assert.Equal(1, await context.BackgroundJobExecutions.CountAsync());
    }

    [Fact]
    public async Task RunOnceAsync_WhenCallbackThrows_MarksExecutionFailed()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunOnceAsync(
            "system.test",
            "test:failure",
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None));

        var execution = await context.BackgroundJobExecutions.SingleAsync();
        Assert.Equal(BackgroundJobExecutionStatus.Failed, execution.Status);
        Assert.Equal(1, execution.Attempt);
        Assert.Equal("boom", execution.ErrorMessage);
        Assert.NotNull(execution.ErrorDetails);
        Assert.NotNull(execution.FailedAt);
        Assert.Null(execution.CompletedAt);
        Assert.NotEqual(Guid.Empty, execution.ConcurrencyStamp);
        Assert.True(execution.UpdatedAt >= execution.FailedAt);
    }

    [Fact]
    public async Task RunOnceAsync_WhenPreviousExecutionFailed_AllowsRetry()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.BackgroundJobExecutions.Add(new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = "system.test",
            BusinessKey = "test:retry-failed",
            Status = BackgroundJobExecutionStatus.Failed,
            StartedAt = now.AddMinutes(-10),
            FailedAt = now.AddMinutes(-9),
            Attempt = 1,
            ConcurrencyStamp = Guid.NewGuid(),
            ErrorMessage = "old",
            ErrorDetails = "old",
            CreatedAt = now.AddMinutes(-10),
            UpdatedAt = now.AddMinutes(-9)
        });
        await context.SaveChangesAsync();
        var originalStartedAt = context.BackgroundJobExecutions.Single().StartedAt;
        var originalConcurrencyStamp = context.BackgroundJobExecutions.Single().ConcurrencyStamp;

        var service = CreateService(context);
        var callbackRan = false;

        var result = await service.RunOnceAsync(
            "system.test",
            "test:retry-failed",
            _ =>
            {
                callbackRan = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var execution = await context.BackgroundJobExecutions.SingleAsync();
        Assert.True(result);
        Assert.True(callbackRan);
        Assert.Equal(BackgroundJobExecutionStatus.Completed, execution.Status);
        Assert.Equal(2, execution.Attempt);
        Assert.Null(execution.ErrorMessage);
        Assert.Null(execution.ErrorDetails);
        Assert.Null(execution.FailedAt);
        Assert.NotNull(execution.CompletedAt);
        Assert.True(execution.StartedAt >= originalStartedAt);
        Assert.NotEqual(originalConcurrencyStamp, execution.ConcurrencyStamp);
    }

    [Fact]
    public async Task RunOnceAsync_WhenExecutionIsRecentlyRunning_SkipsCallback()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.BackgroundJobExecutions.Add(new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = "system.test",
            BusinessKey = "test:running",
            Status = BackgroundJobExecutionStatus.Running,
            StartedAt = now.AddMinutes(-5),
            Attempt = 1,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = now.AddMinutes(-5),
            UpdatedAt = now.AddMinutes(-5)
        });
        await context.SaveChangesAsync();
        var originalStartedAt = context.BackgroundJobExecutions.Single().StartedAt;
        var originalUpdatedAt = context.BackgroundJobExecutions.Single().UpdatedAt;

        var service = CreateService(context);
        var callbackRan = false;

        var result = await service.RunOnceAsync(
            "system.test",
            "test:running",
            _ =>
            {
                callbackRan = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        var execution = await context.BackgroundJobExecutions.SingleAsync();
        Assert.False(result);
        Assert.False(callbackRan);
        Assert.Equal(BackgroundJobExecutionStatus.Running, execution.Status);
        Assert.Equal(1, execution.Attempt);
        Assert.Equal(originalStartedAt, execution.StartedAt);
        Assert.Equal(originalUpdatedAt, execution.UpdatedAt);
    }

    [Fact]
    public async Task RunOnceAsync_WhenRunningExecutionIsStale_AllowsRetry()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.BackgroundJobExecutions.Add(new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = "system.test",
            BusinessKey = "test:stale-running",
            Status = BackgroundJobExecutionStatus.Running,
            StartedAt = now.AddMinutes(-31),
            Attempt = 1,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = now.AddMinutes(-31),
            UpdatedAt = now.AddMinutes(-31)
        });
        await context.SaveChangesAsync();
        var originalStartedAt = context.BackgroundJobExecutions.Single().StartedAt;
        var originalConcurrencyStamp = context.BackgroundJobExecutions.Single().ConcurrencyStamp;

        var service = CreateService(context);
        var callbackRan = false;

        var result = await service.RunOnceAsync(
            "system.test",
            "test:stale-running",
            _ =>
            {
                callbackRan = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var execution = await context.BackgroundJobExecutions.SingleAsync();
        Assert.True(result);
        Assert.True(callbackRan);
        Assert.Equal(BackgroundJobExecutionStatus.Completed, execution.Status);
        Assert.Equal(2, execution.Attempt);
        Assert.True(execution.StartedAt > originalStartedAt);
        Assert.NotEqual(originalConcurrencyStamp, execution.ConcurrencyStamp);
        Assert.NotNull(execution.CompletedAt);
        Assert.Null(execution.FailedAt);
    }

    [Fact]
    public async Task RunOnceAsync_DoesNotSavePendingCallbackChangesWithExecutionMetadata()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var pendingExecution = new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = "system.pending",
            BusinessKey = "pending:1",
            Status = BackgroundJobExecutionStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            Attempt = 1,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await service.RunOnceAsync(
            "system.test",
            "test:isolated-context",
            _ =>
            {
                context.BackgroundJobExecutions.Add(pendingExecution);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await using var verificationContext = CreateSiblingContext(context);
        var execution = await verificationContext.BackgroundJobExecutions.SingleAsync();
        Assert.Equal("test:isolated-context", execution.BusinessKey);
        Assert.Equal(BackgroundJobExecutionStatus.Completed, execution.Status);
        Assert.Equal(EntityState.Added, context.Entry(pendingExecution).State);
    }

    [Fact]
    public async Task RunOnceAsync_WhenFailureBookkeepingLosesConcurrencyRace_RethrowsCallbackException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunOnceAsync(
            "system.test",
            "test:preserve-original-error",
            async _ =>
            {
                await using var competingContext = CreateSiblingContext(context);
                var competingExecution = await competingContext.BackgroundJobExecutions.SingleAsync();
                competingExecution.ConcurrencyStamp = Guid.NewGuid();
                await competingContext.SaveChangesAsync();

                throw new InvalidOperationException("original callback failure");
            },
            CancellationToken.None));

        Assert.Equal("original callback failure", exception.Message);
    }

    private static LLDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static BackgroundJobExecutionService CreateService(LLDbContext context)
    {
        return new BackgroundJobExecutionService(
            new TestContextFactory(GetOptions(context)),
            Options.Create(new BackgroundJobOptions
            {
                MaxConcurrency = 5,
                RunningExecutionTimeoutMinutes = 30
            }),
            NullLogger<BackgroundJobExecutionService>.Instance);
    }

    private static LLDbContext CreateSiblingContext(LLDbContext context) => new(GetOptions(context));

    private static DbContextOptions<LLDbContext> GetOptions(LLDbContext context) =>
        (DbContextOptions<LLDbContext>)context.GetService<IDbContextOptions>();

    private sealed class TestContextFactory(DbContextOptions<LLDbContext> options)
        : IDbContextFactory<LLDbContext>
    {
        public LLDbContext CreateDbContext() => new(options);
    }
}
