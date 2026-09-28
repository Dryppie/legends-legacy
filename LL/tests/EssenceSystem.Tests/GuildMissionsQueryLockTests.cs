using Application.Interfaces.Services.LL.Guilds;
using Application.MediatR.Behaviors;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Queries.GetGuildMissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.LL;

namespace EssenceSystem.Tests;

public sealed class GuildMissionsQueryLockTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Overview_waits_for_commands_only_for_the_same_character(bool sameCharacter)
    {
        var characterId = Guid.NewGuid();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
        var behavior = new TransactionBehavior<TestCommand, bool>(
            db, null!, NullLogger<TransactionBehavior<TestCommand, bool>>.Instance, null!);
        var commandEntered = NewSignal();
        var releaseCommand = NewSignal();
        var command = behavior.Handle(new TestCommand(characterId), async _ =>
        {
            commandEntered.TrySetResult();
            await releaseCommand.Task.WaitAsync(timeout.Token);
            return true;
        }, timeout.Token);
        await commandEntered.Task.WaitAsync(timeout.Token);

        var service = new OverviewService((_, _, _) => Task.FromResult<GuildMissionOverviewDto?>(null));
        var queryCharacterId = sameCharacter ? characterId : Guid.NewGuid();
        var query = new GetGuildMissionsQueryHandler(service).Handle(
            new GetGuildMissionsQuery(queryCharacterId), timeout.Token);

        try
        {
            if (sameCharacter)
            {
                Assert.False(query.IsCompleted);
                Assert.Equal(0, service.Calls);
            }
            else
            {
                await query.WaitAsync(timeout.Token);
                Assert.Equal(1, service.Calls);
            }
        }
        finally
        {
            releaseCommand.TrySetResult();
            await command.WaitAsync(timeout.Token);
            await query.WaitAsync(timeout.Token);
        }

        Assert.Equal(1, service.Calls);
    }

    [Fact]
    public async Task Cancelled_overview_waiter_does_not_enter_service_or_block_later_queries()
    {
        var characterId = Guid.NewGuid();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var waitingCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var entered = NewSignal();
        var release = NewSignal();
        var firstService = new OverviewService(async (_, _, ct) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return null;
        });
        var first = new GetGuildMissionsQueryHandler(firstService).Handle(
            new GetGuildMissionsQuery(characterId), timeout.Token);
        await entered.Task.WaitAsync(timeout.Token);

        var waitingService = new OverviewService((_, _, _) => Task.FromResult<GuildMissionOverviewDto?>(null));
        var waitingHandler = new GetGuildMissionsQueryHandler(waitingService);
        var waiting = waitingHandler.Handle(new GetGuildMissionsQuery(characterId), waitingCancellation.Token);

        try
        {
            Assert.False(waiting.IsCompleted);
            Assert.Equal(0, waitingService.Calls);
            waitingCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
            Assert.Equal(0, waitingService.Calls);
        }
        finally
        {
            waitingCancellation.Cancel();
            release.TrySetResult();
            await first.WaitAsync(timeout.Token);
        }

        await waitingHandler.Handle(new GetGuildMissionsQuery(characterId), timeout.Token).WaitAsync(timeout.Token);
        Assert.Equal(1, waitingService.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_or_cancelled_overview_releases_character_queue(bool cancel)
    {
        var characterId = Guid.NewGuid();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var entered = NewSignal();
        var release = NewSignal();
        var service = new OverviewService(async (_, _, ct) =>
        {
            Assert.Equal(operationCancellation.Token, ct);
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            throw new InvalidOperationException("Overview failed.");
        });
        var first = new GetGuildMissionsQueryHandler(service).Handle(
            new GetGuildMissionsQuery(characterId), operationCancellation.Token);
        await entered.Task.WaitAsync(timeout.Token);

        var nextService = new OverviewService((_, _, _) => Task.FromResult<GuildMissionOverviewDto?>(null));
        var next = new GetGuildMissionsQueryHandler(nextService).Handle(
            new GetGuildMissionsQuery(characterId), timeout.Token);
        try
        {
            Assert.False(next.IsCompleted);
            Assert.Equal(0, nextService.Calls);
            if (cancel)
            {
                operationCancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
            }
            else
            {
                release.TrySetResult();
                await Assert.ThrowsAsync<InvalidOperationException>(() => first);
            }

            await next.WaitAsync(timeout.Token);
            Assert.Equal(1, nextService.Calls);
        }
        finally
        {
            operationCancellation.Cancel();
            release.TrySetResult();
        }
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record TestCommand(Guid CharacterId) : ICommand<bool>;

    private sealed class OverviewService(
        Func<Guid, DateTimeOffset, CancellationToken, Task<GuildMissionOverviewDto?>> getOverview) : IGuildMissionService
    {
        public int Calls { get; private set; }

        public Task<GuildMissionOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Calls++;
            return getOverview(characterId, now, cancellationToken);
        }

        public Task<GuildOperationResult<GuildMissionOverviewDto>> SelectMissionAsync(
            Guid characterId, Guid missionOptionId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimPersonalOrderRewardAsync(
            Guid characterId, Guid orderId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimWeeklyRewardAsync(
            Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildContributionResult> RecordContributionAsync(
            GuildContributionEvent contributionEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
