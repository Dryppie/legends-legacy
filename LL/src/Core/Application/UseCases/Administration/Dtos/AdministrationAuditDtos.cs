namespace Application.UseCases.Administration.Dtos;

public sealed record AdministrationAuditEntryDto(
    Guid OperationId,
    string Source,
    string ActionType,
    string Permission,
    string ActorSubject,
    string ActorDisplayName,
    Guid? TargetAccountId,
    Guid? TargetCharacterId,
    Guid? TargetResourceId,
    string Reason,
    string? InternalNotes,
    string DetailsJson,
    string RiskLevel,
    string Outcome,
    DateTimeOffset OccurredAt);

public sealed record AdministrationAuditPageDto(
    IReadOnlyList<AdministrationAuditEntryDto> Entries,
    string? NextCursor,
    IReadOnlyList<string> UnavailableSources);
