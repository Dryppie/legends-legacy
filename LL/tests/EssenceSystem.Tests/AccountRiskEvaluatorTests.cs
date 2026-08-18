using Domain.Models.Administration;

namespace EssenceSystem.Tests;

public sealed class AccountRiskEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
    private readonly AccountRiskEvaluator _evaluator = new(AccountRiskPolicy.Default);

    [Fact]
    public void BalancedEstablishedTradingRemainsLowRisk()
    {
        var a = Account("A", 365, 70);
        var b = Account("B", 400, 72);
        var transfers = new List<AccountRiskTransferFact>();
        for (var index = 0; index < 10; index++)
        {
            transfers.Add(Transfer(a, b, 500_000, index));
            transfers.Add(Transfer(b, a, 490_000, index + 20));
        }

        var result = _evaluator.Evaluate(a.AccountId, Dataset([a, b], transfers), Now);

        Assert.Equal(AccountRiskSeverity.Low, result.Severity);
        Assert.Empty(result.Signals);
    }

    [Fact]
    public void YoungFeederProducesContextualOutflowSignal()
    {
        var main = Account("Main", 500, 80);
        var feeder = Account("Feeder", 2, 8);
        var transfers = new[]
        {
            Transfer(feeder, main, 3_000, 1),
            Transfer(feeder, main, 3_000, 2),
            Transfer(feeder, main, 4_000, 3),
        };

        var result = _evaluator.Evaluate(feeder.AccountId, Dataset([main, feeder], transfers), Now);

        Assert.Contains(result.Signals, x => x.Type == AccountRiskSignalType.YoungAccountOutflow);
        Assert.Contains(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedRelationship);
        Assert.True(result.Score >= AccountRiskPolicy.Default.ModerateScore);
    }

    [Fact]
    public void SingleYoungAccountTransferDoesNotCreateAnInvestigationPriority()
    {
        var main = Account("SingleFunnelMain", 500, 80);
        var feeder = Account("SingleFunnelAlt", 5, 45);
        var transfer = HistoricalTransfer(feeder, main, 25_000, Now.AddDays(-2), senderLevel: 40);
        var dataset = Dataset([main, feeder], [transfer]);

        var mainResult = _evaluator.Evaluate(main.AccountId, dataset, Now);
        var feederResult = _evaluator.Evaluate(feeder.AccountId, dataset, Now);

        Assert.Empty(mainResult.Signals);
        Assert.Empty(feederResult.Signals);
        Assert.Equal(AccountRiskSeverity.Low, mainResult.Severity);
        Assert.Equal(AccountRiskSeverity.Low, feederResult.Severity);
    }

    [Fact]
    public void SingleEstablishedOneWayTransferDoesNotCreateAnInvestigationPriority()
    {
        var sender = Account("EstablishedSender", 500, 80);
        var recipient = Account("EstablishedRecipient", 700, 90);
        var dataset = Dataset([sender, recipient], [Transfer(sender, recipient, 5_000, 1)]);

        var senderResult = _evaluator.Evaluate(sender.AccountId, dataset, Now);
        var recipientResult = _evaluator.Evaluate(recipient.AccountId, dataset, Now);

        Assert.Empty(senderResult.Signals);
        Assert.Empty(recipientResult.Signals);
        Assert.Equal(AccountRiskSeverity.Low, senderResult.Severity);
        Assert.Equal(AccountRiskSeverity.Low, recipientResult.Severity);
    }

    [Fact]
    public void SingleOneWayItemTransferDoesNotCreateAnInvestigationPriority()
    {
        var sender = Account("ItemSender", 500, 80);
        var recipient = Account("ItemRecipient", 700, 90);
        var dataset = Dataset([sender, recipient], [ItemTransfer(sender, recipient, "item:legendary-sword", 1)]);

        var senderResult = _evaluator.Evaluate(sender.AccountId, dataset, Now);
        var recipientResult = _evaluator.Evaluate(recipient.AccountId, dataset, Now);

        Assert.Empty(senderResult.Signals);
        Assert.Empty(recipientResult.Signals);
        Assert.Equal(AccountRiskSeverity.Low, senderResult.Severity);
        Assert.Equal(AccountRiskSeverity.Low, recipientResult.Severity);
        Assert.Equal(0, senderResult.IncomingCinders);
        Assert.Equal(0, senderResult.OutgoingCinders);
        Assert.Equal(0, recipientResult.IncomingCinders);
        Assert.Equal(0, recipientResult.OutgoingCinders);
    }

    [Fact]
    public void ReciprocalItemSwapRemainsLowRisk()
    {
        var a = Account("ItemTraderA", 500, 80);
        var b = Account("ItemTraderB", 700, 90);
        var transfers = new[]
        {
            ItemTransfer(a, b, "item:sword", 1),
            ItemTransfer(b, a, "item:shield", 2),
        };

        var aResult = _evaluator.Evaluate(a.AccountId, Dataset([a, b], transfers), Now);
        var bResult = _evaluator.Evaluate(b.AccountId, Dataset([a, b], transfers), Now);

        Assert.Equal(AccountRiskSeverity.Low, aResult.Severity);
        Assert.Equal(AccountRiskSeverity.Low, bResult.Severity);
        Assert.DoesNotContain(aResult.Signals, x => x.Type == AccountRiskSignalType.OneSidedItemTransfer);
        Assert.DoesNotContain(bResult.Signals, x => x.Type == AccountRiskSignalType.OneSidedItemTransfer);
    }

    [Fact]
    public void AccountWideItemFunnelPrioritizesTheReported193Of200Pattern()
    {
        var subject = Account("ReportedItemFunnel", 8, 51);
        var sources = Enumerable.Range(1, 19)
            .Select(index => Account($"ReportedSource{index}", 7, 5 + index % 10))
            .ToList();
        var transfers = new List<AccountRiskTransferFact>();
        for (var index = 0; index < 193; index++)
        {
            transfers.Add(SequencedItemTransfer(
                sources[index % sources.Count],
                subject,
                $"item:resource:{index % 8}",
                index));
        }
        for (var index = 0; index < 7; index++)
        {
            transfers.Add(SequencedItemTransfer(
                subject,
                sources[index],
                $"item:return:{index}",
                193 + index));
        }

        var result = _evaluator.Evaluate(subject.AccountId, Dataset([subject, .. sources], transfers), Now);

        var funnel = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.IncomingItemFunnel);
        var sourceNetwork = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.YoungItemSourceNetwork);
        var pairwise = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedItemTransfer);
        Assert.Equal(30, funnel.Contribution);
        Assert.Equal(25, sourceNetwork.Contribution);
        Assert.Equal(0, pairwise.Contribution);
        Assert.Equal(193m, funnel.Evidence["incomingItemTransfers"]);
        Assert.Equal(19m, funnel.Evidence["sourceAccountCount"]);
        Assert.Equal(55, result.Score);
        Assert.Equal(AccountRiskSeverity.High, result.Severity);
        Assert.True(result.Relationships.Count(x => x.Relationship == "Young item-source network") >= 10);
    }

    [Fact]
    public void PairwiseItemEvidenceRanksMaterialRelationshipAheadOfPerfectSmallRelationship()
    {
        var subject = Account("PairwiseSubject", 500, 80);
        var materialSource = Account("MaterialSource", 500, 80);
        var perfectSmallSource = Account("PerfectSmallSource", 500, 80);
        var transfers = new List<AccountRiskTransferFact>();
        for (var index = 0; index < 61; index++)
        {
            transfers.Add(SequencedItemTransfer(materialSource, subject, "item:wood", index));
        }
        transfers.Add(SequencedItemTransfer(subject, materialSource, "item:return", 61));
        for (var index = 0; index < 16; index++)
        {
            transfers.Add(SequencedItemTransfer(perfectSmallSource, subject, "item:dust", 70 + index));
        }

        var result = _evaluator.Evaluate(
            subject.AccountId,
            Dataset([subject, materialSource, perfectSmallSource], transfers),
            Now);

        var pairwise = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedItemTransfer);
        Assert.Equal(62, pairwise.SupportingTransferCount);
        Assert.Equal(61m, pairwise.Evidence["receivedItemTransfers"]);
        Assert.Equal(1m, pairwise.Evidence["sentItemTransfers"]);
        Assert.Equal(0, pairwise.Contribution);
        Assert.Contains(result.Signals, x => x.Type == AccountRiskSignalType.IncomingItemFunnel && x.Contribution > 0);
    }

    [Fact]
    public void QuantityAndSessionScoringDetectsTheReportedMixedRoleCoverTrafficPattern()
    {
        var subject = Account("ReportedMixedRoleMule", 8, 51);
        var counterparties = new[]
        {
            (Account("BiggieSmalls", 7, 12), Incoming: 63, Outgoing: 19),
            (Account("Niko", 7, 10), Incoming: 13, Outgoing: 6),
            (Account("Shakob", 7, 14), Incoming: 2, Outgoing: 16),
            (Account("Slaasmand", 7, 8), Incoming: 10, Outgoing: 0),
            (Account("Dryp", 7, 9), Incoming: 5, Outgoing: 2),
            (Account("neskidug", 7, 7), Incoming: 3, Outgoing: 2),
            (Account("FieryElephantWalker_8482", 7, 11), Incoming: 3, Outgoing: 0),
            (Account("smol", 7, 6), Incoming: 1, Outgoing: 0),
        };
        var incomingStacks = new Queue<(string AssetId, long Quantity)>(
        [
            ("wood", 800), ("wood", 700), ("wood", 42), ("wood", 26), ("wood", 7),
            ("ore", 100), ("ore", 120), ("ore", 8), ("ore", 30), ("ore", 11),
            ("ore", 77), ("ore", 50), ("ore", 3), ("ore", 3),
            ("rawhide", 350), ("rawhide", 65), ("rawhide", 15), ("rawhide", 14),
            ("soul_dust", 50), ("soul_dust", 50),
        ]);
        var transfers = new List<AccountRiskTransferFact>();
        var remainingIncoming = counterparties.Select(x => x.Incoming).ToArray();
        var sequence = 0;
        while (remainingIncoming.Any(x => x > 0))
        {
            for (var counterpartyIndex = 0; counterpartyIndex < counterparties.Length; counterpartyIndex++)
            {
                if (remainingIncoming[counterpartyIndex] == 0) continue;
                var item = incomingStacks.TryDequeue(out var stack)
                    ? stack
                    : (AssetId: $"item:incoming-cover:{sequence}", Quantity: 1L);
                transfers.Add(TimedItemTransfer(
                    counterparties[counterpartyIndex].Item1,
                    subject,
                    item.AssetId,
                    item.Quantity,
                    sequence,
                    Now.AddMinutes(-3_000 + sequence * 10)));
                remainingIncoming[counterpartyIndex]--;
                sequence++;
            }
        }

        var outgoingStacks = new Queue<(string AssetId, long Quantity)>(
        [("wood", 150), ("ore", 13), ("rawhide", 100)]);
        for (var counterpartyIndex = 0; counterpartyIndex < counterparties.Length; counterpartyIndex++)
        {
            for (var index = 0; index < counterparties[counterpartyIndex].Outgoing; index++)
            {
                var item = outgoingStacks.TryDequeue(out var stack)
                    ? stack
                    : (AssetId: $"item:outgoing-cover:{sequence}", Quantity: 1L);
                transfers.Add(TimedItemTransfer(
                    subject,
                    counterparties[counterpartyIndex].Item1,
                    item.AssetId,
                    item.Quantity,
                    sequence,
                    Now.AddMinutes(-1_000 + counterpartyIndex * 30).AddSeconds(index * 10)));
                sequence++;
            }
        }

        var result = _evaluator.Evaluate(
            subject.AccountId,
            Dataset([subject, .. counterparties.Select(x => x.Item1)], transfers),
            Now);

        Assert.Equal(145, transfers.Count);
        Assert.DoesNotContain(result.Signals, x => x.Type == AccountRiskSignalType.IncomingItemFunnel);
        Assert.DoesNotContain(result.Signals, x => x.Type == AccountRiskSignalType.YoungItemSourceNetwork);
        var consolidation = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.ItemQuantityConsolidation);
        var coordination = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.YoungItemCoordinationNetwork);
        var pairwise = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedItemTransfer);
        Assert.Equal(25, consolidation.Contribution);
        Assert.Equal(25, coordination.Contribution);
        Assert.Equal(0, pairwise.Contribution);
        Assert.Equal(4m, consolidation.Evidence["qualifyingAssetCount"]);
        Assert.Equal(100m, coordination.Evidence["incomingItemTransfers"]);
        Assert.Equal(45m, coordination.Evidence["outgoingItemTransfers"]);
        Assert.True(coordination.Evidence["transferSessionCount"] < coordination.Evidence["networkItemTransfers"]);
        Assert.Equal(50, result.Score);
        Assert.Equal(AccountRiskSeverity.High, result.Severity);
        Assert.Contains(result.Relationships, x => x.Relationship == "Young coordinated item recipient");
        Assert.Contains(result.Relationships, x => x.Relationship == "Young coordinated item source");
    }

    [Fact]
    public void BalancedSameAssetTradingDoesNotCreateConsolidationOrCoordinationSignals()
    {
        var subject = Account("BalancedTrader", 100, 50);
        var counterparties = Enumerable.Range(1, 4).Select(x => Account($"BalancedPartner{x}", 7, 15)).ToList();
        var transfers = new List<AccountRiskTransferFact>();
        var sequence = 0;
        foreach (var counterparty in counterparties)
        {
            for (var index = 0; index < 10; index++)
            {
                transfers.Add(TimedItemTransfer(counterparty, subject, "wood", 100, sequence, Now.AddMinutes(-2_000 + sequence * 10)));
                sequence++;
                transfers.Add(TimedItemTransfer(subject, counterparty, "wood", 100, sequence, Now.AddMinutes(-2_000 + sequence * 10)));
                sequence++;
            }
        }

        var result = _evaluator.Evaluate(
            subject.AccountId,
            Dataset([subject, .. counterparties], transfers),
            Now);

        Assert.DoesNotContain(result.Signals, x => x.Type == AccountRiskSignalType.ItemQuantityConsolidation);
        Assert.DoesNotContain(result.Signals, x => x.Type == AccountRiskSignalType.YoungItemCoordinationNetwork);
    }

    [Fact]
    public void ShortLivedDormantAccountWithConcentratedItemOutflowIsHighPriority()
    {
        var createdAt = Now.AddDays(-7);
        var subject = new AccountRiskAccountFact(
            StableGuid("account:ReportedEphemeralFeeder"),
            StableGuid("character:ReportedEphemeralFeeder"),
            "ReportedEphemeralFeeder",
            "test2",
            7,
            createdAt.UtcDateTime,
            createdAt.AddHours(18));
        var recipient = Account("ReportedRecipient", 500, 80);
        var sourceA = Account("ReportedSourceA", 7, 8);
        var sourceB = Account("ReportedSourceB", 7, 9);
        var transfers = new List<AccountRiskTransferFact>();
        for (var index = 0; index < 3; index++)
        {
            transfers.Add(TimedItemTransfer(
                sourceA,
                subject,
                $"item:input-a:{index}",
                10,
                index,
                createdAt.AddHours(6).AddMinutes(index)));
            transfers.Add(TimedItemTransfer(
                sourceB,
                subject,
                $"item:input-b:{index}",
                10,
                index,
                createdAt.AddHours(12).AddMinutes(index)));
        }
        for (var index = 0; index < 12; index++)
        {
            transfers.Add(TimedItemTransfer(
                subject,
                recipient,
                $"item:outgoing:{Math.Min(index, 10)}",
                1,
                index,
                createdAt.AddHours(18).AddMinutes(index)));
        }

        var result = _evaluator.Evaluate(
            subject.AccountId,
            Dataset([subject, recipient, sourceA, sourceB], transfers),
            Now);

        var lifecycle = Assert.Single(
            result.Signals,
            x => x.Type == AccountRiskSignalType.EphemeralItemOutflow);
        Assert.Equal(12m, lifecycle.Evidence["outgoingItemTransfers"]);
        Assert.Equal(11m, lifecycle.Evidence["distinctItemTypes"]);
        Assert.Equal(1m, lifecycle.Evidence["dominantRecipientShare"]);
        Assert.True(lifecycle.Evidence["dormantDaysAfterLastObservedActivity"] >= 6m);
        Assert.Equal(AccountRiskSeverity.High, result.Severity);
        Assert.Equal(52, result.Score);
    }

    [Fact]
    public void ActiveYoungAccountDoesNotProduceEphemeralItemOutflowSignal()
    {
        var subject = Account("ActiveYoungSender", 7, 7) with { LastSessionUtc = Now.AddHours(-1) };
        var recipient = Account("ActiveYoungRecipient", 500, 80);
        var transfers = Enumerable.Range(0, 12)
            .Select(index => TimedItemTransfer(
                subject,
                recipient,
                $"item:gift:{index}",
                1,
                index,
                Now.AddDays(-6).AddMinutes(index)))
            .ToList();

        var result = _evaluator.Evaluate(
            subject.AccountId,
            Dataset([subject, recipient], transfers),
            Now);

        Assert.DoesNotContain(
            result.Signals,
            x => x.Type == AccountRiskSignalType.EphemeralItemOutflow);
    }

    [Fact]
    public void MultipleYoungAccountsCreateFeederNetworkSignal()
    {
        var main = Account("Main", 500, 80);
        var feeders = Enumerable.Range(1, 4).Select(x => Account($"F{x}", x + 1, 5 + x)).ToList();
        var transfers = feeders.Select((x, index) => Transfer(x, main, 10_000 + index * 1_000, index)).ToList();

        var result = _evaluator.Evaluate(main.AccountId, Dataset([main, .. feeders], transfers), Now);

        var signal = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.FeederNetwork);
        Assert.True(signal.Contribution >= AccountRiskPolicy.Default.ModerateScore);
        Assert.Equal(4m, signal.Evidence["feederCount"]);
    }

    [Fact]
    public void TwoFeedersThatWereYoungWhenTransfersBeganFlagTheRecipient()
    {
        var main = Account("HistoricalMain", 500, 80);
        var feederA = Account("HistoricalFeederA", 30, 42);
        var feederB = Account("HistoricalFeederB", 32, 45);
        var occurredAt = Now.AddDays(-20);
        var transfers = new[]
        {
            HistoricalTransfer(feederA, main, 10_500, occurredAt, senderLevel: 8),
            HistoricalTransfer(feederB, main, 10_500, occurredAt.AddHours(1), senderLevel: 55),
        };

        var result = _evaluator.Evaluate(main.AccountId, Dataset([main, feederA, feederB], transfers), Now);

        var signal = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.FeederNetwork);
        Assert.Equal(2m, signal.Evidence["feederCount"]);
        Assert.Equal(1m, signal.Evidence["lowLevelFeederCount"]);
        Assert.Equal(AccountRiskSeverity.Moderate, result.Severity);
        Assert.Equal(25, result.Score);
        Assert.All(result.Signals.Where(x => x.Type != AccountRiskSignalType.FeederNetwork), x => Assert.Equal(0, x.Contribution));
    }

    [Fact]
    public void HighAbsoluteVolumeAloneDoesNotIncreaseRisk()
    {
        var trader = Account("Trader", 900, 100);
        var counterparties = Enumerable.Range(1, 5).Select(x => Account($"T{x}", 600, 90)).ToList();
        var transfers = counterparties.SelectMany((counterparty, index) => new[]
        {
            Transfer(trader, counterparty, 10_000_000 + index, index),
            Transfer(counterparty, trader, 10_000_000 + index, index + 10)
        }).ToList();

        var result = _evaluator.Evaluate(trader.AccountId, Dataset([trader, .. counterparties], transfers), Now);

        Assert.Equal(0, result.Score);
        Assert.Equal(AccountRiskSeverity.Low, result.Severity);
    }

    [Fact]
    public void EconomicallySimilarThreeHopCycleIsReported()
    {
        var a = Account("A", 200, 60);
        var b = Account("B", 180, 58);
        var c = Account("C", 190, 61);
        var transfers = new[]
        {
            Transfer(a, b, 12_000, 1),
            Transfer(b, c, 11_500, 2),
            Transfer(c, a, 11_000, 3),
        };

        var result = _evaluator.Evaluate(a.AccountId, Dataset([a, b, c], transfers), Now);

        Assert.Contains(result.Signals, x => x.Type == AccountRiskSignalType.CircularTransfer);
    }

    [Fact]
    public void SameOutflowIsContextualizedByAccountAgeAndProgression()
    {
        var recipient = Account("Recipient", 700, 90);
        var young = Account("Young", 2, 8);
        var established = Account("Established", 600, 95);
        var youngTransfers = Enumerable.Range(1, 3).Select(x => Transfer(young, recipient, 10_000, x)).ToList();
        var oldTransfers = Enumerable.Range(1, 3).Select(x => Transfer(established, recipient, 10_000, x + 10)).ToList();
        var dataset = Dataset([recipient, young, established], [.. youngTransfers, .. oldTransfers]);

        var youngResult = _evaluator.Evaluate(young.AccountId, dataset, Now);
        var establishedResult = _evaluator.Evaluate(established.AccountId, dataset, Now);

        Assert.Contains(youngResult.Signals, x => x.Type == AccountRiskSignalType.YoungAccountOutflow);
        Assert.DoesNotContain(establishedResult.Signals, x => x.Type == AccountRiskSignalType.YoungAccountOutflow);
        Assert.True(youngResult.Score > establishedResult.Score);
    }

    [Fact]
    public void OverlappingSignalsAreVisibleButTheTransfersContributeOnlyOnce()
    {
        var recipient = Account("CorrelationRecipient", 700, 90);
        var young = Account("CorrelationYoung", 2, 8);
        var transfers = new[]
        {
            Transfer(young, recipient, 6_000, 1),
            Transfer(young, recipient, 6_000, 2),
        };

        var result = _evaluator.Evaluate(young.AccountId, Dataset([recipient, young], transfers), Now);

        var youngSignal = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.YoungAccountOutflow);
        var relationshipSignal = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedRelationship);
        Assert.Equal(25, youngSignal.Contribution);
        Assert.Equal(0, relationshipSignal.Contribution);
        Assert.Equal(25, result.Score);
        Assert.Equal(transfers.Select(x => x.Id).Order(), youngSignal.SupportingTransferIds!.Order());
        Assert.Equal(transfers.Min(x => x.OccurredAt), youngSignal.FirstObservedAt);
        Assert.Equal(transfers.Max(x => x.OccurredAt), result.LastTriggeredAt);
    }

    [Fact]
    public void EvaluationCarriesEvidenceCompletenessMetadata()
    {
        var a = Account("IncompleteA", 300, 70);
        var b = Account("IncompleteB", 300, 70);
        var transfers = new[] { Transfer(a, b, 20_000, 1), Transfer(a, b, 20_000, 2) };
        var windowStart = Now.AddDays(-90);
        var dataset = new AccountRiskAnalysisDataset(
            new[] { a, b }.ToDictionary(x => x.AccountId),
            transfers,
            windowStart,
            evidenceComplete: false);

        var result = _evaluator.Evaluate(a.AccountId, dataset, Now);

        Assert.False(result.EvidenceComplete);
        Assert.Equal(windowStart, result.AnalysisWindowStart);
        Assert.Equal(2, result.AnalyzedTransferCount);
    }

    [Fact]
    public void StoredSignalEvidenceIsBoundedWithoutHidingTheSupportingCount()
    {
        var sender = Account("EvidenceBoundSender", 500, 80);
        var recipient = Account("EvidenceBoundRecipient", 500, 80);
        var transfers = Enumerable.Range(1, 501)
            .Select(index => Transfer(sender, recipient, 100, index))
            .ToList();

        var result = _evaluator.Evaluate(sender.AccountId, Dataset([sender, recipient], transfers), Now);

        var signal = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedRelationship);
        Assert.Equal(501, signal.SupportingTransferCount);
        Assert.Equal(500, signal.SupportingTransferIds!.Count);
        Assert.False(signal.SupportingEvidenceComplete);
    }

    private static AccountRiskAccountFact Account(string name, int ageDays, int level)
    {
        var accountId = StableGuid($"account:{name}");
        return new AccountRiskAccountFact(
            accountId,
            StableGuid($"character:{name}"),
            name,
            name,
            level,
            Now.UtcDateTime.AddDays(-ageDays),
            Now.AddHours(-1));
    }

    private static AccountRiskTransferFact Transfer(AccountRiskAccountFact sender, AccountRiskAccountFact recipient, long amount, int hour) =>
        new(StableGuid($"transfer:{sender.AccountLabel}:{recipient.AccountLabel}:{hour}"), sender.AccountId, recipient.AccountId, amount, Now.AddHours(-100 + hour));

    private static AccountRiskTransferFact ItemTransfer(AccountRiskAccountFact sender, AccountRiskAccountFact recipient, string assetId, int hour) =>
        new(
            StableGuid($"item-transfer:{sender.AccountLabel}:{recipient.AccountLabel}:{assetId}:{hour}"),
            sender.AccountId,
            recipient.AccountId,
            1,
            Now.AddHours(-100 + hour),
            sender.AccountCreatedUtc,
            sender.CharacterLevel,
            recipient.AccountCreatedUtc,
            recipient.CharacterLevel,
            AccountRiskTransferKind.Item,
            assetId);

    private static AccountRiskTransferFact SequencedItemTransfer(
        AccountRiskAccountFact sender,
        AccountRiskAccountFact recipient,
        string assetId,
        int sequence) =>
        new(
            StableGuid($"sequenced-item-transfer:{sender.AccountLabel}:{recipient.AccountLabel}:{assetId}:{sequence}"),
            sender.AccountId,
            recipient.AccountId,
            1,
            Now.AddMinutes(-1_000 + sequence),
            sender.AccountCreatedUtc,
            sender.CharacterLevel,
            recipient.AccountCreatedUtc,
            recipient.CharacterLevel,
            AccountRiskTransferKind.Item,
            assetId);

    private static AccountRiskTransferFact TimedItemTransfer(
        AccountRiskAccountFact sender,
        AccountRiskAccountFact recipient,
        string assetId,
        long quantity,
        int sequence,
        DateTimeOffset occurredAt) =>
        new(
            StableGuid($"timed-item-transfer:{sender.AccountLabel}:{recipient.AccountLabel}:{assetId}:{sequence}"),
            sender.AccountId,
            recipient.AccountId,
            quantity,
            occurredAt,
            sender.AccountCreatedUtc,
            sender.CharacterLevel,
            recipient.AccountCreatedUtc,
            recipient.CharacterLevel,
            AccountRiskTransferKind.Item,
            assetId,
            quantity);

    private static AccountRiskTransferFact HistoricalTransfer(
        AccountRiskAccountFact sender,
        AccountRiskAccountFact recipient,
        long amount,
        DateTimeOffset occurredAt,
        int senderLevel) =>
        new(
            StableGuid($"historical:{sender.AccountLabel}:{recipient.AccountLabel}:{occurredAt:O}"),
            sender.AccountId,
            recipient.AccountId,
            amount,
            occurredAt,
            sender.AccountCreatedUtc,
            senderLevel,
            recipient.AccountCreatedUtc,
            recipient.CharacterLevel);

    private static AccountRiskAnalysisDataset Dataset(
        IReadOnlyCollection<AccountRiskAccountFact> accounts,
        IReadOnlyList<AccountRiskTransferFact> transfers) =>
        new(accounts.ToDictionary(x => x.AccountId), transfers);

    private static Guid StableGuid(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }
}
