using Application.Interfaces;
using Application.Interfaces.Services.Chats;
using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;

namespace Services.Chat.Chats;

public sealed class RaidChatService(IDbContext db) : IRaidChatService
{
    public async Task<bool> ApplySnapshotAsync(
        Guid raidRunId,
        long revision,
        bool isOpen,
        IReadOnlyCollection<Guid> memberCharacterIds,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var channel = await db.RaidChatChannels
            .Include(x => x.Memberships)
            .SingleOrDefaultAsync(x => x.RaidRunId == raidRunId, cancellationToken);

        if (channel is not null && channel.Revision > revision)
            return false;
        if (channel is not null && channel.Revision == revision)
            return true;

        if (channel is null)
        {
            channel = new RaidChatChannel { RaidRunId = raidRunId };
            db.RaidChatChannels.Add(channel);
        }

        channel.Revision = revision;
        channel.IsOpen = isOpen;
        channel.UpdatedAt = updatedAt;

        var desiredMembers = isOpen
            ? memberCharacterIds.Where(x => x != Guid.Empty).ToHashSet()
            : [];
        foreach (var membership in channel.Memberships
                     .Where(x => !desiredMembers.Contains(x.CharacterId))
                     .ToArray())
        {
            db.RaidChatMemberships.Remove(membership);
        }

        var existingMembers = channel.Memberships
            .Select(x => x.CharacterId)
            .ToHashSet();
        foreach (var characterId in desiredMembers.Where(x => !existingMembers.Contains(x)))
        {
            channel.Memberships.Add(new RaidChatMembership
            {
                RaidRunId = raidRunId,
                CharacterId = characterId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> CanAccessAsync(
        Guid raidRunId,
        Guid characterId,
        CancellationToken cancellationToken) =>
        db.RaidChatMemberships.AsNoTracking().AnyAsync(
            x => x.RaidRunId == raidRunId
                 && x.CharacterId == characterId
                 && x.Channel.IsOpen,
            cancellationToken);

    public async Task<IReadOnlyList<string>> GetRecipientsForMemberAsync(
        Guid raidRunId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var canSend = await CanAccessAsync(raidRunId, characterId, cancellationToken);
        if (!canSend)
            return [];

        return await db.RaidChatMemberships.AsNoTracking()
            .Where(x => x.RaidRunId == raidRunId && x.Channel.IsOpen)
            .Select(x => x.CharacterId.ToString())
            .ToArrayAsync(cancellationToken);
    }
}
