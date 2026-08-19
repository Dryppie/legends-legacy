namespace Application.UseCases.Administration.Dtos;

public sealed record MultiplayerRestrictionResultDto(
    Guid OperationId,
    AccountRestrictionDto Restriction,
    bool WasAlreadyProcessed);
