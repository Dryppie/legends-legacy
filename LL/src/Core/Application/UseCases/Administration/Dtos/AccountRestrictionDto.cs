using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record AccountRestrictionDto(
    Guid Id,
    Guid AccountId,
    AccountRestrictionType RestrictionType,
    string Reason,
    string? InternalNotes,
    string CreatedBySubject,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? RevokedBySubject,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);
