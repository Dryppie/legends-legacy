using API.LiveOps.Support;
using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Transfers;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed class TransferConversationCorrelationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Material_repeated_transfers_without_conversation_meet_pattern_without_extra_signal()
    {
        var seeded = await SeedAsync();
        var service = CreateService(seeded.Options, EvidenceMode.NoConversation);

        var result = await service.GetAsync(seeded.SubjectAccountId, CancellationToken.None);

        var report = Assert.IsType<TransferConversationCorrelationReportDto>(result.Report);
        var entry = Assert.Single(report.Entries);
        Assert.True(report.EvidenceComplete);
        Assert.True(entry.MeetsPatternThreshold);
        Assert.Equal("UncommunicativeValueTransferPattern", entry.Assessment);
        Assert.Equal(3, entry.TransferCount);
        Assert.Equal(12_000, entry.CinderValue);
        Assert.Equal(3, entry.NoRecordedConversationCount);
    }

    [Fact]
    public async Task Bidirectional_conversation_prevents_the_pattern()
    {
        var seeded = await SeedAsync();
        var service = CreateService(seeded.Options, EvidenceMode.Bidirectional);

        var result = await service.GetAsync(seeded.SubjectAccountId, CancellationToken.None);

        var entry = Assert.Single(result.Report!.Entries);
        Assert.False(entry.MeetsPatternThreshold);
        Assert.Equal("RecordedBidirectionalConversation", entry.Assessment);
        Assert.Equal(3, entry.EstablishedConversationCount);
    }

    [Fact]
    public async Task Unavailable_chat_is_incomplete_and_is_not_treated_as_no_conversation()
    {
        var seeded = await SeedAsync();
        var service = CreateService(seeded.Options, EvidenceMode.Unavailable);

        var result = await service.GetAsync(seeded.SubjectAccountId, CancellationToken.None);

        var report = Assert.IsType<TransferConversationCorrelationReportDto>(result.Report);
        var entry = Assert.Single(report.Entries);
        Assert.False(report.EvidenceComplete);
        Assert.False(entry.MeetsPatternThreshold);
        Assert.Equal("ChatUnavailable", entry.Assessment);
        Assert.Equal(3, report.UnavailableConversationCount);
        Assert.Equal(0, entry.NoRecordedConversationCount);
    }

    private static TransferConversationCorrelationService CreateService(
        DbContextOptions<LLDbContext> options,
        EvidenceMode mode) => new(
        new TestContextFactory(options),
        new TestChatGateway(mode),
        Options.Create(new LiveOpsOptions
        {
            TransferConversationRelationshipDays = 90,
            TransferConversationLookbackDays = 30,
            TransferConversationAfterHours = 2,
            MaximumTransferConversationCorrelationRows = 500,
            UncommunicativeMinimumTransferCount = 3,
            UncommunicativeMinimumCinders = 10_000,
            UncommunicativeMinimumItemTransferCount = 3
        }),
        new FixedTimeProvider(Now));

    private static async Task<SeededDatabase> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var database = new LLDbContext(options);
        var subject = AppUser.Guest();
        subject.Username = "subject";
        var counterparty = AppUser.Guest();
        counterparty.Username = "counterparty";
        database.Users.AddRange(subject, counterparty);
        for (var index = 0; index < 3; index++)
        {
            database.PlayerTransferHistory.Add(new PlayerTransferRecord
            {
                Kind = PlayerTransferKind.Cinders,
                SenderAccountId = counterparty.Id,
                SenderCharacterId = Guid.NewGuid(),
                SenderCharacterName = "Counterparty",
                RecipientAccountId = subject.Id,
                RecipientCharacterId = Guid.NewGuid(),
                RecipientCharacterName = "Subject",
                AssetId = "currency:cinders",
                AssetName = "Cinders",
                Quantity = 4_000,
                OccurredAt = Now.AddDays(-index - 1)
            });
        }
        await database.SaveChangesAsync();
        return new SeededDatabase(options, subject.Id);
    }

    private sealed record SeededDatabase(
        DbContextOptions<LLDbContext> Options,
        Guid SubjectAccountId);

    private enum EvidenceMode
    {
        NoConversation,
        Bidirectional,
        Unavailable
    }

    private sealed class TestContextFactory(DbContextOptions<LLDbContext> options)
        : IDbContextFactory<LLDbContext>
    {
        public LLDbContext CreateDbContext() => new(options);
        public ValueTask<LLDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestChatGateway(EvidenceMode mode) : IChatModerationGateway
    {
        public Task<ChatConversationEvidenceGatewayResult> GetConversationEvidenceAsync(
            IReadOnlyList<ChatConversationEvidenceGatewayQuery> queries,
            CancellationToken cancellationToken)
        {
            if (mode == EvidenceMode.Unavailable)
            {
                return Task.FromResult(new ChatConversationEvidenceGatewayResult(
                    false,
                    true,
                    [],
                    "Chat unavailable"));
            }
            var count = mode == EvidenceMode.Bidirectional ? 1 : 0;
            return Task.FromResult(new ChatConversationEvidenceGatewayResult(
                true,
                true,
                queries.Select(query => new ChatConversationEvidenceGatewayEntry(
                    query.EvidenceId,
                    count,
                    count,
                    0,
                    null,
                    null,
                    0,
                    0,
                    [],
                    null)).ToList(),
                string.Empty));
        }

        public Task<ChatModerationStateGatewayResult> GetStateAsync(Guid characterId, int historyLimit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatModerationAuditGatewayResult> GetAuditAsync(ChatModerationAuditGatewayQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatPlayerMessageGatewayResult> GetPlayerMessagesAsync(Guid characterId, string? cursor, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> MuteAsync(ChatMuteGatewayRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChatModerationGatewayResult> UnmuteAsync(ChatUnmuteGatewayRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
