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

    public async Task<IReadOnlyList<ChatMessage>> SentByAsync(
        Guid senderId,
        int take,
        DateTimeOffset? beforeSentAt,
        Guid? beforeMessageId,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 51);
        var query = _context.ChatMessages
            .AsNoTracking()
            .Where(message =>
                message.SenderId == senderId &&
                !message.IsSystemGenerated);

        if (beforeSentAt.HasValue && beforeMessageId.HasValue)
        {
            var sentAt = beforeSentAt.Value;
            var messageId = beforeMessageId.Value;
            query = query.Where(message =>
                message.SentAt < sentAt ||
                (message.SentAt == sentAt && message.Id.CompareTo(messageId) < 0));
        }

        return await query
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatConversationEvidence> ConversationEvidenceAsync(
        ChatConversationEvidenceQuery query,
        CancellationToken cancellationToken)
    {
        var direct = _context.ChatMessages
            .AsNoTracking()
            .Where(message =>
                message.ChannelType == ChatChannelType.Whisper &&
                !message.IsSystemGenerated &&
                message.SentAt >= query.From &&
                message.SentAt <= query.To &&
                ((message.SenderId == query.FirstCharacterId &&
                  message.TargetCharacterId == query.SecondCharacterId) ||
                 (message.SenderId == query.SecondCharacterId &&
                  message.TargetCharacterId == query.FirstCharacterId)));

        var summary = await direct
            .GroupBy(_ => 1)
            .Select(messages => new DirectConversationSummary(
                messages.Count(message => message.SenderId == query.FirstCharacterId),
                messages.Count(message => message.SenderId == query.SecondCharacterId),
                messages.Count(message =>
                    message.SentAt >= query.ImmediateFrom &&
                    message.SentAt <= query.ImmediateTo),
                messages.Min(message => message.SentAt),
                messages.Max(message => message.SentAt)))
            .SingleOrDefaultAsync(cancellationToken);

        var sharedRows = await _context.ChatMessages
            .AsNoTracking()
            .Where(message =>
                (message.ChannelType == ChatChannelType.Guild ||
                 message.ChannelType == ChatChannelType.Raid) &&
                !message.IsSystemGenerated &&
                message.SentAt >= query.From &&
                message.SentAt <= query.To &&
                (message.SenderId == query.FirstCharacterId ||
                 message.SenderId == query.SecondCharacterId))
            .GroupBy(message => new
            {
                message.ChannelType,
                message.ContextKey,
                message.SenderId
            })
            .Select(messages => new SharedChannelSummary(
                messages.Key.ChannelType,
                messages.Key.ContextKey,
                messages.Key.SenderId,
                messages.Count()))
            .ToListAsync(cancellationToken);
        var sharedChannels = sharedRows
            .GroupBy(row => new { row.ChannelType, row.ContextKey })
            .Where(channel =>
                channel.Any(row => row.SenderId == query.FirstCharacterId) &&
                channel.Any(row => row.SenderId == query.SecondCharacterId))
            .ToList();

        IReadOnlyList<ChatMessage> messages = [];
        if (query.Take > 0)
        {
            var page = direct;
            if (query.BeforeSentAt.HasValue && query.BeforeMessageId.HasValue)
            {
                var sentAt = query.BeforeSentAt.Value;
                var messageId = query.BeforeMessageId.Value;
                page = page.Where(message =>
                    message.SentAt < sentAt ||
                    (message.SentAt == sentAt && message.Id.CompareTo(messageId) < 0));
            }

            messages = await page
                .OrderByDescending(message => message.SentAt)
                .ThenByDescending(message => message.Id)
                .Take(Math.Clamp(query.Take, 1, 26))
                .ToListAsync(cancellationToken);
        }

        return new ChatConversationEvidence(
            summary?.FirstToSecondMessageCount ?? 0,
            summary?.SecondToFirstMessageCount ?? 0,
            summary?.ImmediateMessageCount ?? 0,
            summary?.FirstMessageAt,
            summary?.LastMessageAt,
            sharedChannels.Count,
            sharedChannels.Sum(channel => channel.Sum(row => row.MessageCount)),
            messages);
    }

    private sealed record DirectConversationSummary(
        int FirstToSecondMessageCount,
        int SecondToFirstMessageCount,
        int ImmediateMessageCount,
        DateTimeOffset FirstMessageAt,
        DateTimeOffset LastMessageAt);

    private sealed record SharedChannelSummary(
        ChatChannelType ChannelType,
        string ContextKey,
        Guid SenderId,
        int MessageCount);
}
