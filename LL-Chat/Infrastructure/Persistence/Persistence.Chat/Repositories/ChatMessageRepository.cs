using Application.Interfaces;
using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Chat.Repositories;
public class ChatMessageRepository : IChatMessageRepository
{
    private readonly IDbContext _context;

    public ChatMessageRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        await _context.ChatMessages.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ChatMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ChatMessage>> LatestAsync(
        Guid userId,
        int take,
        string? guildChannel,
        string? raidChannel,
        DateTimeOffset? after,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 200);
        var publicChannels = new List<ChatChannelType> { ChatChannelType.General, ChatChannelType.Trade, ChatChannelType.Help };
        var query = _context.ChatMessages
            .AsNoTracking()
            .Where(m =>
                publicChannels.Contains(m.ChannelType) ||
                (m.ChannelType == ChatChannelType.Guild && m.ContextKey == guildChannel) ||
                (m.ChannelType == ChatChannelType.Raid && m.ContextKey == raidChannel) ||
                (m.ChannelType == ChatChannelType.Whisper &&
                    (m.SenderId == userId || m.TargetCharacterId == userId)) ||
                (m.ChannelType == ChatChannelType.System &&
                    (m.TargetCharacterId == null || m.TargetCharacterId == userId)));

        if (after.HasValue)
        {
            return await query
                // Include the cursor timestamp; clients merge by stable message ID,
                // so equal-timestamp messages cannot be lost at the boundary.
                .Where(m => m.SentAt >= after.Value)
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        var latest = await query
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
        latest.Reverse();
        return latest;
    }
}
