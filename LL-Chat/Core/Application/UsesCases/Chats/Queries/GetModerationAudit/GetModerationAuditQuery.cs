using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using Domain.Models.Chats;
using MediatR;

namespace Application.UsesCases.Chats.Queries.GetModerationAudit;

public sealed record GetModerationAuditQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? ActionType,
    string? Actor,
    string? Reference,
    Guid? OperationId,
    IReadOnlyCollection<Guid> CharacterIds,
    Guid? RestrictionId,
    DateTimeOffset? BeforeOccurredAt,
    Guid? BeforeOperationId,
    int Take = 51) : IRequest<IReadOnlyList<ChatModerationHistoryEntryDto>>;

public sealed class GetModerationAuditQueryHandler(IChatModerationService moderation)
    : IRequestHandler<GetModerationAuditQuery, IReadOnlyList<ChatModerationHistoryEntryDto>>
{
    public async Task<IReadOnlyList<ChatModerationHistoryEntryDto>> Handle(
        GetModerationAuditQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ActionType) &&
            !Enum.TryParse<ChatModerationActionType>(request.ActionType, true, out _))
        {
            return [];
        }

        var actionType = Enum.TryParse<ChatModerationActionType>(
            request.ActionType,
            true,
            out var parsedActionType)
            ? parsedActionType
            : (ChatModerationActionType?)null;
        var entries = await moderation.GetAuditAsync(
            new ChatModerationAuditQuery(
                request.From,
                request.To,
                actionType,
                request.Actor,
                request.Reference,
                request.OperationId,
                request.CharacterIds,
                request.RestrictionId,
                request.BeforeOccurredAt,
                request.BeforeOperationId,
                request.Take),
            cancellationToken);

        return entries.Select(x => new ChatModerationHistoryEntryDto(
                x.Id,
                x.ActionType.ToString(),
                x.TargetCharacterId,
                x.RestrictionId,
                x.ActorSubject,
                x.ActorDisplayName,
                x.Reason,
                x.OccurredAt))
            .ToList();
    }
}
