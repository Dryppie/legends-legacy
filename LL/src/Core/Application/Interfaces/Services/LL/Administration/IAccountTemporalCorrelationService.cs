using Domain.Models.Administration;

namespace Application.Interfaces.Services.LL.Administration;

public interface IAccountTemporalCorrelationService
{
    Task<AccountTemporalCorrelationReport?> AnalyzeAsync(
        Guid accountId,
        int? windowDays,
        CancellationToken cancellationToken);
}
