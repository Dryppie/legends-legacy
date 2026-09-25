using Application.Common.Interfaces;
using Application.UseCases.Quests.Queries.GetQuestJournal;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class QuestJournalQueryLockTests
{
    [Fact]
    public async Task Concurrent_journal_queries_queue_before_entering_database_lock_scope()
    {
        var characterId = Guid.NewGuid();
        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var firstHandler = new GetQuestJournalQueryHandler(firstContext, null!, null!);
        var secondHandler = new GetQuestJournalQueryHandler(secondContext, null!, null!);

        var first = firstHandler.Handle(
            new GetQuestJournalQuery(characterId),
            CancellationToken.None);
        await firstContext.Entered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        using var secondCancellation = new CancellationTokenSource();
        var second = secondHandler.Handle(
            new GetQuestJournalQuery(characterId),
            secondCancellation.Token);

        try
        {
            await Task.Delay(100);
            Assert.False(secondContext.Entered.Task.IsCompleted);

            secondCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        }
        finally
        {
            secondCancellation.Cancel();
            firstContext.Release.TrySetResult();
            await first;
        }
    }

    private static BlockingContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlockingContext(options);
    }

    private sealed class BlockingContext(DbContextOptions<LLDbContext> options)
        : LLDbContext(options), IDbContext
    {
        public TaskCompletionSource Entered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<T> IDbContext.ExecuteWithCharacterLockAsync<T>(
            Guid characterId,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(ct);
            return default!;
        }
    }
}
