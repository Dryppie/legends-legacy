namespace Application.UseCases.Administration.Dtos;
public sealed record CompensationPackageReceiptDto(Guid OperationId, Guid? CharacterId, Guid? AccountId, bool WasAlreadyProcessed);
