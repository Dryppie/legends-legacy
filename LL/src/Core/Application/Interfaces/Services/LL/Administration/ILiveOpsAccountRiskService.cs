using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public sealed record AccountRiskOperationResult(
    bool IsSuccess,
    string ErrorMessage,
    Guid OperationId,
    bool WasAlreadyProcessed,
    AccountInvestigationStatus? Status,
    AccountRiskNote? Note)
{
    public static AccountRiskOperationResult Success(
        Guid operationId,
        bool alreadyProcessed,
        AccountInvestigationStatus? status = null,
        AccountRiskNote? note = null) =>
        new(true, string.Empty, operationId, alreadyProcessed, status, note);

    public static AccountRiskOperationResult Fail(string error) =>
        new(false, error, Guid.Empty, false, null, null);
}

public interface ILiveOpsAccountRiskService
{
    Task<int> RefreshAsync(CancellationToken cancellationToken);
    Task<AccountRiskPage> SearchAsync(AccountRiskSearch search, CancellationToken cancellationToken);
    Task<AccountRiskDetails?> GetDetailsAsync(Guid accountId, int transferLimit, CancellationToken cancellationToken);
    Task<AccountRiskOperationResult> UpdateStatusAsync(Guid operationId, Guid accountId, AccountInvestigationStatus status, AdministrationActor actor, string reason, CancellationToken cancellationToken);
    Task<AccountRiskOperationResult> AddNoteAsync(Guid operationId, Guid accountId, AdministrationActor actor, string note, CancellationToken cancellationToken);
}
