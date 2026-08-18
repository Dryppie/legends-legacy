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

    public async Task<IReadOnlyList<ChatModerationAction>> GetAuditAsync(
        ChatModerationAuditQuery query,
        CancellationToken cancellationToken)
    {
        var actions = context.ChatModerationActions.AsNoTracking();

        if (query.From.HasValue)
        {
            actions = actions.Where(x => x.OccurredAt >= query.From.Value);
        }
        if (query.To.HasValue)
        {
            actions = actions.Where(x => x.OccurredAt <= query.To.Value);
        }
        if (query.ActionType.HasValue)
        {
            actions = actions.Where(x => x.ActionType == query.ActionType.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Actor))
        {
            var actor = query.Actor.Trim().ToUpper();
            actions = actions.Where(x =>
                x.ActorSubject.ToUpper().Contains(actor) ||
                x.ActorDisplayName.ToUpper().Contains(actor));
        }
        if (!string.IsNullOrWhiteSpace(query.Reference))
        {
            var reference = query.Reference.Trim().ToUpper();
            actions = actions.Where(x => x.Reason.ToUpper().Contains(reference));
        }
        if (query.OperationId.HasValue)
        {
            actions = actions.Where(x => x.Id == query.OperationId.Value);
        }
        if (query.CharacterIds.Count > 0)
        {
            actions = actions.Where(x => query.CharacterIds.Contains(x.TargetCharacterId));
        }
        if (query.RestrictionId.HasValue)
        {
            actions = actions.Where(x => x.RestrictionId == query.RestrictionId.Value);
        }
        if (query.BeforeOccurredAt.HasValue && query.BeforeOperationId.HasValue)
        {
            var occurredAt = query.BeforeOccurredAt.Value;
            var operationId = query.BeforeOperationId.Value;
            actions = actions.Where(x =>
                x.OccurredAt < occurredAt ||
                (x.OccurredAt == occurredAt && x.Id.CompareTo(operationId) < 0));
        }

        return await actions
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(Math.Clamp(query.Limit, 1, 101))
            .ToListAsync(cancellationToken);
    }

    public void AddRestriction(ChatRestriction restriction) =>
        context.ChatRestrictions.Add(restriction);

    public void AddAction(ChatModerationAction action) =>
        context.ChatModerationActions.Add(action);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);
}
