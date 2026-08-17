namespace Application.UseCases.Administration.Dtos;

public sealed record PlayerAdministrationDetailsDto(
    PlayerAdministrationDto Player,
    ChatRestrictionDto? ActiveMute,
    bool ChatAvailable,
    string? ChatStatusMessage,
    IReadOnlyList<AdministrationHistoryDto> AdministrationHistory,
    IReadOnlyList<ChatModerationHistoryDto> ChatHistory);
