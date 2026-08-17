namespace Application.UseCases.Administration.Dtos;

public sealed record ChatModerationHistoryDto(
    Guid OperationId,
    string ActionType,
    Guid CharacterId,
    Guid RestrictionId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason,
    DateTimeOffset OccurredAt);
