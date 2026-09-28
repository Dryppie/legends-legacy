using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record SupportCaseEntryDto(Guid Id, Guid CaseId, int Sequence, SupportCaseEntryKind Kind, string ActorDisplayName,
    string Body, string? EvidenceReference, Guid? LinkedOperationId, string? LinkedSource, DateTimeOffset CreatedAt);
