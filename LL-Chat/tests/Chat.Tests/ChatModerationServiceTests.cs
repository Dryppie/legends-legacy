using Microsoft.EntityFrameworkCore;
using Persistence.Chat;
using Persistence.Chat.Repositories;
using Services.Chat.Chats;
using Domain.Models.Chats;

namespace Chat.Tests;

public sealed class ChatModerationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Mute_and_unmute_are_enforced_audited_and_idempotent()
    {
        await using var db = CreateDb();
        var clock = new FixedTimeProvider(Now);
        var service = new ChatModerationService(
            new ChatRestrictionRepository(db),
            clock);
        var characterId = Guid.NewGuid();
        var muteOperationId = Guid.NewGuid();

        var muted = await service.MuteAsync(
            muteOperationId,
            characterId,
            "staff|moderator-1",
            "Moderator One",
            "Support case LL-321",
            Now.AddHours(2),
            CancellationToken.None);

        Assert.True(muted.IsSuccess);
        Assert.False(muted.WasAlreadyProcessed);
        Assert.NotNull(await service.GetActiveMuteAsync(characterId, CancellationToken.None));
        Assert.Single(await db.ChatRestrictions.ToListAsync());
        Assert.Single(await db.ChatModerationActions.ToListAsync());

        var replay = await service.MuteAsync(
            muteOperationId,
            characterId,
            "staff|moderator-1",
            "Moderator One",
            "Support case LL-321",
            Now.AddHours(2),
            CancellationToken.None);

        Assert.True(replay.IsSuccess);
        Assert.True(replay.WasAlreadyProcessed);
        Assert.Single(await db.ChatRestrictions.ToListAsync());
        Assert.Single(await db.ChatModerationActions.ToListAsync());

        clock.Advance(TimeSpan.FromMinutes(1));
        var unmuted = await service.UnmuteAsync(
            Guid.NewGuid(),
            muted.Restriction!.Id,
            "staff|moderator-2",
            "Moderator Two",
            "Appeal approved",
            CancellationToken.None);

        Assert.True(unmuted.IsSuccess);
        Assert.Null(await service.GetActiveMuteAsync(characterId, CancellationToken.None));
        Assert.Single(await db.ChatRestrictions.ToListAsync());
        Assert.Equal(2, await db.ChatModerationActions.CountAsync());

        var history = await service.GetHistoryAsync(characterId, 20, CancellationToken.None);
        Assert.Equal(2, history.Count);
        Assert.Equal("Unmuted", history[0].ActionType.ToString());
        Assert.Equal("Muted", history[1].ActionType.ToString());
    }

    [Fact]
    public async Task Moderation_audit_entries_are_append_only()
    {
        await using var db = CreateDb();
        var service = new ChatModerationService(
            new ChatRestrictionRepository(db),
            new FixedTimeProvider(Now));
        await service.MuteAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "staff|moderator-1",
            "Moderator One",
            "Support case LL-654",
            null,
            CancellationToken.None);

        var action = await db.ChatModerationActions.SingleAsync();
        action.Reason = "Changed after the fact";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());
        Assert.Contains("append-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Global_moderation_audit_filters_and_pages()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var newestId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var olderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        db.ChatModerationActions.AddRange(
            Action(newestId, characterId, ChatModerationActionType.Muted, Now),
            Action(olderId, characterId, ChatModerationActionType.Muted, Now.AddMinutes(-1)),
            Action(Guid.NewGuid(), Guid.NewGuid(), ChatModerationActionType.Unmuted, Now.AddMinutes(-2)));
        await db.SaveChangesAsync();
        var service = new ChatModerationService(
            new ChatRestrictionRepository(db),
            new FixedTimeProvider(Now));

        var first = await service.GetAuditAsync(
            new ChatModerationAuditQuery(
                null,
                null,
                ChatModerationActionType.Muted,
                "moderator@",
                "case reference",
                null,
                [characterId],
                null,
                null,
                null,
                1),
            CancellationToken.None);
        var firstEntry = Assert.Single(first);
        Assert.Equal(newestId, firstEntry.Id);

        var second = await service.GetAuditAsync(
            new ChatModerationAuditQuery(
                null,
                null,
                ChatModerationActionType.Muted,
                "moderator@",
                "case reference",
                null,
                [characterId],
                null,
                firstEntry.OccurredAt,
                firstEntry.Id,
                1),
            CancellationToken.None);
        Assert.Equal(olderId, Assert.Single(second).Id);
    }

    private static ChatModerationAction Action(
        Guid operationId,
        Guid characterId,
        ChatModerationActionType actionType,
        DateTimeOffset occurredAt) =>
        new()
        {
            Id = operationId,
            ActionType = actionType,
            TargetCharacterId = characterId,
            RestrictionId = Guid.NewGuid(),
            ActorSubject = "moderator@example.test",
            ActorDisplayName = "Moderator",
            Reason = "Case reference",
            OccurredAt = occurredAt
        };

    private static ChatDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ChatDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
