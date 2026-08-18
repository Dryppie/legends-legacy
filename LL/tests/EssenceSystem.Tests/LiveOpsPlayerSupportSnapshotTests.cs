using API.LiveOps.Support;
using Domain.Models.Administration;
using Domain.Models.Entities.Characters;
using Domain.Models.Outbox;
using Domain.Models.Synchronization;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed class LiveOpsPlayerSupportSnapshotTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 18, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Snapshot_returns_bounded_support_data_with_source_freshness()
    {
        var seeded = await SeedAsync();
        var service = CreateService(new TestContextFactory(seeded.Options));

        var result = await service.GetAsync(seeded.CharacterId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(seeded.AccountId, result.AccountId);
        Assert.All(Sections(result), section => Assert.True(section.IsAvailable));
        Assert.Equal("Game database", result.Account.Source);
        Assert.Equal(Now, result.Account.FetchedAtUtc);
        Assert.Equal(1, result.Account.Data!.ActiveSessionCount);
        Assert.Equal("Active", Assert.Single(result.Account.Data.Restrictions).Status);
        Assert.Equal(1250, result.Economy.Data!.Cinders);
        Assert.False(result.Guild.Data!.IsMember);
        Assert.Equal(1, result.Synchronization.Data!.PendingDeliveries);
        Assert.Equal(7, Assert.Single(result.Synchronization.Data.Revisions).Revision);
    }

    [Fact]
    public async Task One_section_failure_does_not_discard_other_sections()
    {
        var seeded = await SeedAsync();
        var service = CreateService(new FailingContextFactory(seeded.Options, failAtCall: 3));

        var result = await service.GetAsync(seeded.CharacterId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, Sections(result).Count(section => !section.IsAvailable));
        Assert.Equal(5, Sections(result).Count(section => section.IsAvailable));
    }

    [Fact]
    public async Task Missing_character_returns_no_snapshot()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var service = CreateService(new TestContextFactory(options));

        Assert.Null(await service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private static IReadOnlyList<ISectionState> Sections(PlayerSupportSnapshotDto snapshot) =>
    [
        new SectionState(snapshot.Account.IsAvailable),
        new SectionState(snapshot.Activity.IsAvailable),
        new SectionState(snapshot.Economy.IsAvailable),
        new SectionState(snapshot.Guild.IsAvailable),
        new SectionState(snapshot.Marketplace.IsAvailable),
        new SectionState(snapshot.Synchronization.IsAvailable)
    ];

    private static LiveOpsPlayerSupportSnapshotService CreateService(
        IDbContextFactory<LLDbContext> factory) => new(
        factory,
        Options.Create(new LiveOpsOptions { SupportSnapshotSectionTimeoutSeconds = 3 }),
        new FixedTimeProvider(Now),
        NullLogger<LiveOpsPlayerSupportSnapshotService>.Instance);

    private static async Task<SeededDatabase> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var database = new LLDbContext(options);
        var user = AppUser.Guest();
        user.Username = "support-player";
        user.CreatedUtc = Now.UtcDateTime.AddYears(-1);
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Name = "ArdentFox",
            NormalizedName = "ARDENTFOX",
            Level = 42,
            Cinders = 1250,
            Soulstones = 75
        };
        database.Users.Add(user);
        database.Characters.Add(character);
        database.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "not-a-real-token",
            CreatedUtc = Now.UtcDateTime.AddHours(-2),
            ExpiresUtc = Now.UtcDateTime.AddDays(1)
        });
        database.AccountRestrictions.Add(new AccountRestriction
        {
            Id = Guid.NewGuid(),
            AccountId = user.Id,
            Reason = "CASE-42",
            CreatedBySubject = "owner",
            CreatedAt = Now.AddHours(-1),
            ExpiresAt = Now.AddDays(1)
        });
        database.StateSyncRevisions.Add(new StateSyncRevision
        {
            ScopeKey = $"character:{character.Id:N}:inventory",
            Revision = 7,
            UpdatedAt = Now.AddMinutes(-1)
        });
        var message = new GameEventOutboxMessage
        {
            Id = Guid.NewGuid(),
            AccountId = user.Id,
            CharacterId = character.Id,
            EventType = "test",
            CreatedAt = Now.AddMinutes(-3)
        };
        database.GameEventOutboxMessages.Add(message);
        database.GameEventOutboxDeliveries.Add(new GameEventOutboxDelivery
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            Message = message,
            Consumer = "test",
            Status = GameEventOutboxDeliveryStatus.Pending,
            CreatedAt = Now.AddMinutes(-3)
        });
        await database.SaveChangesAsync();
        return new SeededDatabase(options, user.Id, character.Id);
    }

    private interface ISectionState
    {
        bool IsAvailable { get; }
    }

    private sealed record SectionState(bool IsAvailable) : ISectionState;
    private sealed record SeededDatabase(
        DbContextOptions<LLDbContext> Options,
        Guid AccountId,
        Guid CharacterId);

    private class TestContextFactory(DbContextOptions<LLDbContext> options)
        : IDbContextFactory<LLDbContext>
    {
        public virtual LLDbContext CreateDbContext() => new(options);
        public virtual ValueTask<LLDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }

    private sealed class FailingContextFactory(
        DbContextOptions<LLDbContext> options,
        int failAtCall) : TestContextFactory(options)
    {
        private int _calls;

        public override LLDbContext CreateDbContext()
        {
            if (Interlocked.Increment(ref _calls) == failAtCall)
            {
                throw new InvalidOperationException("Simulated section failure.");
            }
            return base.CreateDbContext();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
