using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Domain.Models.Administration;
using Domain.Models.Outbox;
using Domain.Models.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace API.LiveOps.Support;

public sealed partial class LiveOpsPlayerSupportSnapshotService(
    IDbContextFactory<LLDbContext> contextFactory,
    IChatModerationGateway chat,
    IOptions<LiveOpsOptions> options,
    TimeProvider timeProvider,
    ILogger<LiveOpsPlayerSupportSnapshotService> logger)
{
    private const string Source = "Game database";
    private readonly int _sectionTimeoutSeconds = Math.Clamp(
        options.Value.SupportSnapshotSectionTimeoutSeconds,
        1,
        15);
    private readonly int _conversationLookbackDays = Math.Clamp(
        options.Value.TransferConversationLookbackDays,
        1,
        90);
    private readonly int _conversationAfterHours = Math.Clamp(
        options.Value.TransferConversationAfterHours,
        1,
        24);
    private readonly int _conversationImmediateBeforeHours = Math.Clamp(
        options.Value.TransferConversationImmediateBeforeHours,
        1,
        72);

    public async Task<PlayerSupportSnapshotDto?> GetAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        await using var lookup = await contextFactory.CreateDbContextAsync(cancellationToken);
        var target = await lookup.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new TargetRow(x.UserId, x.Id))
            .SingleOrDefaultAsync(cancellationToken);
        if (target is null) return null;

        var account = CaptureAsync(
            "account",
            token => LoadAccountAsync(target, token),
            cancellationToken);
        var activity = CaptureAsync(
            "activity",
            token => LoadActivityAsync(target, token),
            cancellationToken);
        var economy = CaptureAsync(
            "economy",
            token => LoadEconomyAsync(target, token),
            cancellationToken);
        var guild = CaptureAsync(
            "guild",
            token => LoadGuildAsync(target, token),
            cancellationToken);
        var marketplace = CaptureAsync(
            "marketplace",
            token => LoadMarketplaceAsync(target, token),
            cancellationToken);
        var transfers = CaptureAsync(
            "transfers",
            token => LoadTransfersAsync(target, null, 25, token),
            cancellationToken);
        var synchronization = CaptureAsync(
            "synchronization",
            token => LoadSynchronizationAsync(target, token),
            cancellationToken);

        var equipment = CaptureAsync("equipment", token => LoadEquipmentAsync(target, token), cancellationToken);
        await Task.WhenAll(account, activity, economy, guild, marketplace, transfers, synchronization, equipment);
        return new PlayerSupportSnapshotDto(
            target.AccountId,
            target.CharacterId,
            timeProvider.GetUtcNow(),
            await account,
            await activity,
            await economy,
            await guild,
            await marketplace,
            await transfers,
            await synchronization,
            await equipment);
    }

    public async Task<TransferHistoryLookupResult> GetTransferHistoryAsync(
        Guid characterId,
        string? cursorValue,
        int take,
        CancellationToken cancellationToken)
    {
        if (!TransferCursor.TryDecode(cursorValue, out var cursor))
            return new TransferHistoryLookupResult(false, false, null);

        await using var lookup = await contextFactory.CreateDbContextAsync(cancellationToken);
        var target = await lookup.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new TargetRow(x.UserId, x.Id))
            .SingleOrDefaultAsync(cancellationToken);
        if (target is null)
            return new TransferHistoryLookupResult(false, true, null);

        var section = await CaptureAsync(
            "transfers",
            token => LoadTransfersAsync(target, cursor, Math.Clamp(take, 1, 50), token),
            cancellationToken);
        return new TransferHistoryLookupResult(true, true, section);
    }

    public async Task<TransferConversationLookupResult> GetTransferConversationAsync(
        Guid characterId,
        Guid transferId,
        string? cursor,
        int take,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var target = await database.Characters.AsNoTracking()
            .Where(character => character.Id == characterId)
            .Select(character => new TargetRow(character.UserId, character.Id))
            .SingleOrDefaultAsync(cancellationToken);
        if (target is null)
            return new TransferConversationLookupResult(false, false, true, null);

        var transfer = await database.PlayerTransferHistory.AsNoTracking()
            .Where(row =>
                row.Id == transferId &&
                (row.SenderAccountId == target.AccountId ||
                 row.RecipientAccountId == target.AccountId))
            .Select(row => new TransferRow(
                row.Id,
                row.Kind,
                row.SenderAccountId,
                row.SenderCharacterId,
                row.SenderCharacterName,
                row.RecipientAccountId,
                row.RecipientCharacterId,
                row.RecipientCharacterName,
                row.AssetId,
                row.AssetName,
                row.SourceItemInstanceId,
                row.DestinationItemInstanceId,
                row.Quantity,
                row.OccurredAt))
            .SingleOrDefaultAsync(cancellationToken);
        if (transfer is null)
            return new TransferConversationLookupResult(true, false, true, null);

        var result = await chat.GetConversationEvidenceAsync(
            [BuildConversationQuery(transfer, cursor, Math.Clamp(take, 1, 25))],
            cancellationToken);
        if (!result.CursorValid)
            return new TransferConversationLookupResult(true, true, false, null);

        var evidence = result.IsSuccess
            ? result.Evidence.SingleOrDefault(entry => entry.EvidenceId == transfer.TransferId)
            : null;
        var summary = evidence is null
            ? UnavailableConversation(transfer, result.ErrorMessage)
            : ToConversationSummary(transfer, evidence);
        var page = new TransferConversationPageDto(
            transfer.TransferId,
            summary,
            evidence?.Messages.Select(message => new TransferConversationMessageDto(
                message.Id,
                message.SenderId,
                message.SenderName,
                message.Body,
                message.TargetCharacterId,
                message.TargetCharacterName,
                message.SentAt)).ToList() ?? [],
            evidence?.NextCursor);
        return new TransferConversationLookupResult(true, true, true, page);
    }

    private async Task<AccountSupportSnapshotDto> LoadAccountAsync(
        TargetRow target,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var created = await database.Users.AsNoTracking()
            .Where(x => x.Id == target.AccountId)
            .Select(x => x.CreatedUtc)
            .SingleAsync(cancellationToken);
        var lastSession = await database.RefreshTokens.AsNoTracking()
            .Where(x => x.UserId == target.AccountId)
            .Select(x => (DateTime?)x.CreatedUtc)
            .MaxAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var nowUtc = now.UtcDateTime;
        var activeSessions = await database.RefreshTokens.AsNoTracking()
            .CountAsync(x =>
                x.UserId == target.AccountId &&
                x.RevokedUtc == null &&
                x.ExpiresUtc > nowUtc,
                cancellationToken);
        var restrictions = await database.AccountRestrictions.AsNoTracking()
            .Where(x => x.AccountId == target.AccountId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return new AccountSupportSnapshotDto(
            created,
            lastSession,
            activeSessions,
            "The game currently stores session issuance, not a dedicated login-event history.",
            restrictions.Select(x => new AccountRestrictionHistoryDto(
                x.Id,
                x.RestrictionType.ToString(),
                x.RevokedAt.HasValue ? "Revoked" :
                    x.ExpiresAt.HasValue && x.ExpiresAt.Value <= now ? "Expired" : "Active",
                x.Reason,
                x.CreatedBySubject,
                x.CreatedAt,
                x.ExpiresAt,
                x.RevokedBySubject,
                x.RevokedAt,
                x.RevocationReason)).ToList());
    }

    private async Task<ActivitySupportSnapshotDto> LoadActivityAsync(
        TargetRow target,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var action = await database.CharacterActions.AsNoTracking()
            .Include(x => x.ActionDetails)
            .SingleOrDefaultAsync(x => x.CharacterId == target.CharacterId, cancellationToken);
        if (action is null || action.IsDeleted)
        {
            return new ActivitySupportSnapshotDto(
                "Idle", null, action?.UpdatedAt, null, null,
                action?.ScheduleGeneration,
                "No active background action is retained for this character.");
        }

        return new ActivitySupportSnapshotDto(
            action.CharacterActionType.ToString(),
            action.ActionDetails?.GetType().Name,
            action.UpdatedAt,
            action.NextResolutionAtUtc,
            action.BlockedUntilUtc,
            action.ScheduleGeneration,
            "UpdatedAt is the last persisted action mutation, not a login timestamp.");
    }

    private async Task<EconomySupportSnapshotDto> LoadEconomyAsync(
        TargetRow target,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var balances = await database.Characters.AsNoTracking()
            .Where(x => x.Id == target.CharacterId)
            .Select(x => new BalanceRow(
                x.Cinders,
                x.Soulstones,
                x.FateEcho,
                x.SigilFragments,
                x.GuildFavor,
                x.TowerTokens))
            .SingleAsync(cancellationToken);
        var inventory = await database.InventoryItems.AsNoTracking()
            .Where(x => x.InventoryId == target.CharacterId)
            .GroupBy(_ => 1)
            .Select(group => new InventoryTotals(
                group.Count(),
                group.Sum(x => (long)x.Quantity),
                group.Count(x => x.SeenAtUtc == null)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new InventoryTotals(0, 0, 0);
        var acquisitions = await database.InventoryItems.AsNoTracking()
            .Where(x => x.InventoryId == target.CharacterId)
            .OrderByDescending(x => x.ItemInstance.AcquiredAtUtc)
            .Take(10)
            .Select(x => new RecentInventoryAcquisitionDto(
                x.ItemInstanceId,
                x.ItemInstance.ItemBaseId,
                x.ItemInstance.ItemBase.Name,
                x.Quantity,
                x.ItemInstance.AcquisitionSource,
                x.ItemInstance.AcquiredAtUtc))
            .ToListAsync(cancellationToken);
        var grantRows = await database.AdminActions.AsNoTracking()
            .Where(x =>
                x.TargetCharacterId == target.CharacterId &&
                x.ActionType == AdminActionType.CompensationItemsGranted)
            .OrderByDescending(x => x.OccurredAt)
            .Take(10)
            .Select(x => new GrantRow(
                x.Id,
                x.DetailsJson,
                x.Reason,
                x.RiskLevel,
                x.OccurredAt))
            .ToListAsync(cancellationToken);
        var grants = await HydrateGrantsAsync(database, grantRows, cancellationToken);

        return new EconomySupportSnapshotDto(
            balances.Cinders,
            balances.Soulstones,
            balances.FateEcho,
            balances.SigilFragments,
            balances.GuildFavor,
            balances.TowerTokens,
            inventory.RowCount,
            inventory.Quantity,
            inventory.UnseenRows,
            acquisitions,
            grants);
    }

    private static async Task<IReadOnlyList<RecentCompensationGrantDto>> HydrateGrantsAsync(
        LLDbContext database,
        IReadOnlyList<GrantRow> rows,
        CancellationToken cancellationToken)
    {
        var parsed = rows.Select(row => (Row: row, Details: TryReadGrant(row.DetailsJson))).ToList();
        var itemIds = parsed
            .Where(x => x.Details is not null)
            .Select(x => x.Details!.ItemBaseId)
            .Distinct()
            .ToArray();
        var names = await database.ItemBases.AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        return parsed.Select(x => new RecentCompensationGrantDto(
            x.Row.OperationId,
            x.Details?.ItemBaseId ?? "unknown",
            x.Details is not null
                ? names.GetValueOrDefault(x.Details.ItemBaseId, x.Details.ItemBaseId)
                : "Unknown item",
            x.Details?.Quantity ?? 0,
            x.Row.Reason,
            x.Row.RiskLevel.ToString(),
            x.Row.OccurredAt)).ToList();
    }

    private async Task<GuildSupportSnapshotDto> LoadGuildAsync(
        TargetRow target,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await database.GuildMembers.AsNoTracking()
            .Where(x => x.CharacterId == target.CharacterId)
            .Select(x => new GuildSupportSnapshotDto(
                true,
                x.GuildId,
                x.Guild.Name,
                x.Guild.Tag,
                x.Role.ToString(),
                x.JoinedAt,
                x.Guild.GuildLevel,
                x.Guild.Members.Count))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new GuildSupportSnapshotDto(false, null, null, null, null, null, null, null);
    }

    private async Task<MarketplaceSupportSnapshotDto> LoadMarketplaceAsync(
        TargetRow target,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var listingCount = await database.MarketPlaceListings.AsNoTracking()
            .CountAsync(x => x.SellerId == target.CharacterId && x.ExpiresAt > now, cancellationToken);
        var buyOrderCount = await database.MarketPlaceBuyOrders.AsNoTracking()
            .CountAsync(x => x.BuyerId == target.CharacterId && x.ExpiresAt > now, cancellationToken);
        var trades = await database.MarketPlaceOrders.AsNoTracking()
            .Where(x => x.SellerId == target.CharacterId || x.BuyerId == target.CharacterId)
            .OrderByDescending(x => x.PurchasedAt)
            .Take(10)
            .Select(x => new RecentMarketplaceTradeDto(
                x.Id,
                x.BuyerId == target.CharacterId ? "Purchased" : "Sold",
                x.ItemBaseId,
                x.ItemBase.Name,
                x.Quantity,
                x.TotalPrice,
                x.PurchasedAt))
            .ToListAsync(cancellationToken);
        return new MarketplaceSupportSnapshotDto(listingCount, buyOrderCount, trades);
    }

    private async Task<SynchronizationSupportSnapshotDto> LoadSynchronizationAsync(
        TargetRow target,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var prefix = $"character:{target.CharacterId:N}";
        var revisions = await database.StateSyncRevisions.AsNoTracking()
            .Where(x => x.ScopeKey.StartsWith(prefix))
            .OrderByDescending(x => x.UpdatedAt)
            .Take(20)
            .Select(x => new StateRevisionDto(
                x.ScopeKey == prefix ? "all" : x.ScopeKey.Substring(prefix.Length + 1),
                x.Revision,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
        var deliveries = database.GameEventOutboxDeliveries.AsNoTracking()
            .Where(x =>
                x.Message.CharacterId == target.CharacterId ||
                x.Message.AccountId == target.AccountId);
        var pending = await deliveries.CountAsync(
            x => x.Status == GameEventOutboxDeliveryStatus.Pending,
            cancellationToken);
        var failed = await deliveries.CountAsync(
            x => x.Status == GameEventOutboxDeliveryStatus.Failed,
            cancellationToken);
        var oldestPending = await deliveries
            .Where(x => x.Status == GameEventOutboxDeliveryStatus.Pending)
            .Select(x => (DateTimeOffset?)x.CreatedAt)
            .MinAsync(cancellationToken);
        var lastEvent = await database.GameEventOutboxMessages.AsNoTracking()
            .Where(x => x.CharacterId == target.CharacterId || x.AccountId == target.AccountId)
            .Select(x => (DateTimeOffset?)x.CreatedAt)
            .MaxAsync(cancellationToken);
        return new SynchronizationSupportSnapshotDto(
            pending,
            failed,
            oldestPending,
            lastEvent,
            revisions,
            "No bounded pending-reward registry currently exists; no status is inferred from unrelated records.");
    }

    private async Task<TransferHistorySupportSnapshotDto> LoadTransfersAsync(
        TargetRow target,
        TransferCursor? cursor,
        int historyLimit,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = database.PlayerTransferHistory.AsNoTracking()
            .Where(x =>
                x.SenderAccountId == target.AccountId ||
                x.RecipientAccountId == target.AccountId);
        if (cursor is not null)
        {
            query = query.Where(x =>
                x.OccurredAt < cursor.OccurredAt ||
                (x.OccurredAt == cursor.OccurredAt && x.Id.CompareTo(cursor.TransferId) < 0));
        }
        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Take(historyLimit + 1)
            .Select(x => new TransferRow(
                x.Id,
                x.Kind,
                x.SenderAccountId,
                x.SenderCharacterId,
                x.SenderCharacterName,
                x.RecipientAccountId,
                x.RecipientCharacterId,
                x.RecipientCharacterName,
                x.AssetId,
                x.AssetName,
                x.SourceItemInstanceId,
                x.DestinationItemInstanceId,
                x.Quantity,
                x.OccurredAt))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > historyLimit;
        var page = rows.Take(historyLimit).ToList();
        var conversations = await LoadConversationSummariesAsync(page, cancellationToken);
        var nextCursor = hasMore && page.Count > 0
            ? TransferCursor.Encode(page[^1].OccurredAtUtc, page[^1].TransferId)
            : null;
        return new TransferHistorySupportSnapshotDto(
            historyLimit,
            page.Select(x => new PlayerTransferHistoryDto(
                x.TransferId,
                Direction(x, target.AccountId),
                x.Kind.ToString(),
                x.SenderAccountId,
                x.SenderCharacterId,
                x.SenderCharacterName,
                x.RecipientAccountId,
                x.RecipientCharacterId,
                x.RecipientCharacterName,
                x.AssetId,
                x.AssetName,
                x.SourceItemInstanceId,
                x.DestinationItemInstanceId,
                x.Quantity,
                x.OccurredAtUtc,
                conversations.GetValueOrDefault(
                    x.TransferId,
                    UnavailableConversation(
                        x,
                        "Chat did not return evidence for this transfer.")))).ToList(),
            nextCursor);
    }

    private async Task<IReadOnlyDictionary<Guid, TransferConversationSummaryDto>>
        LoadConversationSummariesAsync(
            IReadOnlyList<TransferRow> transfers,
            CancellationToken cancellationToken)
    {
        var summaries = new Dictionary<Guid, TransferConversationSummaryDto>();
        foreach (var batch in transfers.Chunk(25))
        {
            var result = await chat.GetConversationEvidenceAsync(
                batch.Select(transfer => BuildConversationQuery(transfer, null, 0)).ToList(),
                cancellationToken);
            if (!result.IsSuccess)
            {
                foreach (var transfer in batch)
                {
                    summaries[transfer.TransferId] = UnavailableConversation(
                        transfer,
                        result.ErrorMessage);
                }
                continue;
            }

            var evidenceById = result.Evidence.ToDictionary(entry => entry.EvidenceId);
            foreach (var transfer in batch)
            {
                summaries[transfer.TransferId] = evidenceById.TryGetValue(
                    transfer.TransferId,
                    out var evidence)
                    ? ToConversationSummary(transfer, evidence)
                    : UnavailableConversation(
                        transfer,
                        "Chat returned incomplete conversation evidence.");
            }
        }
        return summaries;
    }

    private ChatConversationEvidenceGatewayQuery BuildConversationQuery(
        TransferRow transfer,
        string? cursor,
        int limit) =>
        new(
            transfer.TransferId,
            transfer.SenderCharacterId,
            transfer.RecipientCharacterId,
            transfer.OccurredAtUtc.AddDays(-_conversationLookbackDays),
            transfer.OccurredAtUtc.AddHours(_conversationAfterHours),
            transfer.OccurredAtUtc,
            transfer.OccurredAtUtc.AddHours(-_conversationImmediateBeforeHours),
            transfer.OccurredAtUtc.AddHours(_conversationAfterHours),
            cursor,
            limit);

    private TransferConversationSummaryDto ToConversationSummary(
        TransferRow transfer,
        ChatConversationEvidenceGatewayEntry evidence)
    {
        var status = evidence.FirstToSecondMessageCount > 0 &&
                     evidence.SecondToFirstMessageCount > 0
            ? "EstablishedConversation"
            : evidence.FirstToSecondMessageCount > 0 ||
              evidence.SecondToFirstMessageCount > 0
                ? "OneWayConversation"
                : evidence.SharedChannelCount > 0
                    ? "SharedChannelActivity"
                    : "NoRecordedConversation";
        return new TransferConversationSummaryDto(
            status,
            true,
            null,
            evidence.FirstToSecondMessageCount,
            evidence.SecondToFirstMessageCount,
            evidence.ImmediateMessageCount,
            evidence.FirstMessageAt,
            evidence.LastMessageAt,
            evidence.SharedChannelCount,
            evidence.SharedChannelMessageCount,
            transfer.OccurredAtUtc.AddDays(-_conversationLookbackDays),
            transfer.OccurredAtUtc.AddHours(_conversationAfterHours));
    }

    private TransferConversationSummaryDto UnavailableConversation(
        TransferRow transfer,
        string? message) =>
        new(
            "ChatUnavailable",
            false,
            string.IsNullOrWhiteSpace(message)
                ? "Chat conversation evidence is unavailable."
                : message,
            0,
            0,
            0,
            null,
            null,
            0,
            0,
            transfer.OccurredAtUtc.AddDays(-_conversationLookbackDays),
            transfer.OccurredAtUtc.AddHours(_conversationAfterHours));

    private static string Direction(TransferRow transfer, Guid accountId)
    {
        if (transfer.SenderAccountId == accountId && transfer.RecipientAccountId == accountId)
            return "BetweenOwnCharacters";
        return transfer.SenderAccountId == accountId ? "Outgoing" : "Incoming";
    }

    private async Task<PlayerSupportSection<T>> CaptureAsync<T>(
        string section,
        Func<CancellationToken, Task<T>> load,
        CancellationToken requestCancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_sectionTimeoutSeconds));
        try
        {
            var data = await load(timeout.Token);
            return new PlayerSupportSection<T>(
                true,
                Source,
                timeProvider.GetUtcNow(),
                null,
                data);
        }
        catch (OperationCanceledException) when (requestCancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("LiveOps player support section {Section} timed out.", section);
            return Unavailable<T>("This section timed out. Retry the snapshot.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "LiveOps player support section {Section} is unavailable.", section);
            return Unavailable<T>("This section is temporarily unavailable.");
        }
    }

    private PlayerSupportSection<T> Unavailable<T>(string message) =>
        new(false, Source, timeProvider.GetUtcNow(), message, default);

    private static GrantDetails? TryReadGrant(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<GrantDetails>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record TargetRow(Guid AccountId, Guid CharacterId);
    private sealed record BalanceRow(
        long Cinders,
        long Soulstones,
        long FateEcho,
        long SigilFragments,
        long GuildFavor,
        long TowerTokens);
    private sealed record InventoryTotals(int RowCount, long Quantity, int UnseenRows);
    private sealed record GrantRow(
        Guid OperationId,
        string DetailsJson,
        string Reason,
        AdministrationRiskLevel RiskLevel,
        DateTimeOffset OccurredAt);
    private sealed record GrantDetails(string ItemBaseId, int Quantity);
    private sealed record TransferRow(
        Guid TransferId,
        PlayerTransferKind Kind,
        Guid SenderAccountId,
        Guid SenderCharacterId,
        string SenderCharacterName,
        Guid RecipientAccountId,
        Guid RecipientCharacterId,
        string RecipientCharacterName,
        string AssetId,
        string AssetName,
        Guid? SourceItemInstanceId,
        Guid? DestinationItemInstanceId,
        long Quantity,
        DateTimeOffset OccurredAtUtc);

    private sealed record TransferCursor(DateTimeOffset OccurredAt, Guid TransferId)
    {
        public static string Encode(DateTimeOffset occurredAt, Guid transferId)
        {
            var value = string.Create(
                CultureInfo.InvariantCulture,
                $"{occurredAt.UtcTicks}:{transferId:N}");
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static bool TryDecode(string? value, out TransferCursor? cursor)
        {
            cursor = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            try
            {
                var normalized = value.Replace('-', '+').Replace('_', '/');
                normalized = normalized.PadRight(
                    normalized.Length + ((4 - normalized.Length % 4) % 4),
                    '=');
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
                var separator = decoded.IndexOf(':');
                if (separator <= 0 ||
                    !long.TryParse(decoded[..separator], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                    !Guid.TryParseExact(decoded[(separator + 1)..], "N", out var transferId))
                {
                    return false;
                }
                cursor = new TransferCursor(new DateTimeOffset(ticks, TimeSpan.Zero), transferId);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
