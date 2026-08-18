using System.Text.Json.Serialization;

namespace Domain.Models.Administration;

public enum AccountRiskSeverity
{
    Low,
    Moderate,
    High,
    Critical
}

public enum AccountRiskSignalType
{
    IncomingConcentration,
    OneSidedRelationship,
    OneSidedItemTransfer,
    IncomingItemFunnel,
    ItemQuantityConsolidation,
    YoungItemSourceNetwork,
    YoungItemCoordinationNetwork,
    FeederNetwork,
    YoungAccountOutflow,
    EphemeralItemOutflow,
    CircularTransfer
}

public enum AccountInvestigationStatus
{
    Unreviewed,
    Investigating,
    Watchlisted,
    Cleared,
    ConfirmedAbuse,
    Actioned
}

/// <summary>
/// Precomputed, replaceable risk summary. It is an investigative lead and never an
/// enforcement decision. Raw economy records remain the source of truth.
/// </summary>
public sealed class AccountRiskSnapshot
{
    public Guid AccountId { get; set; }
    public Guid CharacterId { get; set; }
    public string AccountLabel { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public int CharacterLevel { get; set; }
    public DateTime AccountCreatedUtc { get; set; }
    public DateTimeOffset? LastSessionUtc { get; set; }
    public int Score { get; set; }
    public AccountRiskSeverity Severity { get; set; }
    public AccountRiskSignalType? PrimarySignalType { get; set; }
    public string PrimaryReason { get; set; } = string.Empty;
    public int ConnectedAccountCount { get; set; }
    public long IncomingCinders { get; set; }
    public long OutgoingCinders { get; set; }
    public int TransferCount { get; set; }
    public DateTimeOffset? FirstFlaggedAt { get; set; }
    public DateTimeOffset? LastTriggeredAt { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
    public int EvaluationVersion { get; set; } = 1;
    public DateTimeOffset AnalysisWindowStart { get; set; }
    public bool EvidenceComplete { get; set; } = true;
    public int AnalyzedTransferCount { get; set; }
    public string SignalsJson { get; set; } = "[]";
    public string RelationshipsJson { get; set; } = "[]";
}

public sealed class AccountRiskHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public int Score { get; set; }
    public AccountRiskSeverity Severity { get; set; }
    public string SignalsJson { get; set; } = "[]";
    public DateTimeOffset EvaluatedAt { get; set; }
    public int EvaluationVersion { get; set; } = 1;
    public DateTimeOffset AnalysisWindowStart { get; set; }
    public bool EvidenceComplete { get; set; } = true;
    public int AnalyzedTransferCount { get; set; }
}

public sealed class AccountRiskInvestigation
{
    public Guid AccountId { get; set; }
    public AccountInvestigationStatus Status { get; set; } = AccountInvestigationStatus.Unreviewed;
    public string UpdatedBySubject { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AccountRiskNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string ActorSubject { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record AccountRiskSignal(
    AccountRiskSignalType Type,
    string Category,
    int Contribution,
    string Title,
    string Explanation,
    IReadOnlyDictionary<string, decimal> Evidence,
    IReadOnlyList<Guid>? SupportingTransferIds = null,
    DateTimeOffset? FirstObservedAt = null,
    DateTimeOffset? LastObservedAt = null,
    int SupportingTransferCount = 0,
    bool SupportingEvidenceComplete = true,
    string CorrelationFamily = "direct-transfer");

public sealed record AccountRiskRelationship(
    Guid AccountId,
    Guid CharacterId,
    string CharacterName,
    string Relationship,
    long SentToSubject,
    long ReceivedFromSubject,
    int TransactionCount,
    bool YoungAccount,
    int? RiskScore = null,
    AccountRiskSeverity? RiskSeverity = null,
    int ItemTransfersToSubject = 0,
    int ItemTransfersFromSubject = 0);

public sealed record AccountRiskEvaluation(
    Guid AccountId,
    Guid CharacterId,
    string AccountLabel,
    string CharacterName,
    int CharacterLevel,
    DateTime AccountCreatedUtc,
    DateTimeOffset? LastSessionUtc,
    int Score,
    AccountRiskSeverity Severity,
    IReadOnlyList<AccountRiskSignal> Signals,
    IReadOnlyList<AccountRiskRelationship> Relationships,
    long IncomingCinders,
    long OutgoingCinders,
    int TransferCount,
    DateTimeOffset? LastTriggeredAt,
    DateTimeOffset AnalysisWindowStart,
    bool EvidenceComplete,
    int AnalyzedTransferCount);

public sealed record AccountRiskAccountFact(
    Guid AccountId,
    Guid CharacterId,
    string AccountLabel,
    string CharacterName,
    int CharacterLevel,
    DateTime AccountCreatedUtc,
    DateTimeOffset? LastSessionUtc);

public sealed record AccountRiskTransferFact(
    Guid Id,
    Guid SenderAccountId,
    Guid RecipientAccountId,
    long Amount,
    DateTimeOffset OccurredAt,
    DateTime? SenderAccountCreatedUtc = null,
    int? SenderCharacterLevel = null,
    DateTime? RecipientAccountCreatedUtc = null,
    int? RecipientCharacterLevel = null,
    AccountRiskTransferKind Kind = AccountRiskTransferKind.Cinders,
    string AssetId = "currency:cinders",
    long Quantity = 0)
{
    public long EffectiveQuantity => Quantity > 0 ? Quantity : Amount;
}

public enum AccountRiskTransferKind
{
    Cinders,
    Item
}

public sealed class AccountRiskAnalysisDataset
{
    private readonly IReadOnlyDictionary<Guid, IReadOnlyList<AccountRiskTransferFact>> _outgoing;
    private readonly IReadOnlyDictionary<Guid, IReadOnlyList<AccountRiskTransferFact>> _incoming;

    public AccountRiskAnalysisDataset(
        IReadOnlyDictionary<Guid, AccountRiskAccountFact> accounts,
        IReadOnlyList<AccountRiskTransferFact> transfers,
        DateTimeOffset? analysisWindowStart = null,
        bool evidenceComplete = true)
    {
        Accounts = accounts;
        Transfers = transfers;
        AnalysisWindowStart = analysisWindowStart ?? DateTimeOffset.MinValue;
        EvidenceComplete = evidenceComplete;
        _outgoing = transfers.GroupBy(x => x.SenderAccountId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<AccountRiskTransferFact>)x.ToList());
        _incoming = transfers.GroupBy(x => x.RecipientAccountId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<AccountRiskTransferFact>)x.ToList());
    }

    public IReadOnlyDictionary<Guid, AccountRiskAccountFact> Accounts { get; }
    public IReadOnlyList<AccountRiskTransferFact> Transfers { get; }
    public DateTimeOffset AnalysisWindowStart { get; }
    public bool EvidenceComplete { get; }

    public IReadOnlyList<AccountRiskTransferFact> GetOutgoing(Guid accountId) =>
        _outgoing.GetValueOrDefault(accountId) ?? [];

    public IReadOnlyList<AccountRiskTransferFact> GetIncoming(Guid accountId) =>
        _incoming.GetValueOrDefault(accountId) ?? [];
}

public sealed record AccountRiskPolicy(
    int ModerateScore,
    int HighScore,
    int CriticalScore,
    int MinimumTransferCount,
    int MinimumCounterpartyCount,
    long MinimumRelationshipCinders,
    int MinimumItemTransferCount,
    int MinimumItemFunnelTransferCount,
    int MinimumItemFunnelCounterpartyCount,
    int ItemFunnelFullScaleTransferCount,
    decimal ItemFunnelIncomingShareThreshold,
    int MinimumConsolidatedItemAssetCount,
    long MinimumConsolidatedItemQuantity,
    int MinimumConsolidatedItemTransferCount,
    decimal ConsolidatedItemIncomingShareThreshold,
    int MinimumYoungItemSourceTransferCount,
    int MinimumYoungItemSourceCounterpartyCount,
    int MinimumYoungItemCoordinationTransferCount,
    int MinimumYoungItemCoordinationCounterpartyCount,
    int MinimumMixedDirectionItemTransferCount,
    int ItemTransferSessionWindowMinutes,
    int MinimumItemCoordinationSessionCount,
    decimal ItemCoordinationDominantSessionShareThreshold,
    int MinimumEphemeralItemOutflowTransferCount,
    int MinimumEphemeralItemDistinctAssetCount,
    int EphemeralAccountMaximumSessionSpanHours,
    int EphemeralAccountMinimumDormantDays,
    decimal EphemeralItemTargetShareThreshold,
    long MinimumFeederCinders,
    long MinimumYoungAccountOutflowCinders,
    long MinimumCircularTransferCinders,
    decimal ConcentrationThreshold,
    decimal RelationshipImbalanceThreshold,
    int YoungAccountDays,
    int YoungAccountMaximumLevel,
    decimal FeederTargetShareThreshold,
    int CircularWindowHours,
    decimal CircularValueSimilarity,
    int FlowCategoryCap,
    int CoordinationCategoryCap)
{
    public static AccountRiskPolicy Default { get; } = new(
        ModerateScore: 25,
        HighScore: 50,
        CriticalScore: 75,
        MinimumTransferCount: 2,
        MinimumCounterpartyCount: 2,
        MinimumRelationshipCinders: 10_000,
        MinimumItemTransferCount: 2,
        MinimumItemFunnelTransferCount: 20,
        MinimumItemFunnelCounterpartyCount: 2,
        ItemFunnelFullScaleTransferCount: 150,
        ItemFunnelIncomingShareThreshold: 0.85m,
        MinimumConsolidatedItemAssetCount: 2,
        MinimumConsolidatedItemQuantity: 50,
        MinimumConsolidatedItemTransferCount: 10,
        ConsolidatedItemIncomingShareThreshold: 0.80m,
        MinimumYoungItemSourceTransferCount: 20,
        MinimumYoungItemSourceCounterpartyCount: 2,
        MinimumYoungItemCoordinationTransferCount: 50,
        MinimumYoungItemCoordinationCounterpartyCount: 4,
        MinimumMixedDirectionItemTransferCount: 10,
        ItemTransferSessionWindowMinutes: 5,
        MinimumItemCoordinationSessionCount: 20,
        ItemCoordinationDominantSessionShareThreshold: 0.70m,
        MinimumEphemeralItemOutflowTransferCount: 5,
        MinimumEphemeralItemDistinctAssetCount: 3,
        EphemeralAccountMaximumSessionSpanHours: 24,
        EphemeralAccountMinimumDormantDays: 3,
        EphemeralItemTargetShareThreshold: 0.80m,
        MinimumFeederCinders: 20_000,
        MinimumYoungAccountOutflowCinders: 10_000,
        MinimumCircularTransferCinders: 10_000,
        ConcentrationThreshold: 0.70m,
        RelationshipImbalanceThreshold: 0.85m,
        YoungAccountDays: 14,
        YoungAccountMaximumLevel: 20,
        FeederTargetShareThreshold: 0.80m,
        CircularWindowHours: 48,
        CircularValueSimilarity: 0.50m,
        FlowCategoryCap: 55,
        CoordinationCategoryCap: 25);
}

public sealed class AccountRiskEvaluator(AccountRiskPolicy policy)
{
    public AccountRiskEvaluation Evaluate(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        DateTimeOffset now)
    {
        if (!dataset.Accounts.TryGetValue(accountId, out var account))
        {
            throw new ArgumentException("The account is missing from the analysis dataset.", nameof(accountId));
        }

        var incoming = dataset.GetIncoming(accountId);
        var outgoing = dataset.GetOutgoing(accountId);
        var direct = incoming.Concat(outgoing).ToList();
        var cinderIncoming = incoming.Where(x => x.Kind == AccountRiskTransferKind.Cinders).ToList();
        var cinderOutgoing = outgoing.Where(x => x.Kind == AccountRiskTransferKind.Cinders).ToList();
        var cinderDirect = cinderIncoming.Concat(cinderOutgoing).ToList();
        var itemDirect = direct.Where(x => x.Kind == AccountRiskTransferKind.Item).ToList();
        var incomingTotal = cinderIncoming.Sum(x => x.Amount);
        var outgoingTotal = cinderOutgoing.Sum(x => x.Amount);
        var signals = new List<AccountRiskSignal>();

        EvaluateIncomingConcentration(accountId, cinderIncoming, incomingTotal, signals);
        EvaluateOneSidedRelationships(accountId, cinderDirect, signals);
        EvaluateIncomingItemFunnel(accountId, itemDirect, signals);
        EvaluateItemQuantityConsolidation(accountId, itemDirect, signals);
        EvaluateOneSidedItemTransfers(accountId, itemDirect, signals);
        EvaluateYoungItemSourceNetwork(accountId, dataset, itemDirect, signals);
        EvaluateYoungItemCoordinationNetwork(accountId, dataset, itemDirect, signals);
        EvaluateEphemeralItemOutflow(account, itemDirect, now, signals);
        EvaluateFeederNetwork(accountId, dataset, cinderIncoming, signals);
        EvaluateYoungAccountOutflow(account, cinderOutgoing, incomingTotal + outgoingTotal, signals);
        EvaluateCircularTransfers(accountId, dataset, signals);

        ApplyCorrelationControls(signals);
        TrimSupportingEvidence(signals);
        ApplyCategoryCaps(signals);
        var score = Math.Clamp(signals.Sum(x => x.Contribution), 0, 100);
        var relationships = BuildRelationships(accountId, dataset, direct);
        return new AccountRiskEvaluation(
            account.AccountId,
            account.CharacterId,
            account.AccountLabel,
            account.CharacterName,
            account.CharacterLevel,
            account.AccountCreatedUtc,
            account.LastSessionUtc,
            score,
            Severity(score),
            signals.OrderByDescending(x => x.Contribution).ToList(),
            relationships,
            incomingTotal,
            outgoingTotal,
            direct.Count,
            signals.Select(x => x.LastObservedAt).Where(x => x.HasValue).Max(),
            dataset.AnalysisWindowStart,
            dataset.EvidenceComplete,
            dataset.Transfers.Count);
    }

    private void EvaluateIncomingConcentration(
        Guid accountId,
        IReadOnlyList<AccountRiskTransferFact> incoming,
        long incomingTotal,
        ICollection<AccountRiskSignal> signals)
    {
        if (incoming.Count < policy.MinimumTransferCount || incomingTotal < policy.MinimumRelationshipCinders) return;
        var groups = incoming.GroupBy(x => x.SenderAccountId)
            .Select(x => new { AccountId = x.Key, Value = x.Sum(y => y.Amount) })
            .OrderByDescending(x => x.Value)
            .ToList();
        if (groups.Count < 2) return;
        var topShare = (decimal)groups[0].Value / incomingTotal;
        if (topShare < policy.ConcentrationThreshold) return;

        var contribution = Scale(topShare, policy.ConcentrationThreshold, 1m, 6, 15);
        signals.Add(Signal(
            AccountRiskSignalType.IncomingConcentration,
            "Resource flow",
            contribution,
            "Concentrated incoming cinders",
            $"{topShare:P0} of direct incoming cinders came from one account across {incoming.Count} transfers.",
            Evidence(("incomingCinders", incomingTotal), ("largestSenderShare", topShare), ("senderCount", groups.Count)),
            incoming));
    }

    private void EvaluateOneSidedRelationships(
        Guid accountId,
        IReadOnlyList<AccountRiskTransferFact> direct,
        ICollection<AccountRiskSignal> signals)
    {
        var strongest = direct
            .GroupBy(x => x.SenderAccountId == accountId ? x.RecipientAccountId : x.SenderAccountId)
            .Select(group =>
            {
                var sent = group.Where(x => x.SenderAccountId == accountId).Sum(x => x.Amount);
                var received = group.Where(x => x.RecipientAccountId == accountId).Sum(x => x.Amount);
                var total = sent + received;
                return new { AccountId = group.Key, Sent = sent, Received = received, Total = total, Imbalance = total == 0 ? 0 : (decimal)Math.Abs(sent - received) / total, Count = group.Count(), Transfers = group.ToList() };
            })
            .Where(x => x.Count >= policy.MinimumTransferCount &&
                        x.Total >= policy.MinimumRelationshipCinders &&
                        x.Imbalance >= policy.RelationshipImbalanceThreshold)
            .OrderByDescending(x => x.Imbalance)
            .ThenByDescending(x => x.Total)
            .FirstOrDefault();
        if (strongest is null) return;

        var direction = strongest.Sent > strongest.Received ? "outgoing" : "incoming";
        signals.Add(Signal(
            AccountRiskSignalType.OneSidedRelationship,
            "Resource flow",
            Scale(strongest.Imbalance, policy.RelationshipImbalanceThreshold, 1m, 6, 15),
            "Highly one-sided transfer relationship",
            $"The strongest repeated relationship was {strongest.Imbalance:P0} one-sided and primarily {direction} across {strongest.Count} direct transfers.",
            Evidence(("sent", strongest.Sent), ("received", strongest.Received), ("imbalance", strongest.Imbalance)),
            strongest.Transfers));
    }

    private void EvaluateOneSidedItemTransfers(
        Guid accountId,
        IReadOnlyList<AccountRiskTransferFact> directItems,
        ICollection<AccountRiskSignal> signals)
    {
        var strongest = directItems
            .GroupBy(x => x.SenderAccountId == accountId ? x.RecipientAccountId : x.SenderAccountId)
            .Select(group =>
            {
                var sent = group.Count(x => x.SenderAccountId == accountId);
                var received = group.Count(x => x.RecipientAccountId == accountId);
                var total = sent + received;
                var imbalance = total == 0 ? 0 : (decimal)Math.Abs(sent - received) / total;
                var contribution = PairwiseItemContribution(total, imbalance);
                return new { Sent = sent, Received = received, Total = total, Imbalance = imbalance, Contribution = contribution, AssetCount = group.Select(x => x.AssetId).Distinct(StringComparer.Ordinal).Count(), Transfers = group.ToList() };
            })
            .Where(x => x.Total >= policy.MinimumItemTransferCount && x.Imbalance >= policy.RelationshipImbalanceThreshold)
            .OrderByDescending(x => x.Contribution)
            .ThenByDescending(x => x.Total)
            .ThenByDescending(x => x.Imbalance)
            .FirstOrDefault();
        if (strongest is null) return;

        var direction = strongest.Sent > strongest.Received ? "sent" : "received";
        signals.Add(Signal(
            AccountRiskSignalType.OneSidedItemTransfer,
            "Resource flow",
            strongest.Contribution,
            "One-sided direct item transfers",
            $"This account only {direction} items in its strongest direct item relationship: {strongest.Total} transfer event(s) involving {strongest.AssetCount} item type(s). Item values are not converted into cinders.",
            Evidence(("sentItemTransfers", strongest.Sent), ("receivedItemTransfers", strongest.Received), ("imbalance", strongest.Imbalance), ("distinctItemTypes", strongest.AssetCount)),
            strongest.Transfers,
            "item-flow"));
    }

    private void EvaluateIncomingItemFunnel(
        Guid accountId,
        IReadOnlyList<AccountRiskTransferFact> directItems,
        ICollection<AccountRiskSignal> signals)
    {
        var incoming = directItems.Where(x => x.RecipientAccountId == accountId).ToList();
        var outgoingCount = directItems.Count(x => x.SenderAccountId == accountId);
        var total = incoming.Count + outgoingCount;
        if (incoming.Count < policy.MinimumItemFunnelTransferCount || total == 0) return;
        var sourceCount = incoming.Select(x => x.SenderAccountId).Distinct().Count();
        if (sourceCount < policy.MinimumItemFunnelCounterpartyCount) return;
        var incomingShare = (decimal)incoming.Count / total;
        if (incomingShare < policy.ItemFunnelIncomingShareThreshold) return;

        var volumePoints = Scale(
            incoming.Count,
            policy.MinimumItemFunnelTransferCount,
            Math.Max(policy.MinimumItemFunnelTransferCount + 1, policy.ItemFunnelFullScaleTransferCount),
            12,
            24);
        var shareBonus = incomingShare >= 0.95m ? 3 : incomingShare >= 0.90m ? 2 : 1;
        var sourceBonus = sourceCount >= 10 ? 3 : sourceCount >= 5 ? 2 : 1;
        var contribution = Math.Min(30, volumePoints + shareBonus + sourceBonus);
        signals.Add(Signal(
            AccountRiskSignalType.IncomingItemFunnel,
            "Resource flow",
            contribution,
            "Account-wide incoming item funnel",
            $"{incoming.Count} of {total} direct item-transfer events ({incomingShare:P0}) moved into this account from {sourceCount} accounts. Item values are not estimated by this rule.",
            Evidence(
                ("incomingItemTransfers", incoming.Count),
                ("outgoingItemTransfers", outgoingCount),
                ("incomingShare", incomingShare),
                ("sourceAccountCount", sourceCount),
                ("distinctItemTypes", incoming.Select(x => x.AssetId).Distinct(StringComparer.Ordinal).Count())),
            directItems,
            "item-flow"));
    }

    private void EvaluateItemQuantityConsolidation(
        Guid accountId,
        IReadOnlyList<AccountRiskTransferFact> directItems,
        ICollection<AccountRiskSignal> signals)
    {
        var imbalancedAssets = directItems
            .GroupBy(x => x.AssetId, StringComparer.Ordinal)
            .Select(group =>
            {
                var incomingQuantity = group
                    .Where(x => x.RecipientAccountId == accountId)
                    .Sum(x => x.EffectiveQuantity);
                var outgoingQuantity = group
                    .Where(x => x.SenderAccountId == accountId)
                    .Sum(x => x.EffectiveQuantity);
                var totalQuantity = incomingQuantity + outgoingQuantity;
                var incomingShare = totalQuantity == 0 ? 0 : (decimal)incomingQuantity / totalQuantity;
                return new ItemQuantityFlow(
                    group.Key,
                    incomingQuantity,
                    outgoingQuantity,
                    incomingShare,
                    group.ToList());
            })
            .Where(x => x.IncomingQuantity >= policy.MinimumConsolidatedItemQuantity &&
                        x.IncomingShare >= policy.ConsolidatedItemIncomingShareThreshold)
            .OrderByDescending(x => x.IncomingQuantity - x.OutgoingQuantity)
            .ToList();
        var supportingTransfers = imbalancedAssets.SelectMany(x => x.Transfers).ToList();
        if (imbalancedAssets.Count < policy.MinimumConsolidatedItemAssetCount ||
            supportingTransfers.Count < policy.MinimumConsolidatedItemTransferCount) return;

        var averageIncomingShare = imbalancedAssets.Average(x => x.IncomingShare);
        var contribution = Math.Min(
            30,
            17 +
            Scale(
                imbalancedAssets.Count,
                policy.MinimumConsolidatedItemAssetCount,
                Math.Max(policy.MinimumConsolidatedItemAssetCount + 1, 5),
                0,
                7) +
            Scale(
                supportingTransfers.Count,
                policy.MinimumConsolidatedItemTransferCount,
                Math.Max(policy.MinimumConsolidatedItemTransferCount + 1, 50),
                0,
                4) +
            Scale(averageIncomingShare, policy.ConsolidatedItemIncomingShareThreshold, 1m, 0, 4));
        var strongest = imbalancedAssets[0];
        var examples = string.Join(
            ", ",
            imbalancedAssets.Take(3).Select(x => $"{x.AssetId}: {x.IncomingQuantity:N0} in / {x.OutgoingQuantity:N0} out"));
        signals.Add(Signal(
            AccountRiskSignalType.ItemQuantityConsolidation,
            "Resource flow",
            contribution,
            "Same-asset item quantity consolidation",
            $"Incoming quantities materially exceeded outgoing quantities for {imbalancedAssets.Count} item types ({examples}). Only matching asset IDs offset one another; unrelated outbound items do not.",
            Evidence(
                ("qualifyingAssetCount", imbalancedAssets.Count),
                ("supportingItemTransfers", supportingTransfers.Count),
                ("averageIncomingQuantityShare", averageIncomingShare),
                ("strongestAssetIncomingQuantity", strongest.IncomingQuantity),
                ("strongestAssetOutgoingQuantity", strongest.OutgoingQuantity)),
            supportingTransfers,
            "item-flow"));
    }

    private void EvaluateYoungItemSourceNetwork(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        IReadOnlyList<AccountRiskTransferFact> directItems,
        ICollection<AccountRiskSignal> signals)
    {
        var sources = YoungItemSources(accountId, dataset, directItems);
        var transferCount = sources.Sum(x => x.Transfers.Count);
        var totalItemTransfers = directItems.Count;
        var incomingShare = totalItemTransfers == 0 ? 0 : (decimal)directItems.Count(x => x.RecipientAccountId == accountId) / totalItemTransfers;
        if (sources.Count < policy.MinimumYoungItemSourceCounterpartyCount ||
            transferCount < policy.MinimumYoungItemSourceTransferCount ||
            incomingShare < policy.ItemFunnelIncomingShareThreshold) return;

        var volumePoints = Scale(
            transferCount,
            policy.MinimumYoungItemSourceTransferCount,
            Math.Max(policy.MinimumYoungItemSourceTransferCount + 1, policy.ItemFunnelFullScaleTransferCount),
            0,
            7);
        var sourcePoints = Scale(
            sources.Count,
            policy.MinimumYoungItemSourceCounterpartyCount,
            Math.Max(policy.MinimumYoungItemSourceCounterpartyCount + 1, 10),
            0,
            8);
        var contribution = Math.Min(25, 10 + volumePoints + sourcePoints);
        signals.Add(Signal(
            AccountRiskSignalType.YoungItemSourceNetwork,
            "Account context",
            contribution,
            "Young-account item source network",
            $"{sources.Count} accounts were at most {policy.YoungAccountDays} days old when they began sending {transferCount} direct item-transfer events into this account; {sources.Count(x => x.WasLowLevel)} were level {policy.YoungAccountMaximumLevel} or below in retained event data.",
            Evidence(
                ("youngSourceCount", sources.Count),
                ("lowLevelSourceCount", sources.Count(x => x.WasLowLevel)),
                ("incomingItemTransfers", transferCount),
                ("accountIncomingItemShare", incomingShare),
                ("maximumAgeAtFirstTransferDays", (decimal)sources.Max(x => x.AgeAtFirstTransferDays))),
            sources.SelectMany(x => x.Transfers),
            "young-item-network"));
    }

    private void EvaluateYoungItemCoordinationNetwork(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        IReadOnlyList<AccountRiskTransferFact> directItems,
        ICollection<AccountRiskSignal> signals)
    {
        var counterparties = YoungItemCounterparties(accountId, dataset, directItems);
        if (counterparties.Count < policy.MinimumYoungItemCoordinationCounterpartyCount) return;
        var counterpartyIds = counterparties.Select(x => x.AccountId).ToHashSet();
        var networkTransfers = directItems
            .Where(x => counterpartyIds.Contains(x.SenderAccountId == accountId
                ? x.RecipientAccountId
                : x.SenderAccountId))
            .ToList();
        var incomingCount = networkTransfers.Count(x => x.RecipientAccountId == accountId);
        var outgoingCount = networkTransfers.Count(x => x.SenderAccountId == accountId);
        if (networkTransfers.Count < policy.MinimumYoungItemCoordinationTransferCount ||
            incomingCount < policy.MinimumMixedDirectionItemTransferCount ||
            outgoingCount < policy.MinimumMixedDirectionItemTransferCount) return;

        var sessions = SummarizeItemTransferSessions(accountId, networkTransfers);
        if (sessions.Total < policy.MinimumItemCoordinationSessionCount ||
            sessions.Incoming == 0 ||
            sessions.Outgoing == 0) return;
        var dominantSessionShare = (decimal)Math.Max(sessions.Incoming, sessions.Outgoing) / sessions.Total;
        if (dominantSessionShare < policy.ItemCoordinationDominantSessionShareThreshold) return;

        var contribution = Math.Min(
            25,
            12 +
            Scale(
                counterparties.Count,
                policy.MinimumYoungItemCoordinationCounterpartyCount,
                Math.Max(policy.MinimumYoungItemCoordinationCounterpartyCount + 1, 8),
                0,
                5) +
            Scale(
                sessions.Total,
                policy.MinimumItemCoordinationSessionCount,
                Math.Max(policy.MinimumItemCoordinationSessionCount + 1, policy.MinimumItemCoordinationSessionCount * 3),
                0,
                5) +
            Scale(
                networkTransfers.Count,
                policy.MinimumYoungItemCoordinationTransferCount,
                Math.Max(policy.MinimumYoungItemCoordinationTransferCount + 1, policy.ItemFunnelFullScaleTransferCount),
                0,
                3) +
            Scale(dominantSessionShare, policy.ItemCoordinationDominantSessionShareThreshold, 1m, 0, 3));
        signals.Add(Signal(
            AccountRiskSignalType.YoungItemCoordinationNetwork,
            "Account context",
            contribution,
            "Mixed-role young-account item network",
            $"This account exchanged {networkTransfers.Count} item-transfer events with {counterparties.Count} young accounts in {sessions.Total} transfer session(s). Both directions were active, but {dominantSessionShare:P0} of sessions moved in the dominant direction.",
            Evidence(
                ("youngCounterpartyCount", counterparties.Count),
                ("networkItemTransfers", networkTransfers.Count),
                ("incomingItemTransfers", incomingCount),
                ("outgoingItemTransfers", outgoingCount),
                ("transferSessionCount", sessions.Total),
                ("incomingSessionCount", sessions.Incoming),
                ("outgoingSessionCount", sessions.Outgoing),
                ("dominantSessionShare", dominantSessionShare)),
            networkTransfers,
            "young-item-network"));
    }

    private void EvaluateFeederNetwork(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        IReadOnlyList<AccountRiskTransferFact> incoming,
        ICollection<AccountRiskSignal> signals)
    {
        var feeders = incoming.GroupBy(x => x.SenderAccountId)
            .Select(group =>
            {
                var allSenderOutgoing = dataset.GetOutgoing(group.Key)
                    .Where(x => x.Kind == AccountRiskTransferKind.Cinders)
                    .Sum(x => x.Amount);
                var toSubject = group.Sum(x => x.Amount);
                var share = allSenderOutgoing == 0 ? 0 : (decimal)toSubject / allSenderOutgoing;
                dataset.Accounts.TryGetValue(group.Key, out var sender);
                var firstTransfer = group.OrderBy(x => x.OccurredAt).First();
                var createdUtc = firstTransfer.SenderAccountCreatedUtc ?? sender?.AccountCreatedUtc;
                var level = firstTransfer.SenderCharacterLevel ?? sender?.CharacterLevel;
                var ageAtTransfer = createdUtc.HasValue
                    ? (firstTransfer.OccurredAt.UtcDateTime - createdUtc.Value).TotalDays
                    : double.MaxValue;
                var wasYoung = ageAtTransfer <= policy.YoungAccountDays;
                var wasLowLevel = level.HasValue && level.Value <= policy.YoungAccountMaximumLevel;
                return new { AccountId = group.Key, Share = share, WasYoung = wasYoung, WasLowLevel = wasLowLevel, Value = toSubject, AgeAtTransfer = ageAtTransfer, Transfers = group.ToList() };
            })
            .Where(x => x.WasYoung && x.Share >= policy.FeederTargetShareThreshold)
            .ToList();
        if (feeders.Count < policy.MinimumCounterpartyCount || feeders.Sum(x => x.Value) < policy.MinimumFeederCinders) return;

        var contribution = Math.Min(32, 25 + (feeders.Count - policy.MinimumCounterpartyCount) * 3);
        signals.Add(Signal(
            AccountRiskSignalType.FeederNetwork,
            "Resource flow",
            contribution,
            "Possible feeder-account network",
            $"{feeders.Count} accounts were at most {policy.YoungAccountDays} days old when transfers began and sent at least {policy.FeederTargetShareThreshold:P0} of their observable direct outflow to this account; {feeders.Count(x => x.WasLowLevel)} were level {policy.YoungAccountMaximumLevel} or below in the retained event data.",
            Evidence(("feederCount", feeders.Count), ("lowLevelFeederCount", feeders.Count(x => x.WasLowLevel)), ("cindersReceived", feeders.Sum(x => x.Value)), ("maximumAgeAtFirstTransferDays", (decimal)feeders.Max(x => x.AgeAtTransfer))),
            feeders.SelectMany(x => x.Transfers)));
    }

    private void EvaluateEphemeralItemOutflow(
        AccountRiskAccountFact account,
        IReadOnlyList<AccountRiskTransferFact> directItems,
        DateTimeOffset now,
        ICollection<AccountRiskSignal> signals)
    {
        var outgoing = directItems
            .Where(x => x.SenderAccountId == account.AccountId)
            .OrderBy(x => x.OccurredAt)
            .ToList();
        if (outgoing.Count < policy.MinimumEphemeralItemOutflowTransferCount ||
            account.LastSessionUtc is not { } lastSession) return;

        var firstTransfer = outgoing[0];
        var createdUtc = firstTransfer.SenderAccountCreatedUtc ?? account.AccountCreatedUtc;
        var level = firstTransfer.SenderCharacterLevel ?? account.CharacterLevel;
        var ageAtFirstTransferDays = (firstTransfer.OccurredAt.UtcDateTime - createdUtc).TotalDays;
        if (ageAtFirstTransferDays is < 0 || ageAtFirstTransferDays > policy.YoungAccountDays ||
            level > policy.YoungAccountMaximumLevel) return;

        var sessionSpanHours = (lastSession.UtcDateTime - createdUtc).TotalHours;
        if (sessionSpanHours is < 0 || sessionSpanHours > policy.EphemeralAccountMaximumSessionSpanHours) return;

        var lastDirectTransfer = directItems.Max(x => x.OccurredAt);
        var lastObservedActivity = lastSession > lastDirectTransfer ? lastSession : lastDirectTransfer;
        var dormantDays = (now - lastObservedActivity).TotalDays;
        if (dormantDays < policy.EphemeralAccountMinimumDormantDays) return;

        var distinctAssetCount = outgoing.Select(x => x.AssetId).Distinct(StringComparer.Ordinal).Count();
        if (distinctAssetCount < policy.MinimumEphemeralItemDistinctAssetCount) return;

        var dominantRecipient = outgoing
            .GroupBy(x => x.RecipientAccountId)
            .Select(group => new { AccountId = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.AccountId)
            .First();
        var dominantRecipientShare = (decimal)dominantRecipient.Count / outgoing.Count;
        if (dominantRecipientShare < policy.EphemeralItemTargetShareThreshold) return;

        var contribution = Math.Min(
            40,
            30 +
            Scale(
                outgoing.Count,
                policy.MinimumEphemeralItemOutflowTransferCount,
                Math.Max(policy.MinimumEphemeralItemOutflowTransferCount + 1, 20),
                0,
                5) +
            Scale(
                distinctAssetCount,
                policy.MinimumEphemeralItemDistinctAssetCount,
                Math.Max(policy.MinimumEphemeralItemDistinctAssetCount + 1, 10),
                0,
                4) +
            Scale(dominantRecipientShare, policy.EphemeralItemTargetShareThreshold, 1m, 0, 3) +
            Scale(
                (decimal)dormantDays,
                policy.EphemeralAccountMinimumDormantDays,
                Math.Max(policy.EphemeralAccountMinimumDormantDays + 1, 14),
                0,
                3));
        var incoming = directItems.Count(x => x.RecipientAccountId == account.AccountId);
        signals.Add(Signal(
            AccountRiskSignalType.EphemeralItemOutflow,
            "Account lifecycle",
            contribution,
            "Short-lived account item outflow",
            $"This level {level} account began sending items {Math.Max(0, ageAtFirstTransferDays):0.#} day(s) after registration, issued its newest session within {Math.Max(0, sessionSpanHours):0.#} hour(s) of registration, and then sent {outgoing.Count} item-transfer events involving {distinctAssetCount} item types. {dominantRecipientShare:P0} went to one account, and no newer session issuance or direct transfer was observed for {dormantDays:0.#} day(s).",
            Evidence(
                ("accountAgeAtFirstOutgoingDays", (decimal)ageAtFirstTransferDays),
                ("characterLevelAtFirstTransfer", level),
                ("sessionIssuanceSpanHours", (decimal)sessionSpanHours),
                ("dormantDaysAfterLastObservedActivity", (decimal)dormantDays),
                ("outgoingItemTransfers", outgoing.Count),
                ("incomingItemTransfers", incoming),
                ("distinctItemTypes", distinctAssetCount),
                ("dominantRecipientTransfers", dominantRecipient.Count),
                ("dominantRecipientShare", dominantRecipientShare)),
            outgoing,
            "ephemeral-item-account"));
    }

    private void EvaluateYoungAccountOutflow(
        AccountRiskAccountFact account,
        IReadOnlyList<AccountRiskTransferFact> outgoing,
        long totalFlow,
        ICollection<AccountRiskSignal> signals)
    {
        if (outgoing.Count < policy.MinimumTransferCount || totalFlow <= 0) return;
        var firstTransfer = outgoing.OrderBy(x => x.OccurredAt).First();
        var createdUtc = firstTransfer.SenderAccountCreatedUtc ?? account.AccountCreatedUtc;
        var level = firstTransfer.SenderCharacterLevel ?? account.CharacterLevel;
        var ageDays = (firstTransfer.OccurredAt.UtcDateTime - createdUtc).TotalDays;
        if (ageDays > policy.YoungAccountDays) return;
        var outgoingTotal = outgoing.Sum(x => x.Amount);
        if (outgoingTotal < policy.MinimumYoungAccountOutflowCinders) return;
        var share = (decimal)outgoingTotal / totalFlow;
        if (share < policy.FeederTargetShareThreshold) return;

        signals.Add(Signal(
            AccountRiskSignalType.YoungAccountOutflow,
            "Account context",
            25,
            "Young account dominated by outgoing transfers",
            $"This account was {Math.Max(0, (int)ageDays)} days old and level {level} when the observed outgoing pattern began; {share:P0} of its direct-transfer volume went outward.",
            Evidence(("accountAgeAtFirstTransferDays", (decimal)ageDays), ("characterLevelAtFirstTransfer", (decimal)level), ("outgoingShare", share), ("outgoingCinders", outgoingTotal)),
            outgoing));
    }

    private void EvaluateCircularTransfers(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        ICollection<AccountRiskSignal> signals)
    {
        var outbound = dataset.GetOutgoing(accountId).Where(x => x.Kind == AccountRiskTransferKind.Cinders);
        var inboundBySender = dataset.GetIncoming(accountId)
            .Where(x => x.Kind == AccountRiskTransferKind.Cinders)
            .ToLookup(x => x.SenderAccountId);
        var cycles = new Dictionary<string, IReadOnlyList<AccountRiskTransferFact>>(StringComparer.Ordinal);
        foreach (var first in outbound)
        {
            foreach (var second in dataset.GetOutgoing(first.RecipientAccountId).Where(x => x.Kind == AccountRiskTransferKind.Cinders && x.RecipientAccountId != accountId))
            {
                foreach (var third in inboundBySender[second.RecipientAccountId])
                {
                    if (first.OccurredAt > second.OccurredAt || second.OccurredAt > third.OccurredAt) continue;
                    var times = new[] { first.OccurredAt, second.OccurredAt, third.OccurredAt };
                    if ((times.Max() - times.Min()).TotalHours > policy.CircularWindowHours) continue;
                    var values = new[] { first.Amount, second.Amount, third.Amount };
                    if (values.Min() < policy.MinimumCircularTransferCinders) continue;
                    if ((decimal)values.Min() / values.Max() < policy.CircularValueSimilarity) continue;
                    cycles.TryAdd($"{first.RecipientAccountId:N}:{second.RecipientAccountId:N}", [first, second, third]);
                }
            }
        }
        if (cycles.Count == 0) return;

        signals.Add(Signal(
            AccountRiskSignalType.CircularTransfer,
            "Transfer chains",
            Math.Min(30, 20 + (cycles.Count - 1) * 5),
            "Circular cinder movement",
            $"Found {cycles.Count} three-account transfer cycle(s) with similar values inside {policy.CircularWindowHours} hours.",
            Evidence(("cycleCount", cycles.Count), ("windowHours", policy.CircularWindowHours), ("minimumValueSimilarity", policy.CircularValueSimilarity)),
            cycles.Values.SelectMany(x => x)));
    }

    private IReadOnlyList<AccountRiskRelationship> BuildRelationships(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        IReadOnlyList<AccountRiskTransferFact> direct)
    {
        var directItems = direct.Where(x => x.Kind == AccountRiskTransferKind.Item).ToList();
        var youngItemSources = YoungItemSources(accountId, dataset, directItems);
        var itemIncomingShare = directItems.Count == 0
            ? 0
            : (decimal)directItems.Count(x => x.RecipientAccountId == accountId) / directItems.Count;
        var itemSourceNetworkQualified =
            youngItemSources.Count >= policy.MinimumYoungItemSourceCounterpartyCount &&
            youngItemSources.Sum(x => x.Transfers.Count) >= policy.MinimumYoungItemSourceTransferCount &&
            itemIncomingShare >= policy.ItemFunnelIncomingShareThreshold;
        var qualifiedYoungItemSourceIds = itemSourceNetworkQualified
            ? youngItemSources.Select(x => x.AccountId).ToHashSet()
            : [];
        var youngItemCounterparties = YoungItemCounterparties(accountId, dataset, directItems);
        var youngCounterpartyIds = youngItemCounterparties.Select(x => x.AccountId).ToHashSet();
        var coordinationTransfers = directItems.Where(x => youngCounterpartyIds.Contains(
            x.SenderAccountId == accountId ? x.RecipientAccountId : x.SenderAccountId)).ToList();
        var coordinationIncomingCount = coordinationTransfers.Count(x => x.RecipientAccountId == accountId);
        var coordinationOutgoingCount = coordinationTransfers.Count(x => x.SenderAccountId == accountId);
        var coordinationSessions = SummarizeItemTransferSessions(accountId, coordinationTransfers);
        var coordinationDominantShare = coordinationSessions.Total == 0
            ? 0
            : (decimal)Math.Max(coordinationSessions.Incoming, coordinationSessions.Outgoing) / coordinationSessions.Total;
        var itemCoordinationNetworkQualified =
            youngItemCounterparties.Count >= policy.MinimumYoungItemCoordinationCounterpartyCount &&
            coordinationTransfers.Count >= policy.MinimumYoungItemCoordinationTransferCount &&
            coordinationIncomingCount >= policy.MinimumMixedDirectionItemTransferCount &&
            coordinationOutgoingCount >= policy.MinimumMixedDirectionItemTransferCount &&
            coordinationSessions.Total >= policy.MinimumItemCoordinationSessionCount &&
            coordinationDominantShare >= policy.ItemCoordinationDominantSessionShareThreshold;
        var coordinatedYoungItemIds = itemCoordinationNetworkQualified ? youngCounterpartyIds : [];

        return direct.GroupBy(x => x.SenderAccountId == accountId ? x.RecipientAccountId : x.SenderAccountId)
            .Select(group =>
            {
                var sent = group.Where(x => x.Kind == AccountRiskTransferKind.Cinders && x.RecipientAccountId == accountId).Sum(x => x.Amount);
                var received = group.Where(x => x.Kind == AccountRiskTransferKind.Cinders && x.SenderAccountId == accountId).Sum(x => x.Amount);
                var itemTransfersToSubject = group.Count(x => x.Kind == AccountRiskTransferKind.Item && x.RecipientAccountId == accountId);
                var itemTransfersFromSubject = group.Count(x => x.Kind == AccountRiskTransferKind.Item && x.SenderAccountId == accountId);
                dataset.Accounts.TryGetValue(group.Key, out var counterparty);
                var firstTransfer = group.OrderBy(x => x.OccurredAt).First();
                var counterpartyCreatedUtc = firstTransfer.SenderAccountId == group.Key
                    ? firstTransfer.SenderAccountCreatedUtc
                    : firstTransfer.RecipientAccountCreatedUtc;
                counterpartyCreatedUtc ??= counterparty?.AccountCreatedUtc;
                var young = counterpartyCreatedUtc.HasValue &&
                    (firstTransfer.OccurredAt.UtcDateTime - counterpartyCreatedUtc.Value).TotalDays <= policy.YoungAccountDays;
                var total = sent + received;
                var itemTotal = itemTransfersToSubject + itemTransfersFromSubject;
                var cinderImbalance = total == 0 ? 0 : (decimal)Math.Abs(sent - received) / total;
                var itemImbalance = itemTotal == 0 ? 0 : (decimal)Math.Abs(itemTransfersToSubject - itemTransfersFromSubject) / itemTotal;
                var itemFlowIsStronger = itemImbalance > cinderImbalance;
                var imbalance = Math.Max(cinderImbalance, itemImbalance);
                var flowsToSubject = itemFlowIsStronger
                    ? itemTransfersToSubject > itemTransfersFromSubject
                    : sent > received;
                var isOneSided = imbalance >= policy.RelationshipImbalanceThreshold;
                var relationship = qualifiedYoungItemSourceIds.Contains(group.Key) &&
                                   itemFlowIsStronger && flowsToSubject && isOneSided
                    ? "Young item-source network"
                    : coordinatedYoungItemIds.Contains(group.Key) && itemTransfersToSubject > itemTransfersFromSubject
                        ? "Young coordinated item source"
                        : coordinatedYoungItemIds.Contains(group.Key) && itemTransfersFromSubject > itemTransfersToSubject
                            ? "Young coordinated item recipient"
                            : coordinatedYoungItemIds.Contains(group.Key) && itemTotal > 0
                                ? "Young coordinated item exchange"
                    : flowsToSubject && young && itemFlowIsStronger && isOneSided
                        ? "Young one-sided item source"
                        : flowsToSubject && young && isOneSided
                            ? "Young one-sided cinder source"
                            : isOneSided ? "One-sided transfer" : "Mutual transfer";
                return new AccountRiskRelationship(
                    group.Key,
                    counterparty?.CharacterId ?? Guid.Empty,
                    counterparty?.CharacterName ?? group.Key.ToString(),
                    relationship,
                    sent,
                    received,
                    group.Count(),
                    young,
                    ItemTransfersToSubject: itemTransfersToSubject,
                    ItemTransfersFromSubject: itemTransfersFromSubject);
            })
            .OrderByDescending(x => x.SentToSubject + x.ReceivedFromSubject + x.ItemTransfersToSubject + x.ItemTransfersFromSubject)
            .Take(50)
            .ToList();
    }

    private void ApplyCategoryCaps(List<AccountRiskSignal> signals)
    {
        CapCategory(signals, "Resource flow", policy.FlowCategoryCap);
        CapCategory(signals, "Account context", policy.CoordinationCategoryCap);
    }

    private IReadOnlyList<YoungItemSource> YoungItemSources(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        IReadOnlyList<AccountRiskTransferFact> directItems) =>
        directItems
            .Where(x => x.RecipientAccountId == accountId)
            .GroupBy(x => x.SenderAccountId)
            .Select(group =>
            {
                dataset.Accounts.TryGetValue(group.Key, out var sender);
                var transfers = group.OrderBy(x => x.OccurredAt).ToList();
                var firstTransfer = transfers[0];
                var createdUtc = firstTransfer.SenderAccountCreatedUtc ?? sender?.AccountCreatedUtc;
                var level = firstTransfer.SenderCharacterLevel ?? sender?.CharacterLevel;
                var ageDays = createdUtc.HasValue
                    ? (firstTransfer.OccurredAt.UtcDateTime - createdUtc.Value).TotalDays
                    : double.MaxValue;
                return new YoungItemSource(
                    group.Key,
                    ageDays is >= 0 && ageDays <= policy.YoungAccountDays,
                    level.HasValue && level.Value <= policy.YoungAccountMaximumLevel,
                    ageDays,
                    transfers);
            })
            .Where(x => x.WasYoung)
            .ToList();

    private IReadOnlyList<YoungItemCounterparty> YoungItemCounterparties(
        Guid accountId,
        AccountRiskAnalysisDataset dataset,
        IReadOnlyList<AccountRiskTransferFact> directItems) =>
        directItems
            .GroupBy(x => x.SenderAccountId == accountId ? x.RecipientAccountId : x.SenderAccountId)
            .Select(group =>
            {
                dataset.Accounts.TryGetValue(group.Key, out var counterparty);
                var transfers = group.OrderBy(x => x.OccurredAt).ToList();
                var firstTransfer = transfers[0];
                var counterpartyIsSender = firstTransfer.SenderAccountId == group.Key;
                var createdUtc = counterpartyIsSender
                    ? firstTransfer.SenderAccountCreatedUtc
                    : firstTransfer.RecipientAccountCreatedUtc;
                createdUtc ??= counterparty?.AccountCreatedUtc;
                var level = counterpartyIsSender
                    ? firstTransfer.SenderCharacterLevel
                    : firstTransfer.RecipientCharacterLevel;
                level ??= counterparty?.CharacterLevel;
                var ageDays = createdUtc.HasValue
                    ? (firstTransfer.OccurredAt.UtcDateTime - createdUtc.Value).TotalDays
                    : double.MaxValue;
                return new YoungItemCounterparty(
                    group.Key,
                    ageDays is >= 0 && ageDays <= policy.YoungAccountDays,
                    level.HasValue && level.Value <= policy.YoungAccountMaximumLevel,
                    ageDays,
                    transfers);
            })
            .Where(x => x.WasYoung)
            .ToList();

    private ItemTransferSessionSummary SummarizeItemTransferSessions(
        Guid accountId,
        IReadOnlyList<AccountRiskTransferFact> transfers)
    {
        var incoming = 0;
        var outgoing = 0;
        var sessionWindow = TimeSpan.FromMinutes(policy.ItemTransferSessionWindowMinutes);
        foreach (var group in transfers.GroupBy(x => new
                 {
                     CounterpartyId = x.SenderAccountId == accountId ? x.RecipientAccountId : x.SenderAccountId,
                     Incoming = x.RecipientAccountId == accountId
                 }))
        {
            DateTimeOffset? previous = null;
            foreach (var transfer in group.OrderBy(x => x.OccurredAt))
            {
                if (previous is null || transfer.OccurredAt - previous.Value > sessionWindow)
                {
                    if (group.Key.Incoming) incoming++;
                    else outgoing++;
                }

                previous = transfer.OccurredAt;
            }
        }

        return new ItemTransferSessionSummary(incoming + outgoing, incoming, outgoing);
    }

    private int PairwiseItemContribution(int transferCount, decimal imbalance) => Math.Min(
        15,
        Scale(imbalance, policy.RelationshipImbalanceThreshold, 1m, 4, 9) +
        Scale(
            transferCount,
            policy.MinimumItemTransferCount,
            Math.Max(policy.MinimumItemTransferCount + 1, 50),
            2,
            6));

    private static void ApplyCorrelationControls(List<AccountRiskSignal> signals)
    {
        var visited = new HashSet<int>();
        for (var start = 0; start < signals.Count; start++)
        {
            if (!visited.Add(start)) continue;
            var component = new List<int> { start };
            var componentEvidence = new HashSet<Guid>(signals[start].SupportingTransferIds ?? []);
            if (componentEvidence.Count == 0) continue;

            var changed = true;
            while (changed)
            {
                changed = false;
                for (var candidate = 0; candidate < signals.Count; candidate++)
                {
                    if (visited.Contains(candidate)) continue;
                    if (!string.Equals(
                            signals[candidate].CorrelationFamily,
                            signals[start].CorrelationFamily,
                            StringComparison.Ordinal)) continue;
                    var candidateEvidence = signals[candidate].SupportingTransferIds ?? [];
                    if (!candidateEvidence.Any(componentEvidence.Contains)) continue;
                    visited.Add(candidate);
                    component.Add(candidate);
                    componentEvidence.UnionWith(candidateEvidence);
                    changed = true;
                }
            }

            var winner = component
                .OrderByDescending(index => signals[index].Contribution)
                .ThenBy(index => index)
                .First();
            foreach (var index in component.Where(index => index != winner))
            {
                signals[index] = signals[index] with { Contribution = 0 };
            }
        }
    }

    private static void TrimSupportingEvidence(List<AccountRiskSignal> signals)
    {
        const int retainedEvidenceLimit = 500;
        for (var index = 0; index < signals.Count; index++)
        {
            var supportingIds = signals[index].SupportingTransferIds ?? [];
            signals[index] = signals[index] with
            {
                SupportingTransferIds = supportingIds.Take(retainedEvidenceLimit).ToList(),
                SupportingTransferCount = supportingIds.Count,
                SupportingEvidenceComplete = supportingIds.Count <= retainedEvidenceLimit
            };
        }
    }

    private static void CapCategory(List<AccountRiskSignal> signals, string category, int cap)
    {
        var categorySignals = signals.Where(x => x.Category == category).ToList();
        var total = categorySignals.Sum(x => x.Contribution);
        if (total <= cap) return;
        var factor = (decimal)cap / total;
        foreach (var signal in categorySignals)
        {
            var index = signals.IndexOf(signal);
            signals[index] = signal with
            {
                Contribution = signal.Contribution == 0
                    ? 0
                    : Math.Max(1, (int)Math.Floor(signal.Contribution * factor))
            };
        }

        var excess = signals.Where(x => x.Category == category).Sum(x => x.Contribution) - cap;
        while (excess > 0)
        {
            var candidate = signals
                .Where(x => x.Category == category && x.Contribution > 1)
                .OrderByDescending(x => x.Contribution)
                .FirstOrDefault();
            if (candidate is null) break;
            var index = signals.IndexOf(candidate);
            signals[index] = candidate with { Contribution = candidate.Contribution - 1 };
            excess--;
        }
    }

    private AccountRiskSeverity Severity(int score) => score >= policy.CriticalScore
        ? AccountRiskSeverity.Critical
        : score >= policy.HighScore
            ? AccountRiskSeverity.High
            : score >= policy.ModerateScore ? AccountRiskSeverity.Moderate : AccountRiskSeverity.Low;

    private static int Scale(decimal value, decimal lower, decimal upper, int minimum, int maximum)
    {
        if (upper <= lower) return maximum;
        var ratio = Math.Clamp((value - lower) / (upper - lower), 0m, 1m);
        return minimum + (int)Math.Round(ratio * (maximum - minimum));
    }

    private static IReadOnlyDictionary<string, decimal> Evidence(params (string Key, decimal Value)[] values) =>
        values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

    private static AccountRiskSignal Signal(
        AccountRiskSignalType type,
        string category,
        int contribution,
        string title,
        string explanation,
        IReadOnlyDictionary<string, decimal> evidence,
        IEnumerable<AccountRiskTransferFact> supportingTransfers,
        string correlationFamily = "direct-transfer")
    {
        var transfers = supportingTransfers
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.OccurredAt)
            .ToList();
        return new AccountRiskSignal(
            type,
            category,
            contribution,
            title,
            explanation,
            evidence,
            transfers.Select(x => x.Id).ToList(),
            transfers.Count == 0 ? null : transfers[0].OccurredAt,
            transfers.Count == 0 ? null : transfers[^1].OccurredAt,
            transfers.Count,
            true,
            correlationFamily);
    }

    private sealed record YoungItemSource(
        Guid AccountId,
        bool WasYoung,
        bool WasLowLevel,
        double AgeAtFirstTransferDays,
        IReadOnlyList<AccountRiskTransferFact> Transfers);

    private sealed record YoungItemCounterparty(
        Guid AccountId,
        bool WasYoung,
        bool WasLowLevel,
        double AgeAtFirstTransferDays,
        IReadOnlyList<AccountRiskTransferFact> Transfers);

    private sealed record ItemQuantityFlow(
        string AssetId,
        long IncomingQuantity,
        long OutgoingQuantity,
        decimal IncomingShare,
        IReadOnlyList<AccountRiskTransferFact> Transfers);

    private sealed record ItemTransferSessionSummary(int Total, int Incoming, int Outgoing);

}
