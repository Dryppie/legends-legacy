using Domain.Models.Inventories;

namespace Domain.Models.Administration;

public sealed record AdministrationActor(string Subject, string DisplayName);

public sealed record PlayerAdministrationSnapshot(
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

public sealed record AccountBanOperation(
    AdminAction Action,
    AccountRestriction Restriction,
    bool WasAlreadyProcessed);

public sealed record MultiplayerRestrictionOperation(
    AdminAction Action,
    AccountRestriction Restriction,
    bool WasAlreadyProcessed);

public sealed record ItemGrantOperation(
    AdminAction Action,
    Guid AccountId,
    Guid CharacterId,
    string ItemBaseId,
    int Quantity,
    IReadOnlyList<InventoryItem> GrantedItems,
    bool WasAlreadyProcessed);

public sealed record AdministrationOperationResult<T>(
    bool IsSuccess,
    T? Value,
    string ErrorCode,
    string ErrorMessage)
{
    public static AdministrationOperationResult<T> Success(T value) =>
        new(true, value, string.Empty, string.Empty);

    public static AdministrationOperationResult<T> Fail(string code, string message) =>
        new(false, default, code, message);
}
