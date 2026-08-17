using Application.Interfaces;
using Domain.Models.Chats;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Chat.Repositories;

public sealed class ChatRestrictionRepository(IDbContext context)
    : IChatRestrictionRepository
{
    public Task<ChatModerationAction?> GetActionAsync(
        Guid operationId,
        CancellationToken cancellationToken) =>
        context.ChatModerationActions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken);

    public Task<ChatRestriction?> GetRestrictionAsync(
        Guid restrictionId,
        CancellationToken cancellationToken) =>
        context.ChatRestrictions
            .SingleOrDefaultAsync(x => x.Id == restrictionId, cancellationToken);

    public Task<ChatRestriction?> GetActiveMuteAsync(
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        context.ChatRestrictions
            .AsNoTracking()
            .Where(x => x.TargetCharacterId == characterId &&
                        x.RevokedAt == null &&
                        (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ChatModerationAction>> GetActionsAsync(
        Guid characterId,
        int limit,
        CancellationToken cancellationToken) =>
        await context.ChatModerationActions
            .AsNoTracking()
            .Where(x => x.TargetCharacterId == characterId)
            .OrderByDescending(x => x.OccurredAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

    public void AddRestriction(ChatRestriction restriction) =>
        context.ChatRestrictions.Add(restriction);

    public void AddAction(ChatModerationAction action) =>
        context.ChatModerationActions.Add(action);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);
}
