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

    public async Task<IReadOnlyList<ChatMessage>> LatestAsync(Guid userId, int take, string? guildChannel, CancellationToken cancellationToken)
    {
        var publicChannels = new List<ChatChannelType> { ChatChannelType.General, ChatChannelType.Trade, ChatChannelType.Help };

        var publicMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => publicChannels.Contains(m.ChannelType))
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var guildMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChannelType == ChatChannelType.Guild && m.ContextKey == guildChannel)
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var whisperMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChannelType == ChatChannelType.Whisper && (m.SenderId == userId || m.TargetUserId == userId))
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return [.. publicMessages, .. guildMessages, .. whisperMessages];
    }
}
