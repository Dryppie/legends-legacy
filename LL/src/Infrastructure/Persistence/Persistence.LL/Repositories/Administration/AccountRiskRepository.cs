using System.Text.Json;
using Application.Common.Interfaces;
using Domain.Models.Administration;
using Domain.Models.Economy;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Administration;

public sealed class AccountRiskRepository(
    IDbContext context,
    JsonSerializerOptions jsonOptions) : IAccountRiskRepository
{
    private const long EvaluationLockId = 5_468_932_011;

    public async Task AcquireEvaluationLockAsync(CancellationToken cancellationToken)
    {
        await context.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            cancellationToken,
            EvaluationLockId);
    }

    public Task<bool> HasFreshEvaluationAsync(
        int evaluationVersion,
        DateTimeOffset evaluatedAfter,
        CancellationToken cancellationToken) =>
        context.AccountRiskSnapshots.AsNoTracking()
            .AnyAsync(x => x.EvaluationVersion == evaluationVersion && x.EvaluatedAt >= evaluatedAfter, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetCandidateAccountIdsAsync(
        DateTimeOffset since,
        int limit,
        CancellationToken cancellationToken)
    {
        var transfers = context.EconomyLedger.AsNoTracking()
            .Where(x => x.EventType == EconomyEventType.DirectCurrencyTransfer &&
                        x.AssetId == "currency:cinders" &&
                        x.SenderAccountId.HasValue &&
                        x.RecipientAccountId.HasValue &&
                        x.SenderAccountId != x.RecipientAccountId &&
                        x.OccurredAt >= since);
        var take = Math.Clamp(limit, 1, 5_000);
        var senders = await transfers
            .GroupBy(x => x.SenderAccountId!.Value)
            .Select(x => new { AccountId = x.Key, LastActivity = x.Max(y => y.OccurredAt) })
            .OrderByDescending(x => x.LastActivity)
            .Take(take)
            .ToListAsync(cancellationToken);
        var recipients = await transfers
            .GroupBy(x => x.RecipientAccountId!.Value)
            .Select(x => new { AccountId = x.Key, LastActivity = x.Max(y => y.OccurredAt) })
            .OrderByDescending(x => x.LastActivity)
            .Take(take)
            .ToListAsync(cancellationToken);

        var active = senders.Concat(recipients)
            .GroupBy(x => x.AccountId)
            .Select(x => new { AccountId = x.Key, LastActivity = x.Max(y => y.LastActivity) })
            .OrderByDescending(x => x.LastActivity)
            .ThenBy(x => x.AccountId)
            .Take(take)
            .Select(x => x.AccountId)
            .ToList();
        var expired = await context.AccountRiskSnapshots.AsNoTracking()
            .Where(x => x.Severity != AccountRiskSeverity.Low &&
                        (!x.LastTriggeredAt.HasValue || x.LastTriggeredAt < since))
            .OrderBy(x => x.EvaluatedAt)
            .Take(Math.Max(1, take / 10))
            .Select(x => x.AccountId)
            .ToListAsync(cancellationToken);

        // The maintenance slice clears flags after their last evidence ages out of
        // the lookback without making the foreground queue execute analytics.
        return active.Concat(expired).Distinct().ToList();
    }

    public async Task<AccountRiskAnalysisDataset> GetAnalysisDatasetAsync(
        IReadOnlyCollection<Guid> accountIds,
        DateTimeOffset since,
        int maximumTransfers,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
        {
            return new AccountRiskAnalysisDataset(
                new Dictionary<Guid, AccountRiskAccountFact>(),
                []);
        }

        var candidates = accountIds.Distinct().ToArray();
        var take = Math.Clamp(maximumTransfers, 100, 250_000);
        var baseQuery = context.EconomyLedger.AsNoTracking()
            .Where(x => x.EventType == EconomyEventType.DirectCurrencyTransfer &&
                        x.AssetId == "currency:cinders" &&
                        x.SenderAccountId.HasValue &&
                        x.RecipientAccountId.HasValue &&
                        x.SenderAccountId != x.RecipientAccountId &&
                        x.Quantity > 0 &&
                        x.OccurredAt >= since);

        var firstHop = await baseQuery
            .Where(x => candidates.Contains(x.SenderAccountId!.Value) ||
                        candidates.Contains(x.RecipientAccountId!.Value))
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .Select(x => new AccountRiskTransferFact(
                x.Id,
                x.SenderAccountId!.Value,
                x.RecipientAccountId!.Value,
                x.TotalValue ?? x.Quantity,
                x.OccurredAt,
                x.SenderAccountCreatedUtc,
                x.SenderCharacterLevel,
                x.RecipientAccountCreatedUtc,
                x.RecipientCharacterLevel))
            .ToListAsync(cancellationToken);

        var relatedIds = firstHop
            .SelectMany(x => new[] { x.SenderAccountId, x.RecipientAccountId })
            .Distinct()
            .ToArray();
        var secondHop = relatedIds.Length == 0
            ? []
            : await baseQuery
                .Where(x => relatedIds.Contains(x.SenderAccountId!.Value))
                .OrderByDescending(x => x.OccurredAt)
                .Take(take)
                .Select(x => new AccountRiskTransferFact(
                    x.Id,
                    x.SenderAccountId!.Value,
                    x.RecipientAccountId!.Value,
                    x.TotalValue ?? x.Quantity,
                    x.OccurredAt,
                    x.SenderAccountCreatedUtc,
                    x.SenderCharacterLevel,
                    x.RecipientAccountCreatedUtc,
                    x.RecipientCharacterLevel))
                .ToListAsync(cancellationToken);

        var transfers = firstHop.Concat(secondHop)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();
        var involvedAccountIds = transfers
            .SelectMany(x => new[] { x.SenderAccountId, x.RecipientAccountId })
            .Concat(candidates)
            .Distinct()
            .ToArray();

        var users = await context.Users.AsNoTracking()
            .Where(x => involvedAccountIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Username, x.CreatedUtc })
            .ToListAsync(cancellationToken);
        var characters = await context.Characters.AsNoTracking()
            .Where(x => involvedAccountIds.Contains(x.UserId))
            .Select(x => new { x.UserId, x.Id, x.Name, x.Level })
            .ToListAsync(cancellationToken);
        var lastSessions = await context.RefreshTokens.AsNoTracking()
            .Where(x => involvedAccountIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new { AccountId = x.Key, Last = x.Max(y => y.CreatedUtc) })
            .ToDictionaryAsync(x => x.AccountId, x => (DateTimeOffset?)new DateTimeOffset(x.Last, TimeSpan.Zero), cancellationToken);

        var characterByAccount = characters
            .GroupBy(x => x.UserId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Level).ThenBy(y => y.Id).First());
        var facts = new Dictionary<Guid, AccountRiskAccountFact>();
        foreach (var user in users)
        {
            if (!characterByAccount.TryGetValue(user.Id, out var character)) continue;
            facts[user.Id] = new AccountRiskAccountFact(
                user.Id,
                character.Id,
                user.Username,
                character.Name,
                character.Level,
                DateTime.SpecifyKind(user.CreatedUtc, DateTimeKind.Utc),
                lastSessions.GetValueOrDefault(user.Id));
        }

        return new AccountRiskAnalysisDataset(facts, transfers);
    }

    public async Task UpsertEvaluationsAsync(
        IReadOnlyCollection<AccountRiskEvaluation> evaluations,
        DateTimeOffset evaluatedAt,
        int evaluationVersion,
        int historyMinimumScoreChange,
        CancellationToken cancellationToken)
    {
        var ids = evaluations.Select(x => x.AccountId).ToArray();
        var existing = await context.AccountRiskSnapshots
            .Where(x => ids.Contains(x.AccountId))
            .ToDictionaryAsync(x => x.AccountId, cancellationToken);

        foreach (var evaluation in evaluations)
        {
            existing.TryGetValue(evaluation.AccountId, out var snapshot);
            var previousScore = snapshot?.Score;
            var previousSeverity = snapshot?.Severity;
            var previousEvaluationVersion = snapshot?.EvaluationVersion;
            var signalsJson = JsonSerializer.Serialize(evaluation.Signals, jsonOptions);
            var relationshipsJson = JsonSerializer.Serialize(evaluation.Relationships, jsonOptions);
            var primary = evaluation.Signals.FirstOrDefault();

            if (snapshot is null)
            {
                snapshot = new AccountRiskSnapshot { AccountId = evaluation.AccountId };
                context.AccountRiskSnapshots.Add(snapshot);
            }

            snapshot.CharacterId = evaluation.CharacterId;
            snapshot.AccountLabel = evaluation.AccountLabel;
            snapshot.CharacterName = evaluation.CharacterName;
            snapshot.CharacterLevel = evaluation.CharacterLevel;
            snapshot.AccountCreatedUtc = evaluation.AccountCreatedUtc;
            snapshot.LastSessionUtc = evaluation.LastSessionUtc;
            snapshot.Score = evaluation.Score;
            snapshot.Severity = evaluation.Severity;
            snapshot.PrimarySignalType = primary?.Type;
            snapshot.PrimaryReason = primary?.Explanation ?? "No material cinder-flow anomaly was detected in the configured lookback.";
            snapshot.ConnectedAccountCount = evaluation.Relationships.Count;
            snapshot.IncomingCinders = evaluation.IncomingCinders;
            snapshot.OutgoingCinders = evaluation.OutgoingCinders;
            snapshot.TransferCount = evaluation.TransferCount;
            snapshot.FirstFlaggedAt ??= evaluation.Severity >= AccountRiskSeverity.Moderate ? evaluatedAt : null;
            snapshot.LastTriggeredAt = evaluation.LastTriggeredAt;
            snapshot.EvaluatedAt = evaluatedAt;
            snapshot.EvaluationVersion = evaluationVersion;
            snapshot.SignalsJson = signalsJson;
            snapshot.RelationshipsJson = relationshipsJson;

            if (!previousScore.HasValue || previousSeverity != evaluation.Severity ||
                previousEvaluationVersion != evaluationVersion ||
                Math.Abs(previousScore.Value - evaluation.Score) >= Math.Max(1, historyMinimumScoreChange))
            {
                context.AccountRiskHistory.Add(new AccountRiskHistory
                {
                    AccountId = evaluation.AccountId,
                    Score = evaluation.Score,
                    Severity = evaluation.Severity,
                    SignalsJson = signalsJson,
                    EvaluatedAt = evaluatedAt,
                    EvaluationVersion = evaluationVersion
                });
            }
        }
    }

    public async Task<AccountRiskPage> SearchAsync(
        AccountRiskSearch search,
        CancellationToken cancellationToken)
    {
        var snapshots = context.AccountRiskSnapshots.AsNoTracking().AsQueryable();
        if (search.MinimumSeverity.HasValue)
        {
            var allowedSeverities = Enum.GetValues<AccountRiskSeverity>()
                .Where(x => x >= search.MinimumSeverity.Value)
                .ToArray();
            snapshots = snapshots.Where(x => allowedSeverities.Contains(x.Severity));
        }
        if (search.SignalType.HasValue) snapshots = snapshots.Where(x => x.PrimarySignalType == search.SignalType.Value);
        if (search.MinimumScore.HasValue) snapshots = snapshots.Where(x => x.Score >= search.MinimumScore.Value);
        if (search.MaximumAccountAgeDays.HasValue)
        {
            var createdAfter = DateTime.UtcNow.AddDays(-Math.Max(0, search.MaximumAccountAgeDays.Value));
            snapshots = snapshots.Where(x => x.AccountCreatedUtc >= createdAfter);
        }
        if (search.LastTriggeredAfter.HasValue) snapshots = snapshots.Where(x => x.LastTriggeredAt >= search.LastTriggeredAfter.Value);
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var term = search.Search.Trim().ToUpper();
            if (Guid.TryParse(term, out var id))
            {
                snapshots = snapshots.Where(x => x.AccountId == id || x.CharacterId == id);
            }
            else
            {
                snapshots = snapshots.Where(x => x.AccountLabel.ToUpper().Contains(term) || x.CharacterName.ToUpper().Contains(term));
            }
        }

        var joined = from snapshot in snapshots
                     join investigation in context.AccountRiskInvestigations.AsNoTracking()
                         on snapshot.AccountId equals investigation.AccountId into investigationRows
                     from investigation in investigationRows.DefaultIfEmpty()
                     select new { Snapshot = snapshot, Status = investigation == null ? AccountInvestigationStatus.Unreviewed : investigation.Status };
        if (search.Status.HasValue) joined = joined.Where(x => x.Status == search.Status.Value);

        joined = search.Sort.ToLowerInvariant() switch
        {
            "recent" => joined.OrderByDescending(x => x.Snapshot.LastTriggeredAt).ThenByDescending(x => x.Snapshot.Score),
            "volume" => joined.OrderByDescending(x => x.Snapshot.IncomingCinders + x.Snapshot.OutgoingCinders).ThenByDescending(x => x.Snapshot.Score),
            "connected" => joined.OrderByDescending(x => x.Snapshot.ConnectedAccountCount).ThenByDescending(x => x.Snapshot.Score),
            "newest" => joined.OrderByDescending(x => x.Snapshot.FirstFlaggedAt).ThenByDescending(x => x.Snapshot.Score),
            _ => joined.OrderByDescending(x => x.Snapshot.Score).ThenByDescending(x => x.Snapshot.LastTriggeredAt)
        };

        var total = await joined.CountAsync(cancellationToken);
        var entries = await joined
            .Skip((Math.Max(1, search.Page) - 1) * Math.Clamp(search.PageSize, 1, 100))
            .Take(Math.Clamp(search.PageSize, 1, 100))
            .ToListAsync(cancellationToken);
        var counts = await context.AccountRiskSnapshots.AsNoTracking()
            .GroupBy(x => x.Severity)
            .Select(x => new { Severity = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count, cancellationToken);
        var lastEvaluated = await context.AccountRiskSnapshots.AsNoTracking()
            .MaxAsync(x => (DateTimeOffset?)x.EvaluatedAt, cancellationToken);
        var directEvidence = context.EconomyLedger.AsNoTracking()
            .Where(x => x.EventType == EconomyEventType.DirectCurrencyTransfer &&
                        x.AssetId == "currency:cinders" &&
                        x.SenderAccountId.HasValue &&
                        x.RecipientAccountId.HasValue);
        var firstEvidence = await directEvidence.MinAsync(x => (DateTimeOffset?)x.OccurredAt, cancellationToken);
        var directTransferCount = await directEvidence.LongCountAsync(cancellationToken);
        return new AccountRiskPage(
            entries.Select(x => new AccountRiskSnapshotView(x.Snapshot, x.Status)).ToList(),
            total,
            counts,
            lastEvaluated,
            firstEvidence,
            directTransferCount,
            counts.Values.Sum());
    }

    public async Task<AccountRiskDetails?> GetDetailsAsync(
        Guid accountId,
        int transferLimit,
        CancellationToken cancellationToken)
    {
        var snapshot = await context.AccountRiskSnapshots.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
        if (snapshot is null) return null;
        var status = await context.AccountRiskInvestigations.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => (AccountInvestigationStatus?)x.Status)
            .SingleOrDefaultAsync(cancellationToken) ?? AccountInvestigationStatus.Unreviewed;
        var signals = Deserialize<AccountRiskSignal>(snapshot.SignalsJson);
        var relationships = Deserialize<AccountRiskRelationship>(snapshot.RelationshipsJson).ToList();
        var relatedIds = relationships.Select(x => x.AccountId).ToArray();
        var relatedRisk = await context.AccountRiskSnapshots.AsNoTracking()
            .Where(x => relatedIds.Contains(x.AccountId))
            .Select(x => new { x.AccountId, x.Score, x.Severity })
            .ToDictionaryAsync(x => x.AccountId, cancellationToken);
        relationships = relationships.Select(x => relatedRisk.TryGetValue(x.AccountId, out var risk)
                ? x with { RiskScore = risk.Score, RiskSeverity = risk.Severity }
                : x)
            .ToList();

        var transfers = await context.PlayerTransferHistory.AsNoTracking()
            .Where(x => x.SenderAccountId == accountId || x.RecipientAccountId == accountId)
            .OrderByDescending(x => x.OccurredAt)
            .Take(Math.Clamp(transferLimit, 1, 500))
            .Select(x => new AccountRiskTransferEvidence(
                x.Id,
                x.RecipientAccountId == accountId ? "Incoming" : "Outgoing",
                x.Kind.ToString(),
                x.RecipientAccountId == accountId ? x.SenderAccountId : x.RecipientAccountId,
                x.RecipientAccountId == accountId ? x.SenderCharacterId : x.RecipientCharacterId,
                x.RecipientAccountId == accountId ? x.SenderCharacterName : x.RecipientCharacterName,
                x.AssetId,
                x.AssetName,
                x.Quantity,
                x.OccurredAt))
            .ToListAsync(cancellationToken);
        var history = await context.AccountRiskHistory.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.EvaluatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        var notes = await context.AccountRiskNotes.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return new AccountRiskDetails(snapshot, status, signals, relationships, transfers, history, notes);
    }

    public Task<AccountRiskInvestigation?> GetInvestigationAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.AccountRiskInvestigations.SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

    public Task<AccountRiskSnapshot?> GetSnapshotAsync(Guid accountId, CancellationToken cancellationToken) =>
        context.AccountRiskSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);

    public void AddInvestigation(AccountRiskInvestigation investigation) => context.AccountRiskInvestigations.Add(investigation);
    public void AddNote(AccountRiskNote note) => context.AccountRiskNotes.Add(note);
    public void AddAdminAction(AdminAction action) => context.AdminActions.Add(action);

    private IReadOnlyList<T> Deserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, jsonOptions) ?? []; }
        catch (JsonException) { return []; }
    }
}
