using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;
using Persistence.Chat;
using Persistence.Chat.Repositories;
using Services.Chat.Chats;

namespace Chat.Tests;

public sealed class RaidChatServiceTests
{
    [Fact]
    public async Task Snapshot_updates_membership_ignores_older_revisions_and_closes_channel()
    {
        await using var db = CreateDb();
        var service = new RaidChatService(db);
        var raidRunId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        await service.ApplySnapshotAsync(
            raidRunId,
            1,
            true,
            [leaderId, memberId],
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        var recipients = await service.GetRecipientsForMemberAsync(
            raidRunId,
            leaderId,
            CancellationToken.None);
        Assert.Equal(
            new[] { leaderId.ToString(), memberId.ToString() }.Order(),
            recipients.Order());

        await service.ApplySnapshotAsync(
            raidRunId,
            2,
            true,
            [memberId],
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        Assert.False(await service.CanAccessAsync(raidRunId, leaderId, CancellationToken.None));

        await service.ApplySnapshotAsync(
            raidRunId,
            1,
            true,
            [leaderId, memberId],
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        Assert.False(await service.CanAccessAsync(raidRunId, leaderId, CancellationToken.None));

        await service.ApplySnapshotAsync(
            raidRunId,
            3,
            false,
            [memberId],
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        Assert.False(await service.CanAccessAsync(raidRunId, memberId, CancellationToken.None));
        Assert.Empty(await db.RaidChatMemberships.ToListAsync());
    }

    [Fact]
    public async Task History_only_includes_the_requested_raid_channel()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var firstRaidId = Guid.NewGuid();
        var secondRaidId = Guid.NewGuid();
        db.ChatMessages.AddRange(
            Message(firstRaidId, characterId, "old raid"),
            Message(secondRaidId, characterId, "new raid"));
        await db.SaveChangesAsync();

        var repository = new ChatMessageRepository(db);
        var history = await repository.LatestAsync(
            characterId,
            50,
            guildChannel: null,
            raidChannel: secondRaidId.ToString(),
            after: null,
            CancellationToken.None);

        var raidMessages = history.Where(x => x.ChannelType == ChatChannelType.Raid).ToArray();
        Assert.Equal("new raid", Assert.Single(raidMessages).Body);
    }

    private static ChatMessage Message(Guid raidRunId, Guid senderId, string body) =>
        new()
        {
            ChannelType = ChatChannelType.Raid,
            ContextKey = raidRunId.ToString(),
            SenderId = senderId,
            SenderName = "Raider",
            Body = body
        };

    private static ChatDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ChatDbContext(options);
    }
}
