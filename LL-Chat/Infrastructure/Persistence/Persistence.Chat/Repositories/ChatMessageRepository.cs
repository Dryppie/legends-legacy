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

    public async Task<IReadOnlyList<ChatMessage>> LatestAsync(string channel, int take, CancellationToken cancellationToken)
    {
        return await _context.ChatMessages
            .Where(m => m.Channel == channel)
            .OrderByDescending(m => m.SentAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
