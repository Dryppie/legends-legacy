namespace Application.UseCases.Administration.Dtos;

public sealed record ChatModerationResultDto(
    Guid RestrictionId,
    bool WasAlreadyProcessed);
