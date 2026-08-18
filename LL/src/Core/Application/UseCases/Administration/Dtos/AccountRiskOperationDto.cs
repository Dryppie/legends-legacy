using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record AccountRiskOperationDto(
    Guid OperationId,
    bool WasAlreadyProcessed,
    AccountInvestigationStatus? Status,
    AccountRiskNoteDto? Note);
