using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record AdministrationHistoryDto(
    Guid OperationId,
    AdminActionType ActionType,
    string Permission,
    string ActorSubject,
    string ActorDisplayName,
    Guid? TargetAccountId,
    Guid? TargetCharacterId,
    Guid? TargetResourceId,
    string Reason,
    string? InternalNotes,
    string DetailsJson,
    DateTimeOffset OccurredAt);
