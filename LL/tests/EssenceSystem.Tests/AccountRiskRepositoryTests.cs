using System.Text.Json;
using Domain.Models.Administration;
using Domain.Models.Economy;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Administration;

namespace EssenceSystem.Tests;

public sealed class AccountRiskRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CandidateSelectionPrioritizesAccountsThatHaveNeverBeenEvaluated()
    {
        await using var context = CreateContext();
        var pendingAccount = Guid.NewGuid();
        var currentAccount = Guid.NewGuid();
        var currentCounterparty = Guid.NewGuid();
        context.EconomyLedger.AddRange(
            Transfer(pendingAccount, currentCounterparty, Now.AddDays(-2)),
            Transfer(currentAccount, currentCounterparty, Now.AddMinutes(-2)));
        context.AccountRiskSnapshots.AddRange(
            Snapshot(currentAccount, evaluationVersion: 6, evaluatedAt: Now),
            Snapshot(currentCounterparty, evaluationVersion: 6, evaluatedAt: Now));
        await context.SaveChangesAsync();
        var repository = Repository(context);

        var candidates = await repository.GetCandidateAccountIdsAsync(
            Now.AddDays(-90),
            evaluationVersion: 6,
            limit: 1,
            CancellationToken.None);

        Assert.Equal([pendingAccount], candidates);
    }

    [Fact]
    public async Task CoverageSeparatesCurrentSnapshotsFromPendingEligibleAccounts()
    {
        await using var context = CreateContext();
        var currentAccount = Guid.NewGuid();
        var pendingAccount = Guid.NewGuid();
        var currentCounterparty = Guid.NewGuid();
        context.EconomyLedger.AddRange(
            Transfer(currentAccount, currentCounterparty, Now.AddDays(-2)),
            Transfer(pendingAccount, currentCounterparty, Now.AddDays(-1)));
        context.AccountRiskSnapshots.AddRange(
            Snapshot(currentAccount, evaluationVersion: 6, evaluatedAt: Now),
            Snapshot(currentCounterparty, evaluationVersion: 6, evaluatedAt: Now),
            Snapshot(pendingAccount, evaluationVersion: 5, evaluatedAt: Now));
        await context.SaveChangesAsync();
        var repository = Repository(context);

        var page = await repository.SearchAsync(
            new AccountRiskSearch(null, null, null, null, null, null, null, "risk", 1, 50),
            Now.AddDays(-90),
            evaluationVersion: 6,
            lookbackDays: 90,
            CancellationToken.None);

        Assert.Equal(3, page.EligibleAccountCount);
        Assert.Equal(2, page.UpToDateAccountCount);
        Assert.Equal(1, page.PendingEvaluationCount);
        Assert.Equal(2, page.EvaluatedAccountCount);
        Assert.Equal(2, page.Entries.Count);
    }

    private static LLDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AccountRiskRepository Repository(LLDbContext context) =>
        new(context, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static EconomyLedgerEntry Transfer(Guid sender, Guid recipient, DateTimeOffset occurredAt) => new()
    {
        EventType = EconomyEventType.DirectCurrencyTransfer,
        AssetType = EconomyAssetType.Currency,
        SenderAccountId = sender,
        RecipientAccountId = recipient,
        AssetId = "currency:cinders",
        AssetName = "Cinders",
        Quantity = 10_000,
        TotalValue = 10_000,
        OccurredAt = occurredAt,
        Source = "test"
    };

    private static AccountRiskSnapshot Snapshot(Guid accountId, int evaluationVersion, DateTimeOffset evaluatedAt) => new()
    {
        AccountId = accountId,
        CharacterId = Guid.NewGuid(),
        AccountLabel = accountId.ToString(),
        CharacterName = "Risk test",
        EvaluationVersion = evaluationVersion,
        EvaluatedAt = evaluatedAt,
        AnalysisWindowStart = Now.AddDays(-90),
        EvidenceComplete = true
    };
}
