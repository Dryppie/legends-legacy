namespace Application.UseCases.Administration.Dtos;
public sealed record StateRefreshRecoveryResultDto(Guid OperationId, Guid DeliveryId, Guid CharacterId,
    Guid ReplacementMessageId, bool WasAlreadyProcessed);
