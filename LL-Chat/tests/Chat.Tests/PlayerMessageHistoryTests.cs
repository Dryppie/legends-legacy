using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Persistence.Chat;
using Persistence.Chat.Repositories;

namespace Chat.Tests;

public sealed class PlayerMessageHistoryTests
{
    [Fact]
    public async Task SentBy_returns_only_player_authored_messages_across_all_channels()
    {
        await using var db = CreateDb();
        var playerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.ChatMessages.AddRange(
            Message(playerId, ChatChannelType.General, "general", now.AddMinutes(-3)),
            Message(playerId, ChatChannelType.Guild, "guild", now.AddMinutes(-2)),
            Message(playerId, ChatChannelType.Whisper, "whisper", now.AddMinutes(-1)),
            Message(otherPlayerId, ChatChannelType.Trade, "other player", now),
            Message(playerId, ChatChannelType.System, "generated", now, isSystemGenerated: true));
        await db.SaveChangesAsync();

        var repository = new ChatMessageRepository(db);
        var history = await repository.SentByAsync(
            playerId,
            25,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(new[] { "whisper", "guild", "general" }, history.Select(x => x.Body));
    }

    [Fact]
    public async Task SentBy_uses_timestamp_and_id_as_a_stable_exclusive_cursor()
    {
        await using var db = CreateDb();
        var playerId = Guid.NewGuid();
        var sentAt = DateTimeOffset.UtcNow;
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var cursorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        db.ChatMessages.AddRange(
            Message(playerId, ChatChannelType.General, "lower", sentAt, lowerId),
            Message(playerId, ChatChannelType.Trade, "cursor", sentAt, cursorId),
            Message(playerId, ChatChannelType.Help, "higher", sentAt, higherId),
            Message(playerId, ChatChannelType.Guild, "older", sentAt.AddMinutes(-1)));
        await db.SaveChangesAsync();

        var repository = new ChatMessageRepository(db);
        var history = await repository.SentByAsync(
            playerId,
            25,
            sentAt,
            cursorId,
            CancellationToken.None);

        Assert.Equal(new[] { "lower", "older" }, history.Select(x => x.Body));
    }

    [Fact]
    public async Task Conversation_evidence_counts_both_whisper_directions_and_shared_channels()
    {
        await using var db = CreateDb();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var transferAt = DateTimeOffset.UtcNow;
        db.ChatMessages.AddRange(
            Whisper(first, second, "before", transferAt.AddHours(-2)),
            Whisper(first, second, "immediate", transferAt.AddMinutes(-5)),
            Whisper(second, first, "reply", transferAt.AddMinutes(1)),
            Whisper(first, unrelated, "unrelated", transferAt),
            Message(first, ChatChannelType.Guild, "first guild", transferAt.AddDays(-1), contextKey: "guild-1"),
            Message(second, ChatChannelType.Guild, "second guild", transferAt.AddDays(-1), contextKey: "guild-1"),
            Message(first, ChatChannelType.Raid, "solo raid", transferAt.AddDays(-1), contextKey: "raid-1"));
        await db.SaveChangesAsync();

        var repository = new ChatMessageRepository(db);
        var evidence = await repository.ConversationEvidenceAsync(
            new ChatConversationEvidenceQuery(
                first,
                second,
                transferAt.AddDays(-30),
                transferAt.AddHours(2),
                transferAt.AddHours(-24),
                transferAt.AddHours(2),
                null,
                null,
                2),
            CancellationToken.None);

        Assert.Equal(2, evidence.FirstToSecondMessageCount);
        Assert.Equal(1, evidence.SecondToFirstMessageCount);
        Assert.Equal(3, evidence.ImmediateMessageCount);
        Assert.Equal(1, evidence.SharedChannelCount);
        Assert.Equal(2, evidence.SharedChannelMessageCount);
        Assert.Equal(new[] { "reply", "immediate" }, evidence.Messages.Select(x => x.Body));
    }

    [Fact]
    public async Task Conversation_evidence_cursor_is_exclusive()
    {
        await using var db = CreateDb();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var transferAt = DateTimeOffset.UtcNow;
        var latest = Whisper(first, second, "latest", transferAt);
        var older = Whisper(second, first, "older", transferAt.AddMinutes(-1));
        db.ChatMessages.AddRange(latest, older);
        await db.SaveChangesAsync();

        var repository = new ChatMessageRepository(db);
        var evidence = await repository.ConversationEvidenceAsync(
            new ChatConversationEvidenceQuery(
                first,
                second,
                transferAt.AddDays(-30),
                transferAt.AddHours(2),
                transferAt.AddHours(-24),
                transferAt.AddHours(2),
                latest.SentAt,
                latest.Id,
                25),
            CancellationToken.None);

        Assert.Equal("older", Assert.Single(evidence.Messages).Body);
        Assert.Equal(1, evidence.FirstToSecondMessageCount);
        Assert.Equal(1, evidence.SecondToFirstMessageCount);
    }

    private static ChatMessage Message(
        Guid senderId,
        ChatChannelType channel,
        string body,
        DateTimeOffset sentAt,
        Guid? id = null,
        bool isSystemGenerated = false,
        Guid? targetId = null,
        string? contextKey = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            ChannelType = channel,
            ContextKey = contextKey ?? channel.ToString().ToLowerInvariant(),
            SenderId = senderId,
            SenderName = "Player",
            Body = body,
            SentAt = sentAt,
            IsSystemGenerated = isSystemGenerated,
            TargetCharacterId = targetId,
            TargetCharacterName = targetId.HasValue ? "Target" : null
        };

    private static ChatMessage Whisper(
        Guid senderId,
        Guid targetId,
        string body,
        DateTimeOffset sentAt) =>
        Message(senderId, ChatChannelType.Whisper, body, sentAt, targetId: targetId);

    private static ChatDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ChatDbContext(options);
    }
}
