using Microsoft.EntityFrameworkCore;
using Persistence.Chat;
using Persistence.Chat.Repositories;
using Services.Chat.Chats;

namespace Chat.Tests;

public sealed class ChatModerationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Mute_and_unmute_are_enforced_audited_and_idempotent()
    {
        await using var db = CreateDb();
        var service = new ChatModerationService(
            new ChatRestrictionRepository(db),
            new FixedTimeProvider(Now));
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

    private static ChatDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ChatDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
