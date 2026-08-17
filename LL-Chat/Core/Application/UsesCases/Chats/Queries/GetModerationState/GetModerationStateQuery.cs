using Application.Interfaces.Services.Chats;
using Application.UsesCases.Chats.Dtos;
using MediatR;

namespace Application.UsesCases.Chats.Queries.GetModerationState;

public sealed record GetModerationStateQuery(Guid CharacterId, int Limit = 50)
    : IRequest<ChatModerationStateDto>;

public sealed class GetModerationStateQueryHandler(IChatModerationService moderation)
    : IRequestHandler<GetModerationStateQuery, ChatModerationStateDto>
{
    public async Task<ChatModerationStateDto> Handle(
        GetModerationStateQuery request,
        CancellationToken cancellationToken)
    {
        var activeMute = await moderation.GetActiveMuteAsync(
            request.CharacterId,
            cancellationToken);
        var history = await moderation.GetHistoryAsync(
            request.CharacterId,
            request.Limit,
            cancellationToken);

        return new ChatModerationStateDto(
            activeMute is null
                ? null
                : new ChatRestrictionStateDto(
                    activeMute.Id,
                    activeMute.TargetCharacterId,
                    activeMute.Reason,
                    activeMute.CreatedBySubject,
                    activeMute.CreatedAt,
                    activeMute.ExpiresAt,
                    activeMute.RevokedBySubject,
                    activeMute.RevokedAt,
                    activeMute.RevocationReason),
            history.Select(x => new ChatModerationHistoryEntryDto(
                    x.Id,
                    x.ActionType.ToString(),
                    x.TargetCharacterId,
                    x.RestrictionId,
                    x.ActorSubject,
                    x.ActorDisplayName,
                    x.Reason,
                    x.OccurredAt))
                .ToList());
    }
}
