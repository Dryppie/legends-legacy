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
            Transfer(feeder, main, 3_500, 3),
        };

        var result = _evaluator.Evaluate(feeder.AccountId, Dataset([main, feeder], transfers), Now);

        Assert.Contains(result.Signals, x => x.Type == AccountRiskSignalType.YoungAccountOutflow);
        Assert.Contains(result.Signals, x => x.Type == AccountRiskSignalType.OneSidedRelationship);
        Assert.True(result.Score >= AccountRiskPolicy.Default.ModerateScore);
    }

    [Fact]
    public void SingleYoungAccountFunnelFlagsBothSenderAndRecipientForInvestigation()
    {
        var main = Account("SingleFunnelMain", 500, 80);
        var feeder = Account("SingleFunnelAlt", 5, 45);
        var transfer = HistoricalTransfer(feeder, main, 25_000, Now.AddDays(-2), senderLevel: 40);
        var dataset = Dataset([main, feeder], [transfer]);

        var mainResult = _evaluator.Evaluate(main.AccountId, dataset, Now);
        var feederResult = _evaluator.Evaluate(feeder.AccountId, dataset, Now);

        Assert.Contains(mainResult.Signals, x => x.Type == AccountRiskSignalType.FeederNetwork);
        Assert.Contains(feederResult.Signals, x => x.Type == AccountRiskSignalType.YoungAccountOutflow);
        Assert.Equal(AccountRiskSeverity.Moderate, mainResult.Severity);
        Assert.Equal(AccountRiskSeverity.Moderate, feederResult.Severity);
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
            HistoricalTransfer(feederA, main, 9_500, occurredAt, senderLevel: 8),
            HistoricalTransfer(feederB, main, 8_700, occurredAt.AddHours(1), senderLevel: 55),
        };

        var result = _evaluator.Evaluate(main.AccountId, Dataset([main, feederA, feederB], transfers), Now);

        var signal = Assert.Single(result.Signals, x => x.Type == AccountRiskSignalType.FeederNetwork);
        Assert.Equal(2m, signal.Evidence["feederCount"]);
        Assert.Equal(1m, signal.Evidence["lowLevelFeederCount"]);
        Assert.Equal(AccountRiskSeverity.Moderate, result.Severity);
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
            Transfer(a, b, 10_000, 1),
            Transfer(b, c, 9_500, 2),
            Transfer(c, a, 9_000, 3),
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
