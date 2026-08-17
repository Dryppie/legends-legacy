namespace Application.UseCases.Administration.Dtos;

public sealed record AccountBanResultDto(
    Guid OperationId,
    AccountRestrictionDto Restriction,
    bool WasAlreadyProcessed);
