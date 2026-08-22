using System.Text.Json;
using Domain.Models.Administration;
using Domain.Models.Economy;
using Domain.Models.Transfers;
using Domain.Models.Users;
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

    [Fact]
    public async Task DetailsReportsTheRetainedTransferTotalWhenTheTimelineIsLimited()
    {
        await using var context = CreateContext();
        var accountId = Guid.NewGuid();
        var counterpartyId = Guid.NewGuid();
        context.AccountRiskSnapshots.Add(Snapshot(accountId, evaluationVersion: 7, evaluatedAt: Now));
        context.PlayerTransferHistory.AddRange(
            PlayerTransfer(counterpartyId, accountId, Now.AddMinutes(-3)),
            PlayerTransfer(counterpartyId, accountId, Now.AddMinutes(-2)),
            PlayerTransfer(counterpartyId, accountId, Now.AddMinutes(-1)));
        await context.SaveChangesAsync();

        var details = await Repository(context).GetDetailsAsync(accountId, transferLimit: 2, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(3, details.TotalRetainedTransferCount);
        Assert.Equal(2, details.Transfers.Count);
    }

    [Fact]
    public async Task TemporalDatasetBoundsRelatedAccountsAndResolvesTokenChainsWithoutReturningHashes()
    {
        await using var context = CreateContext();
        var subjectId = Guid.NewGuid();
        var relatedId = Guid.NewGuid();
        var relationship = new AccountRiskRelationship(
            relatedId, Guid.NewGuid(), "Related", "Sender", 10_000, 0, 3, false);
        var snapshot = Snapshot(subjectId, evaluationVersion: 7, evaluatedAt: Now);
        snapshot.CharacterName = "Subject";
        snapshot.RelationshipsJson = JsonSerializer.Serialize(new[] { relationship });
        context.AccountRiskSnapshots.Add(snapshot);
        context.RefreshTokens.AddRange(
            new RefreshToken
            {
                Id = 1, UserId = subjectId, TokenHash = "subject-root", ReplacedBy = "subject-refresh",
                CreatedUtc = Now.AddDays(-1).UtcDateTime, ExpiresUtc = Now.AddDays(29).UtcDateTime
            },
            new RefreshToken
            {
                Id = 2, UserId = subjectId, TokenHash = "subject-refresh",
                CreatedUtc = Now.AddDays(-1).AddMinutes(30).UtcDateTime, ExpiresUtc = Now.AddDays(29).UtcDateTime
            },
            new RefreshToken
            {
                Id = 3, UserId = relatedId, TokenHash = "related-root",
                CreatedUtc = Now.AddDays(-1).AddMinutes(3).UtcDateTime, ExpiresUtc = Now.AddDays(29).UtcDateTime
            });
        await context.SaveChangesAsync();

        var repository = new AccountTemporalCorrelationRepository(
            context,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var dataset = await repository.GetDatasetAsync(
            subjectId, Now.AddDays(-90), Now, 20, 1_000, 1_000, CancellationToken.None);

        Assert.NotNull(dataset);
        Assert.Equal([relatedId], dataset.RelatedAccountIds);
        Assert.Equal(3, dataset.Tokens.Count);
        Assert.Equal(2, dataset.Tokens.Single(x => x.Id == 1).ReplacementId);
        Assert.True(dataset.EvidenceComplete);
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

    private static PlayerTransferRecord PlayerTransfer(Guid sender, Guid recipient, DateTimeOffset occurredAt) => new()
    {
        Kind = PlayerTransferKind.InventoryItem,
        SenderAccountId = sender,
        SenderCharacterId = Guid.NewGuid(),
        SenderCharacterName = "Source",
        RecipientAccountId = recipient,
        RecipientCharacterId = Guid.NewGuid(),
        RecipientCharacterName = "Subject",
        AssetId = "item:wood",
        AssetName = "Wood",
        Quantity = 1,
        OccurredAt = occurredAt
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
