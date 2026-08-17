namespace Application.UseCases.Administration.Dtos;

public sealed record ChatRestrictionDto(
    Guid Id,
    Guid CharacterId,
    string Reason,
    string CreatedBySubject,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? RevokedBySubject,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);
