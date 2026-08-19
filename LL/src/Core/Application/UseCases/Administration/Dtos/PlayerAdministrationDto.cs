namespace Application.UseCases.Administration.Dtos;

public sealed record PlayerAdministrationDto(
    Guid AccountId,
    Guid CharacterId,
    string AccountLabel,
    string? Email,
    string CharacterName,
    int CharacterLevel,
    DateTime CreatedUtc,
    Guid? ActiveBanId,
    string? ActiveBanReason,
    DateTimeOffset? ActiveBanExpiresAt,
    Guid? ActiveMultiplayerRestrictionId,
    string? ActiveMultiplayerRestrictionReason,
    DateTimeOffset? ActiveMultiplayerRestrictionExpiresAt);
