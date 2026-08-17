namespace Application.UsesCases.Chats.Dtos;

public sealed record ChatModerationDto(
    Guid RestrictionId,
    bool WasAlreadyProcessed);
