using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace API.LiveOps.Support;

public sealed record TransferConversationCorrelationEntryDto(
    Guid CounterpartyAccountId,
    Guid CounterpartyCharacterId,
    string CounterpartyCharacterName,
    string Assessment,
    bool MeetsPatternThreshold,
    string Explanation,
    int TransferCount,
    int IncomingTransferCount,
    int OutgoingTransferCount,
    long CinderValue,
    long IncomingCinders,
    long OutgoingCinders,
    int ItemTransferCount,
    int EstablishedConversationCount,
    int OneWayConversationCount,
    int SharedChannelActivityCount,
    int NoRecordedConversationCount,
    int ImmediateMessageCount,
    DateTimeOffset FirstTransferAt,
    DateTimeOffset LastTransferAt,
    IReadOnlyList<Guid> SupportingTransferIds);

public sealed record TransferConversationCorrelationReportDto(
    Guid AccountId,
    DateTimeOffset WindowStart,
    DateTimeOffset EvaluatedAt,
    bool EvidenceComplete,
    int AnalyzedTransferCount,
    int UnavailableConversationCount,
    IReadOnlyList<TransferConversationCorrelationEntryDto> Entries);

public sealed record TransferConversationCorrelationLookupResult(
    bool AccountFound,
    TransferConversationCorrelationReportDto? Report);

public sealed class TransferConversationCorrelationService(
    IDbContextFactory<LLDbContext> contextFactory,
    IChatModerationGateway chat,
    IOptions<LiveOpsOptions> configuredOptions,
    TimeProvider timeProvider)
{
    private readonly LiveOpsOptions _options = configuredOptions.Value;

    public async Task<TransferConversationCorrelationLookupResult> GetAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var windowStart = now.AddDays(-Math.Clamp(
            _options.TransferConversationRelationshipDays,
            1,
            180));
        var maximumRows = Math.Clamp(
            _options.MaximumTransferConversationCorrelationRows,
            25,
            2_000);
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await database.Users.AsNoTracking()
                .AnyAsync(user => user.Id == accountId, cancellationToken))
        {
            return new TransferConversationCorrelationLookupResult(false, null);
        }

        var query = database.PlayerTransferHistory.AsNoTracking()
            .Where(transfer =>
                transfer.OccurredAt >= windowStart &&
                transfer.SenderAccountId != transfer.RecipientAccountId &&
                (transfer.SenderAccountId == accountId ||
                 transfer.RecipientAccountId == accountId));
        var total = await query.CountAsync(cancellationToken);
        var transfers = await query
            .OrderByDescending(transfer => transfer.OccurredAt)
            .ThenByDescending(transfer => transfer.Id)
            .Take(maximumRows)
            .Select(transfer => new CorrelationTransferRow(
                transfer.Id,
                transfer.Kind,
                transfer.SenderAccountId,
                transfer.SenderCharacterId,
                transfer.SenderCharacterName,
                transfer.RecipientAccountId,
                transfer.RecipientCharacterId,
                transfer.RecipientCharacterName,
                transfer.Quantity,
                transfer.OccurredAt))
            .ToListAsync(cancellationToken);

        var evidence = new Dictionary<Guid, ChatConversationEvidenceGatewayEntry>();
        var unavailable = 0;
        foreach (var batch in transfers.Chunk(25))
        {
            var result = await chat.GetConversationEvidenceAsync(
                batch.Select(transfer => new ChatConversationEvidenceGatewayQuery(
                    transfer.Id,
                    transfer.SenderCharacterId,
                    transfer.RecipientCharacterId,
                    transfer.OccurredAt.AddDays(-Math.Clamp(
                        _options.TransferConversationLookbackDays,
                        1,
                        90)),
                    transfer.OccurredAt.AddHours(Math.Clamp(
                        _options.TransferConversationAfterHours,
                        1,
                        24)),
                    transfer.OccurredAt,
                    transfer.OccurredAt.AddHours(-Math.Clamp(
                        _options.TransferConversationImmediateBeforeHours,
                        1,
                        72)),
                    transfer.OccurredAt.AddHours(Math.Clamp(
                        _options.TransferConversationAfterHours,
                        1,
                        24)),
                    null,
                    0)).ToList(),
                cancellationToken);
            if (!result.IsSuccess)
            {
                unavailable += batch.Length;
                continue;
            }
            foreach (var entry in result.Evidence)
            {
                evidence[entry.EvidenceId] = entry;
            }
            unavailable += batch.Count(transfer => !evidence.ContainsKey(transfer.Id));
        }

        var minimumTransfers = Math.Max(1, _options.UncommunicativeMinimumTransferCount);
        var minimumCinders = Math.Max(1, _options.UncommunicativeMinimumCinders);
        var minimumItems = Math.Max(1, _options.UncommunicativeMinimumItemTransferCount);
        var entries = transfers
            .GroupBy(transfer => CounterpartyAccountId(transfer, accountId))
            .Select(group =>
            {
                var rows = group.OrderBy(transfer => transfer.OccurredAt).ToList();
                var available = rows
                    .Where(transfer => evidence.ContainsKey(transfer.Id))
                    .Select(transfer => evidence[transfer.Id])
                    .ToList();
                var established = available.Count(item =>
                    item.FirstToSecondMessageCount > 0 &&
                    item.SecondToFirstMessageCount > 0);
                var oneWay = available.Count(item =>
                    (item.FirstToSecondMessageCount > 0) !=
                    (item.SecondToFirstMessageCount > 0));
                var shared = available.Count(item =>
                    item.FirstToSecondMessageCount == 0 &&
                    item.SecondToFirstMessageCount == 0 &&
                    item.SharedChannelCount > 0);
                var none = available.Count(item =>
                    item.FirstToSecondMessageCount == 0 &&
                    item.SecondToFirstMessageCount == 0 &&
                    item.SharedChannelCount == 0);
                var cinders = rows
                    .Where(transfer => transfer.Kind == PlayerTransferKind.Cinders)
                    .Sum(transfer => transfer.Quantity);
                var incoming = rows.Count(transfer =>
                    transfer.RecipientAccountId == accountId);
                var outgoing = rows.Count(transfer =>
                    transfer.SenderAccountId == accountId);
                var incomingCinders = rows
                    .Where(transfer =>
                        transfer.Kind == PlayerTransferKind.Cinders &&
                        transfer.RecipientAccountId == accountId)
                    .Sum(transfer => transfer.Quantity);
                var outgoingCinders = rows
                    .Where(transfer =>
                        transfer.Kind == PlayerTransferKind.Cinders &&
                        transfer.SenderAccountId == accountId)
                    .Sum(transfer => transfer.Quantity);
                var itemCount = rows.Count(transfer =>
                    transfer.Kind == PlayerTransferKind.InventoryItem);
                var complete = available.Count == rows.Count;
                var material = cinders >= minimumCinders || itemCount >= minimumItems;
                var meets = complete &&
                            rows.Count >= minimumTransfers &&
                            established == 0 &&
                            material;
                var newest = rows[^1];
                var assessment = !complete
                    ? "ChatUnavailable"
                    : established > 0
                        ? "RecordedBidirectionalConversation"
                        : meets
                            ? "UncommunicativeValueTransferPattern"
                            : "BelowPatternThreshold";
                var explanation = !complete
                    ? $"Chat evidence was unavailable for {rows.Count - available.Count} of {rows.Count} transfer(s); absence of conversation was not inferred."
                    : established > 0
                        ? $"Bidirectional direct conversation was recorded around {established} of {rows.Count} transfer(s)."
                        : meets
                            ? $"{incoming} incoming and {outgoing} outgoing transfer(s) totaling {cinders:N0} Cinders and {itemCount} item transfer(s) had no bidirectional in-game conversation in the configured windows."
                            : $"{incoming} incoming and {outgoing} outgoing transfer(s), {cinders:N0} Cinders, and {itemCount} item transfer(s) did not meet the configured uncommunicative-transfer threshold.";
                return new TransferConversationCorrelationEntryDto(
                    group.Key,
                    CounterpartyCharacterId(newest, accountId),
                    CounterpartyCharacterName(newest, accountId),
                    assessment,
                    meets,
                    explanation,
                    rows.Count,
                    incoming,
                    outgoing,
                    cinders,
                    incomingCinders,
                    outgoingCinders,
                    itemCount,
                    established,
                    oneWay,
                    shared,
                    none,
                    available.Sum(item => item.ImmediateMessageCount),
                    rows[0].OccurredAt,
                    newest.OccurredAt,
                    rows.Select(transfer => transfer.Id).Take(100).ToList());
            })
            .OrderByDescending(entry => entry.MeetsPatternThreshold)
            .ThenByDescending(entry => entry.CinderValue)
            .ThenByDescending(entry => entry.TransferCount)
            .ToList();

        return new TransferConversationCorrelationLookupResult(
            true,
            new TransferConversationCorrelationReportDto(
                accountId,
                windowStart,
                now,
                total <= maximumRows && unavailable == 0,
                transfers.Count,
                unavailable,
                entries));
    }

    private static Guid CounterpartyAccountId(
        CorrelationTransferRow transfer,
        Guid accountId) =>
        transfer.SenderAccountId == accountId &&
        transfer.RecipientAccountId != accountId
            ? transfer.RecipientAccountId
            : transfer.SenderAccountId;

    private static Guid CounterpartyCharacterId(
        CorrelationTransferRow transfer,
        Guid accountId) =>
        transfer.SenderAccountId == accountId &&
        transfer.RecipientAccountId != accountId
            ? transfer.RecipientCharacterId
            : transfer.SenderCharacterId;

    private static string CounterpartyCharacterName(
        CorrelationTransferRow transfer,
        Guid accountId) =>
        transfer.SenderAccountId == accountId &&
        transfer.RecipientAccountId != accountId
            ? transfer.RecipientCharacterName
            : transfer.SenderCharacterName;

    private sealed record CorrelationTransferRow(
        Guid Id,
        PlayerTransferKind Kind,
        Guid SenderAccountId,
        Guid SenderCharacterId,
        string SenderCharacterName,
        Guid RecipientAccountId,
        Guid RecipientCharacterId,
        string RecipientCharacterName,
        long Quantity,
        DateTimeOffset OccurredAt);
}
