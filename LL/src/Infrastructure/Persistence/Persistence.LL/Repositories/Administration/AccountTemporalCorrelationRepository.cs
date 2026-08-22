using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;

public sealed class AccountTemporalCorrelationRepository(
    IDbContext context,
    JsonSerializerOptions jsonOptions) : IAccountTemporalCorrelationRepository
{
    private const int TokenChainPredecessorBufferDays = 31;

    public async Task<AccountTemporalCorrelationDataset?> GetDatasetAsync(
        Guid subjectAccountId,
        DateTimeOffset windowStart,
        DateTimeOffset evaluatedAt,
        int relatedAccountLimit,
        int maximumTokenRows,
        int maximumTransferRows,
        CancellationToken cancellationToken)
    {
        var snapshot = await context.AccountRiskSnapshots.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == subjectAccountId, cancellationToken);
        if (snapshot is null) return null;

        var relationships = DeserializeRelationships(snapshot.RelationshipsJson)
            .Where(x => x.AccountId != subjectAccountId)
            .OrderByDescending(x => x.TransactionCount)
            .ThenBy(x => x.AccountId)
            .Take(Math.Clamp(relatedAccountLimit, 1, 50))
            .ToList();
        var relatedIds = relationships.Select(x => x.AccountId).Distinct().ToArray();
        var accountIds = relatedIds.Append(subjectAccountId).Distinct().ToArray();
        var accounts = relationships
            .GroupBy(x => x.AccountId)
            .Select(x => x.First())
            .ToDictionary(
                x => x.AccountId,
                x => new AccountTemporalCorrelationAccountFact(x.AccountId, x.CharacterId, x.CharacterName));
        accounts[subjectAccountId] = new AccountTemporalCorrelationAccountFact(
            subjectAccountId,
            snapshot.CharacterId,
            snapshot.CharacterName);

        var tokenLimit = Math.Clamp(maximumTokenRows, 100, 100_000);
        var tokenBufferStart = windowStart.AddDays(-TokenChainPredecessorBufferDays).UtcDateTime;
        var tokenRows = await context.RefreshTokens.AsNoTracking()
            .Where(x => accountIds.Contains(x.UserId) && x.CreatedUtc >= tokenBufferStart)
            .OrderByDescending(x => x.CreatedUtc)
            .ThenByDescending(x => x.Id)
            .Take(tokenLimit + 1)
            .Select(x => new TokenRow(
                x.Id,
                x.UserId,
                x.TokenHash,
                x.ReplacedBy,
                x.CreatedUtc,
                x.ExpiresUtc,
                x.RevokedUtc))
            .ToListAsync(cancellationToken);
        var tokenEvidenceComplete = tokenRows.Count <= tokenLimit;
        tokenRows = tokenRows.Take(tokenLimit).ToList();
        var tokenIdsByHash = tokenRows
            .GroupBy(x => x.TokenHash, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.Ordinal);
        var tokens = tokenRows.Select(x => new AccountTemporalTokenFact(
                x.Id,
                x.AccountId,
                Utc(x.CreatedUtc),
                Utc(x.ExpiresUtc),
                x.RevokedUtc.HasValue ? Utc(x.RevokedUtc.Value) : null,
                x.ReplacedBy is not null && tokenIdsByHash.TryGetValue(x.ReplacedBy, out var replacementId)
                    ? replacementId
                    : null))
            .ToList();

        var transferLimit = Math.Clamp(maximumTransferRows, 100, 25_000);
        var transferStart = windowStart;
        var transferRows = relatedIds.Length == 0
            ? []
            : await context.PlayerTransferHistory.AsNoTracking()
                .Where(x => x.OccurredAt >= transferStart &&
                    ((x.SenderAccountId == subjectAccountId && relatedIds.Contains(x.RecipientAccountId)) ||
                     (x.RecipientAccountId == subjectAccountId && relatedIds.Contains(x.SenderAccountId))))
                .OrderByDescending(x => x.OccurredAt)
                .Take(transferLimit + 1)
                .Select(x => new AccountTemporalTransferFact(
                    x.Id,
                    x.SenderAccountId,
                    x.RecipientAccountId,
                    x.OccurredAt))
                .ToListAsync(cancellationToken);
        var transferEvidenceComplete = transferRows.Count <= transferLimit;
        var transfers = transferRows.Take(transferLimit).ToList();

        return new AccountTemporalCorrelationDataset(
            subjectAccountId,
            accounts,
            relatedIds,
            tokens,
            transfers,
            windowStart,
            evaluatedAt,
            tokenEvidenceComplete && transferEvidenceComplete,
            tokenRows.Count(x => Utc(x.CreatedUtc) >= windowStart),
            transfers.Count);
    }

    private IReadOnlyList<AccountRiskRelationship> DeserializeRelationships(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<AccountRiskRelationship>>(json, jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static DateTimeOffset Utc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);

    private sealed record TokenRow(
        long Id,
        Guid AccountId,
        string TokenHash,
        string? ReplacedBy,
        DateTime CreatedUtc,
        DateTime ExpiresUtc,
        DateTime? RevokedUtc);
}
