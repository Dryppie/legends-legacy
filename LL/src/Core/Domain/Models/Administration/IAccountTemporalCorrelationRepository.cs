namespace Domain.Models.Administration;

public interface IAccountTemporalCorrelationRepository
{
    Task<AccountTemporalCorrelationDataset?> GetDatasetAsync(
        Guid subjectAccountId,
        DateTimeOffset windowStart,
        DateTimeOffset evaluatedAt,
        int relatedAccountLimit,
        int maximumTokenRows,
        int maximumTransferRows,
        CancellationToken cancellationToken);
}
