using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Administration;
using Microsoft.Extensions.Options;

namespace Services.LL.Administration;

public sealed class AccountTemporalCorrelationService(
    IAccountTemporalCorrelationRepository repository,
    IOptions<AccountTemporalCorrelationOptions> configuredOptions,
    TimeProvider timeProvider) : IAccountTemporalCorrelationService
{
    private readonly AccountTemporalCorrelationOptions _options = configuredOptions.Value;

    public async Task<AccountTemporalCorrelationReport?> AnalyzeAsync(
        Guid accountId,
        int? windowDays,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var days = Math.Clamp(
            windowDays ?? _options.DefaultWindowDays,
            7,
            _options.MaximumWindowDays);
        var dataset = await repository.GetDatasetAsync(
            accountId,
            now.AddDays(-days),
            now,
            _options.RelatedAccountLimit,
            _options.MaximumTokenRows,
            _options.MaximumTransferRows,
            cancellationToken);
        if (dataset is null) return null;

        var entries = new AccountTemporalCorrelationEvaluator(_options.ToPolicy()).Evaluate(dataset);
        return new AccountTemporalCorrelationReport(
            accountId,
            dataset.WindowStart,
            dataset.EvaluatedAt,
            dataset.EvidenceComplete,
            dataset.AnalyzedTokenCount,
            dataset.AnalyzedTransferCount,
            _options.AnalysisVersion,
            entries);
    }
}
