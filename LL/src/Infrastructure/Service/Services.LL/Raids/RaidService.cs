using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Raids;
using Application.UseCases.Raids.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Quests;
using Domain.Models.Raids;
using Common.Randomness;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.LL.Interfaces;
using Services.LL.PowerRatings;

namespace Services.LL.Raids;

public sealed class RaidService(
    IDbContext db,
    IRaidBossDefinitionProvider definitions,
    IRaidTrophyVendorCatalog trophyVendor,
    IRaidPowerRecommendationStore raidPowerRecommendations,
    ICharacterSnapshotService snapshots,
    IPowerRatingService powerRatings,
    IInventoryService inventory,
    IInventoryItemFactory inventoryItemFactory,
    IItemBaseRepository itemBases,
    IRaidCombatResolver combatResolver,
    IRaidPlaybackBundleBuilder playbackBundles,
    IAchievementService achievements,
    IGameEventOutbox outbox,
    IStateSyncService stateSync,
    IMemoryCache memoryCache,
    TimeProvider timeProvider,
    JsonSerializerOptions jsonOptions,
    IRaidDevelopmentRosterFactory developmentRosters,
    IOptions<RaidOptions> options,
    ILogger<RaidService> logger) : IRaidService
{
    private const int MaximumOpenRaidsPerBoss = 20;
    private const int BattlePlanSampleCount = 10;
    private const int BattlePlanHourlyLimit = 30;
    private const string RealmFirstRaidTitleKey = "title.realm_first_raider";
    private static readonly TimeSpan PlaybackTransitionDelay = TimeSpan.FromMilliseconds(1500);
    private static readonly RaidRunStatus[] ActiveStatuses =
        [RaidRunStatus.Mustering, RaidRunStatus.Resolving, RaidRunStatus.Playback];

    public async Task<IReadOnlyList<RaidBossSummaryDto>> GetRaidBossesAsync(
        Guid characterId,
        int? region,
        CancellationToken cancellationToken)
    {
        var character = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Level })
            .SingleOrDefaultAsync(cancellationToken);
        if (character is null)
            return [];

        var activeRaidId = await db.RaidSignups.AsNoTracking()
            .Where(x => x.CharacterId == characterId && ActiveStatuses.Contains(x.RaidRun.Status))
            .Select(x => (Guid?)x.RaidRunId)
            .FirstOrDefaultAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var weekKey = GetWeekKey(now);
        var reducedBossIds = await db.RaidRewardClaims.AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.WeekKey == weekKey)
            .Select(x => x.RaidBossId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var openCounts = await db.RaidRuns.AsNoTracking()
            .Where(x => x.Status == RaidRunStatus.Mustering && x.SignupClosesAt > now)
            .GroupBy(x => x.RaidBossId)
            .Select(x => new { RaidBossId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RaidBossId, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var selected = definitions.GetAll().Where(x =>
            !region.HasValue
            || (x.Regions.Count == 0 ? [x.Region] : x.Regions).Contains(region.Value)).ToArray();
        var unlockErrors = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var boss in selected)
            unlockErrors[boss.Id] = await GetBossUnlockErrorAsync(characterId, character.Level, boss, cancellationToken);
        return selected.Select(boss =>
        {
            var unlockError = unlockErrors[boss.Id];
            var unlocked = unlockError is null;
            return new RaidBossSummaryDto(
                boss.Id,
                boss.Name,
                boss.Region,
                boss.Regions.Count == 0 ? [boss.Region] : boss.Regions,
                boss.LevelRequirement,
                boss.ImagePath,
                unlocked,
                unlockError,
                openCounts.GetValueOrDefault(boss.Id),
                reducedBossIds.Contains(boss.Id, StringComparer.OrdinalIgnoreCase),
                activeRaidId,
                boss.Tiers.Select(tier => new RaidBossTierSummaryDto(
                    tier.Tier,
                    tier.LaneSlots,
                    tier.MinimumRoster,
                    tier.SignupWindowHours,
                    ToPowerDto(boss.Id, tier))).ToArray(),
                options.Value.DevelopmentToolsEnabled);
        }).ToArray();
    }

    private RaidRecommendedWingPowerDto ToPowerDto(string raidBossId, RaidBossTierDefinition tier)
    {
        if (raidPowerRecommendations.TryGet(raidBossId, tier.Tier, out var recommendation))
        {
            return new RaidRecommendedWingPowerDto(
                recommendation.Vanguard.RecommendedPower,
                recommendation.Flank.RecommendedPower,
                recommendation.Ward.RecommendedPower,
                recommendation.Vanguard.LowerRecommendedPower,
                recommendation.Vanguard.UpperRecommendedPower,
                recommendation.Flank.LowerRecommendedPower,
                recommendation.Flank.UpperRecommendedPower,
                recommendation.Ward.LowerRecommendedPower,
                recommendation.Ward.UpperRecommendedPower,
                recommendation.Confidence,
                true);
        }

        var authored = tier.RecommendedWingPower;
        return new RaidRecommendedWingPowerDto(
            authored.Vanguard,
            authored.Flank,
            authored.Ward,
            authored.Vanguard,
            authored.Vanguard,
            authored.Flank,
            authored.Flank,
            authored.Ward,
            authored.Ward,
            PowerRatingConfidence.Low,
            false);
    }

    public async Task<IReadOnlyList<RaidRunSummaryDto>> GetOpenRaidsAsync(
        Guid characterId,
        string raidBossId,
        CancellationToken cancellationToken)
    {
        var boss = definitions.Get(raidBossId);
        if (boss is null)
            return [];
        var character = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Level, x.UserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (character is null)
            return [];
        var unlockError = await GetBossUnlockErrorAsync(characterId, character.Level, boss, cancellationToken);
        var hasActive = await HasActiveRaidAsync(characterId, null, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var runs = await db.RaidRuns.AsNoTracking()
            .Include(x => x.Signups)
            .Where(x => x.RaidBossId == boss.Id && x.Status == RaidRunStatus.Mustering && x.SignupClosesAt > now)
            .OrderBy(x => x.SignupClosesAt)
            .Take(MaximumOpenRaidsPerBoss)
            .ToArrayAsync(cancellationToken);
        return runs.Select(run => ToSummary(
            run,
            boss,
            !hasActive
            && unlockError is null
            && !run.Signups.Any(x => x.AccountId == character.UserId)
            && run.Signups.Count < ResolvePinnedTier(run).LaneSlots * 3)).ToArray();
    }

    public async Task<IReadOnlyList<RaidHistoryEntryDto>> GetHistoryAsync(
        Guid characterId,
        string? raidBossId,
        int take,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 50);
        raidBossId = string.IsNullOrWhiteSpace(raidBossId) ? null : raidBossId.Trim();

        var query = db.RaidRewardClaims.AsNoTracking()
            .Where(x =>
                x.CharacterId == characterId &&
                x.RaidRun.Outcome.HasValue &&
                (x.RaidRun.Status == RaidRunStatus.Resolved || x.RaidRun.Status == RaidRunStatus.Settled));
        if (raidBossId is not null)
            query = query.Where(x => x.RaidBossId == raidBossId);

        var rows = await query
            .OrderBy(x => x.ClaimedAt.HasValue)
            .ThenByDescending(x => x.RaidRun.ResolvedAt ?? x.CreatedAt)
            .Take(limit)
            .Select(x => new
            {
                x.RaidRunId,
                x.RaidBossId,
                x.RaidRun.Tier,
                x.RaidRun.Outcome,
                x.RaidRun.ResolvedAt,
                x.Trophies,
                x.WasReduced,
                x.ClaimedAt,
                x.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return rows.Select(x => new RaidHistoryEntryDto(
            x.RaidRunId,
            x.RaidBossId,
            definitions.Get(x.RaidBossId)?.Name ?? x.RaidBossId,
            x.Tier,
            x.Outcome!.Value,
            x.ResolvedAt ?? x.CreatedAt,
            x.Trophies,
            x.WasReduced,
            x.ClaimedAt,
            !x.ClaimedAt.HasValue)).ToArray();
    }

    public async Task<RaidRunDto?> GetRaidAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken)
    {
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        return run is null || run.Signups.All(x => x.CharacterId != characterId)
            ? null
            : await ToDtoAsync(run, characterId, cancellationToken);
    }

    public async Task<RaidRunDto?> GetActiveRaidAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var raidRunId = await db.RaidSignups.AsNoTracking()
            .Where(x => x.CharacterId == characterId && ActiveStatuses.Contains(x.RaidRun.Status))
            .OrderByDescending(x => x.SignedUpAt)
            .Select(x => (Guid?)x.RaidRunId)
            .FirstOrDefaultAsync(cancellationToken);
        return raidRunId.HasValue
            ? await GetRaidAsync(characterId, raidRunId.Value, cancellationToken)
            : null;
    }

    public Task<RaidOperationResult<RaidRunDto>> CreateAsync(
        Guid characterId,
        string raidBossId,
        int tierNumber,
        CancellationToken cancellationToken) =>
        CreateCoreAsync(
            characterId,
            raidBossId,
            tierNumber,
            requirePriorTierSlay: true,
            cancellationToken: cancellationToken);

    public Task<RaidOperationResult<RaidRunDto>> CreateDevelopmentAsync(
        Guid characterId,
        string raidBossId,
        int tierNumber,
        CancellationToken cancellationToken)
    {
        if (!options.Value.DevelopmentToolsEnabled)
        {
            return Task.FromResult(
                RaidOperationResult<RaidRunDto>.Fail("Raid development tools are disabled."));
        }

        return CreateCoreAsync(
            characterId,
            raidBossId,
            tierNumber,
            requirePriorTierSlay: false,
            cancellationToken: cancellationToken);
    }

    private async Task<RaidOperationResult<RaidRunDto>> CreateCoreAsync(
        Guid characterId,
        string raidBossId,
        int tierNumber,
        bool requirePriorTierSlay,
        CancellationToken cancellationToken)
    {
        var boss = definitions.Get(raidBossId);
        var tier = boss?.Tiers.SingleOrDefault(x => x.Tier == tierNumber);
        if (boss is null || tier is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid boss or tier was not found.");
        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        var eligibility = await GetEligibilityAsync(characterId, boss, null, cancellationToken);
        if (eligibility.Error is not null)
            return RaidOperationResult<RaidRunDto>.Fail(eligibility.Error);
        var previousTier = boss.Tiers.Where(x => x.Tier < tier.Tier)
            .OrderByDescending(x => x.Tier)
            .FirstOrDefault();
        if (requirePriorTierSlay
            && previousTier is not null
            && !await db.RaidParticipantResults.AsNoTracking().AnyAsync(
                x => x.CharacterId == characterId
                     && x.RaidRun.RaidBossId == boss.Id
                     && x.RaidRun.Tier == previousTier.Tier
                     && x.RaidRun.Outcome == RaidOutcome.Slain
                     && (x.RaidRun.Status == RaidRunStatus.Resolved
                         || x.RaidRun.Status == RaidRunStatus.Settled),
                cancellationToken))
            return RaidOperationResult<RaidRunDto>.Fail(
                $"Slay Tier {previousTier.Tier} of this raid boss before leading Tier {tier.Tier}.");
        if (await db.RaidRuns.AnyAsync(x => x.LeaderCharacterId == characterId && ActiveStatuses.Contains(x.Status), cancellationToken))
            return RaidOperationResult<RaidRunDto>.Fail("This character already leads an active raid.");
        if (await db.RaidRuns.CountAsync(x => x.RaidBossId == boss.Id && x.Status == RaidRunStatus.Mustering, cancellationToken) >= MaximumOpenRaidsPerBoss)
            return RaidOperationResult<RaidRunDto>.Fail("This raid boss already has the maximum number of recruiting raids.");

        var snapshot = await snapshots.CreateAsync(characterId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var definitionJson = JsonSerializer.Serialize(tier, jsonOptions);
        var run = new RaidRun
        {
            RaidBossId = boss.Id,
            Tier = tier.Tier,
            DefinitionHash = HashDefinition(definitionJson),
            DefinitionSnapshotJson = definitionJson,
            LeaderCharacterId = characterId,
            Status = RaidRunStatus.Mustering,
            CreatedAt = now,
            SignupClosesAt = now.AddHours(tier.SignupWindowHours),
            WeekKey = GetWeekKey(now),
            RowVersion = 1
        };
        run.Signups.Add(CreateSignup(run, snapshot, eligibility, now));
        db.RaidRuns.Add(run);
        await QueueRaidChatSnapshotAsync(
            run,
            cancellationToken,
            $"Raid channel opened. {eligibility.CharacterName} is leading the raid.",
            "opened");
        await QueueRaidUpdateAsync(run, "Created", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidRunDto>> JoinAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken)
    {
        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
        if (run.Status != RaidRunStatus.Mustering || run.SignupClosesAt <= timeProvider.GetUtcNow())
            return RaidOperationResult<RaidRunDto>.Fail("This raid is no longer accepting signups.");
        var boss = definitions.Get(run.RaidBossId);
        if (boss is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid boss content is unavailable.");
        var eligibility = await GetEligibilityAsync(characterId, boss, run.Id, cancellationToken);
        if (eligibility.Error is not null)
            return RaidOperationResult<RaidRunDto>.Fail(eligibility.Error);
        var tier = ResolvePinnedTier(run);
        if (run.Signups.Count >= tier.LaneSlots * 3)
            return RaidOperationResult<RaidRunDto>.Fail("This raid roster is full.");
        if (run.Signups.Any(x => x.AccountId == eligibility.AccountId))
            return RaidOperationResult<RaidRunDto>.Fail("This account already occupies a slot in this raid.");

        var snapshot = await snapshots.CreateAsync(characterId, cancellationToken);
        var signup = CreateSignup(run, snapshot, eligibility, timeProvider.GetUtcNow());
        run.Signups.Add(signup);
        db.RaidSignups.Add(signup);
        run.RowVersion++;
        await QueueRaidChatSnapshotAsync(
            run,
            cancellationToken,
            $"{eligibility.CharacterName} joined the raid.",
            $"joined:{characterId:N}");
        await QueueRaidUpdateAsync(run, "ParticipantJoined", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidRunDto>> LeaveAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken)
    {
        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
        if (run.Status != RaidRunStatus.Mustering)
            return RaidOperationResult<RaidRunDto>.Fail("A raid can only be left while it is mustering.");
        var signup = run.Signups.SingleOrDefault(x => x.CharacterId == characterId);
        if (signup is null)
            return RaidOperationResult<RaidRunDto>.Fail("This character is not signed up for the raid.");
        if (run.LeaderCharacterId == characterId && run.Signups.Count > 1)
            return RaidOperationResult<RaidRunDto>.Fail("The raid leader cannot leave while other players are signed up.");

        if (run.LeaderCharacterId == characterId)
        {
            run.Status = RaidRunStatus.Cancelled;
            run.CancelledAt = timeProvider.GetUtcNow();
        }
        else
        {
            run.Signups.Remove(signup);
            db.RaidSignups.Remove(signup);
        }
        run.RowVersion++;
        await QueueRaidChatSnapshotAsync(
            run,
            cancellationToken,
            run.Status == RaidRunStatus.Mustering
                ? $"{signup.CharacterName} left the raid."
                : null,
            run.Status == RaidRunStatus.Mustering
                ? $"left:{characterId:N}"
                : null);
        await QueueRaidUpdateAsync(
            run,
            run.Status == RaidRunStatus.Cancelled ? "Cancelled" : "ParticipantLeft",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidRunDto>> CancelAsync(
        Guid characterId,
        Guid raidRunId,
        CancellationToken cancellationToken)
    {
        var transaction = db.CurrentTransaction is null ? await db.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
            await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
            var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
            if (run is null)
                return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
            if (run.LeaderCharacterId != characterId)
                return RaidOperationResult<RaidRunDto>.Fail("Only the raid leader can cancel the raid.");
            if (run.Status == RaidRunStatus.Cancelled)
                return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
            if (run.Status != RaidRunStatus.Mustering)
                return RaidOperationResult<RaidRunDto>.Fail("A raid can only be cancelled while it is mustering.");

            run.Status = RaidRunStatus.Cancelled;
            run.CancelledAt = timeProvider.GetUtcNow();
            run.RowVersion++;
            await QueueRaidChatSnapshotAsync(run, cancellationToken);
            await QueueRaidUpdateAsync(run, "Cancelled", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<RaidOperationResult<RaidRunDto>> TransferLeadershipAsync(
        Guid characterId,
        Guid raidRunId,
        Guid targetCharacterId,
        CancellationToken cancellationToken)
    {
        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
        if (run.LeaderCharacterId != characterId)
            return RaidOperationResult<RaidRunDto>.Fail("Only the raid leader can transfer leadership.");
        if (run.Status != RaidRunStatus.Mustering || run.SignupClosesAt <= timeProvider.GetUtcNow())
            return RaidOperationResult<RaidRunDto>.Fail("Leadership can only be transferred during an open muster.");
        if (targetCharacterId == characterId)
            return RaidOperationResult<RaidRunDto>.Fail("That character is already the raid leader.");
        if (run.Signups.All(x => x.CharacterId != targetCharacterId))
            return RaidOperationResult<RaidRunDto>.Fail("The new leader must be signed up for this raid.");

        run.LeaderCharacterId = targetCharacterId;
        run.RowVersion++;
        await QueueRaidChatSnapshotAsync(run, cancellationToken);
        await QueueRaidUpdateAsync(run, "LeadershipTransferred", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidRunDto>> RefreshSnapshotAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken)
    {
        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null || run.Status != RaidRunStatus.Mustering || run.SignupClosesAt <= timeProvider.GetUtcNow())
            return RaidOperationResult<RaidRunDto>.Fail("The raid is not available for loadout updates.");
        var signup = run.Signups.SingleOrDefault(x => x.CharacterId == characterId);
        if (signup is null)
            return RaidOperationResult<RaidRunDto>.Fail("This character is not signed up for the raid.");
        var rating = await powerRatings.GetCharacterRatingAsync(characterId, cancellationToken);
        if (rating.State != PowerAnalysisState.Available)
            return RaidOperationResult<RaidRunDto>.Fail(rating.StatusMessage ?? "Combat Rating is unavailable.");
        var snapshot = await snapshots.CreateAsync(characterId, cancellationToken);
        signup.CharacterSnapshotId = snapshot.Id;
        signup.CharacterSnapshot = snapshot;
        signup.PowerRating = CombatRatingDisplay.FromRaw(rating.Overall);
        signup.LoadoutHash = rating.BuildFingerprint;
        signup.SnapshotRefreshedAt = timeProvider.GetUtcNow();
        run.RowVersion++;
        await QueueRaidUpdateAsync(run, "LoadoutRefreshed", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidRunDto>> AssignAsync(
        Guid characterId,
        Guid raidRunId,
        Guid targetCharacterId,
        RaidLane lane,
        int slotIndex,
        CancellationToken cancellationToken)
    {
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
        if (run.LeaderCharacterId != characterId)
            return RaidOperationResult<RaidRunDto>.Fail("Only the raid leader can assign wings.");
        if (run.Status != RaidRunStatus.Mustering)
            return RaidOperationResult<RaidRunDto>.Fail("Wing assignments are locked after commencement.");
        if (run.SignupClosesAt <= timeProvider.GetUtcNow())
            return RaidOperationResult<RaidRunDto>.Fail("Wing assignments are locked after the muster closes.");
        if (!Enum.IsDefined(lane))
            return RaidOperationResult<RaidRunDto>.Fail("That raid wing is not valid.");
        var tier = ResolvePinnedTier(run);
        if (slotIndex < 0 || slotIndex >= tier.LaneSlots)
            return RaidOperationResult<RaidRunDto>.Fail("Wing slot is outside the tier limit.");
        var signup = run.Signups.SingleOrDefault(x => x.CharacterId == targetCharacterId);
        if (signup is null)
            return RaidOperationResult<RaidRunDto>.Fail("That character is not signed up for this raid.");
        if (run.Signups.Any(x => x.Id != signup.Id && x.Lane == lane && x.WingSlotIndex == slotIndex))
            return RaidOperationResult<RaidRunDto>.Fail("That wing slot is already occupied.");
        signup.Lane = lane;
        signup.WingSlotIndex = slotIndex;
        run.RowVersion++;
        await QueueRaidUpdateAsync(run, "PartiesUpdated", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidRunDto>> UpdatePartiesAsync(
        Guid characterId,
        Guid raidRunId,
        IReadOnlyList<RaidPartyAssignment> assignments,
        CancellationToken cancellationToken)
    {
        var transaction = db.CurrentTransaction is null
            ? await db.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
            var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
            if (run is null)
                return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
            if (run.LeaderCharacterId != characterId)
                return RaidOperationResult<RaidRunDto>.Fail("Only the raid leader can arrange parties.");
            if (run.Status != RaidRunStatus.Mustering)
                return RaidOperationResult<RaidRunDto>.Fail("Party assignments are locked after commencement.");
            if (run.SignupClosesAt <= timeProvider.GetUtcNow())
                return RaidOperationResult<RaidRunDto>.Fail("Party assignments are locked after the muster closes.");
            if (assignments.Count != run.Signups.Count)
                return RaidOperationResult<RaidRunDto>.Fail("The party layout must include every raid participant.");

            var participantIds = run.Signups.Select(signup => signup.CharacterId).ToHashSet();
            var assignmentIds = assignments.Select(assignment => assignment.CharacterId).ToArray();
            if (assignmentIds.Distinct().Count() != assignmentIds.Length
                || assignmentIds.Any(characterIdToAssign => !participantIds.Contains(characterIdToAssign)))
            {
                return RaidOperationResult<RaidRunDto>.Fail(
                    "The party layout contains a duplicate or unknown participant.");
            }

            var tier = ResolvePinnedTier(run);
            if (assignments.Any(assignment => assignment.Lane.HasValue != assignment.WingSlotIndex.HasValue))
            {
                return RaidOperationResult<RaidRunDto>.Fail(
                    "A participant must have both a party and slot, or neither while benched.");
            }
            if (assignments.Any(assignment => assignment.Lane.HasValue && !Enum.IsDefined(assignment.Lane.Value)))
                return RaidOperationResult<RaidRunDto>.Fail("The party layout contains an invalid raid party.");
            if (assignments.Any(assignment => assignment.WingSlotIndex is < 0
                    || assignment.WingSlotIndex >= tier.LaneSlots))
            {
                return RaidOperationResult<RaidRunDto>.Fail(
                    $"Party slots must be between 1 and {tier.LaneSlots}, or empty for the bench.");
            }

            var occupiedPositions = assignments
                .Where(assignment => assignment.Lane.HasValue)
                .Select(assignment => (assignment.Lane!.Value, assignment.WingSlotIndex!.Value))
                .ToArray();
            if (occupiedPositions.Distinct().Count() != occupiedPositions.Length)
                return RaidOperationResult<RaidRunDto>.Fail("Each raid party slot can hold only one participant.");

            // Clearing first allows two occupied slots to be swapped while preserving the database uniqueness invariant.
            foreach (var signup in run.Signups)
            {
                signup.Lane = null;
                signup.WingSlotIndex = null;
            }
            await db.SaveChangesAsync(cancellationToken);

            var assignmentByCharacter = assignments.ToDictionary(assignment => assignment.CharacterId);
            foreach (var signup in run.Signups)
            {
                var assignment = assignmentByCharacter[signup.CharacterId];
                signup.Lane = assignment.Lane;
                signup.WingSlotIndex = assignment.WingSlotIndex;
            }

            run.RowVersion++;
            await QueueRaidChatSnapshotAsync(run, cancellationToken);
            await QueueRaidUpdateAsync(run, "PartiesUpdated", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return RaidOperationResult<RaidRunDto>.Success(
                await ToDtoAsync(run, characterId, cancellationToken));
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<RaidOperationResult<RaidRunDto>> FillWithDevelopmentCharactersAsync(
        Guid characterId,
        Guid raidRunId,
        CancellationToken cancellationToken)
    {
        if (!options.Value.DevelopmentToolsEnabled)
            return RaidOperationResult<RaidRunDto>.Fail("Raid development tools are disabled.");

        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
        if (run.LeaderCharacterId != characterId)
            return RaidOperationResult<RaidRunDto>.Fail("Only the raid leader can fill a development roster.");
        if (run.Status != RaidRunStatus.Mustering || run.SignupClosesAt <= timeProvider.GetUtcNow())
            return RaidOperationResult<RaidRunDto>.Fail("Only an open raid muster can be filled.");

        var boss = definitions.Get(run.RaidBossId);
        if (boss is null)
            return RaidOperationResult<RaidRunDto>.Fail("Raid boss content is unavailable.");
        var tier = ResolvePinnedTier(run);
        var openSlots = tier.LaneSlots * 3 - run.Signups.Count;
        if (openSlots <= 0)
            return RaidOperationResult<RaidRunDto>.Fail("This raid roster is already full.");

        var occupiedCharacterIds = await db.RaidSignups.AsNoTracking()
            .Where(signup => ActiveStatuses.Contains(signup.RaidRun.Status))
            .Select(signup => signup.CharacterId)
            .ToArrayAsync(cancellationToken);
        var occupiedAccountIds = run.Signups.Select(signup => signup.AccountId).ToArray();
        var candidates = await db.Characters.AsNoTracking()
            .Where(candidate =>
                candidate.User.IsGuest
                && candidate.User.Username.StartsWith("SeedGuest")
                && !occupiedCharacterIds.Contains(candidate.Id)
                && !occupiedAccountIds.Contains(candidate.UserId))
            .OrderBy(candidate => candidate.Name)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.UserId,
                candidate.Name
            })
            .Take(openSlots)
            .ToArrayAsync(cancellationToken);
        if (candidates.Length != openSlots)
        {
            return RaidOperationResult<RaidRunDto>.Fail(
                $"Only {candidates.Length} seeded development character(s) were available for {openSlots} open slot(s). Restart the API with local guest seeding enabled.");
        }

        var developmentPositions = Enum.GetValues<RaidLane>()
            .SelectMany(lane => Enumerable.Range(0, tier.LaneSlots)
                .Select(slotIndex => (Lane: lane, SlotIndex: slotIndex)))
            .ToArray();

        var now = timeProvider.GetUtcNow();
        for (var index = 0; index < candidates.Length; index++)
        {
            var candidate = candidates[index];
            var position = developmentPositions[(run.Signups.Count + index) % developmentPositions.Length];
            var build = developmentRosters.Create(
                candidate.Id,
                candidate.Name,
                boss,
                tier,
                position.Lane,
                position.SlotIndex);
            var signup = new RaidSignup
            {
                RaidRun = run,
                RaidRunId = run.Id,
                CharacterId = candidate.Id,
                AccountId = candidate.UserId,
                CharacterName = candidate.Name,
                CharacterSnapshotId = build.Snapshot.Id,
                CharacterSnapshot = build.Snapshot,
                LoadoutHash = $"development:{tier.Tier}:{position.Lane}:{position.SlotIndex}",
                PowerRating = build.PowerRating,
                Lane = null,
                WingSlotIndex = null,
                SignedUpAt = now.AddTicks(position.SlotIndex + 1),
                SnapshotRefreshedAt = now
            };
            run.Signups.Add(signup);
            db.RaidSignups.Add(signup);
        }

        run.RowVersion++;
        await QueueRaidChatSnapshotAsync(run, cancellationToken);
        await QueueRaidUpdateAsync(run, "DevelopmentRosterFilled", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRunDto>.Success(
            await ToDtoAsync(run, characterId, cancellationToken));
    }

    public async Task<RaidOperationResult<RaidBattlePlanPreviewDto>> PreviewBattlePlanAsync(
        Guid characterId,
        Guid raidRunId,
        CancellationToken cancellationToken)
    {
        var run = await LoadRunAsync(raidRunId, includeSnapshots: true, cancellationToken);
        if (run is null)
            return RaidOperationResult<RaidBattlePlanPreviewDto>.Fail("Raid was not found.");
        if (run.LeaderCharacterId != characterId)
            return RaidOperationResult<RaidBattlePlanPreviewDto>.Fail("Only the raid leader can preview the Battle Plan.");
        if (run.Status != RaidRunStatus.Mustering || run.SignupClosesAt <= timeProvider.GetUtcNow())
            return RaidOperationResult<RaidBattlePlanPreviewDto>.Fail("The Battle Plan is only available during an open muster.");

        var tier = ResolvePinnedTier(run);
        var validation = ValidateBattlePlan(run);
        if (validation is not null)
            return RaidOperationResult<RaidBattlePlanPreviewDto>.Fail(validation);
        if (!TryReserveBattlePlanPreview(characterId, out var retryAfter))
            return RaidOperationResult<RaidBattlePlanPreviewDto>.Fail(
                $"Battle Plan preview limit reached. Try again in {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minutes.");

        var samples = await combatResolver.PreviewAsync(
            run,
            tier,
            BattlePlanSampleCount,
            cancellationToken);
        return RaidOperationResult<RaidBattlePlanPreviewDto>.Success(
            CreateBattlePlanDto(run.Id, samples, timeProvider.GetUtcNow()));
    }

    public async Task<RaidPlaybackDto?> GetPlaybackAsync(
        Guid characterId,
        Guid raidRunId,
        RaidLane lane,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(lane) || !await CanViewPlaybackAsync(characterId, raidRunId, cancellationToken))
            return null;

        return await db.RaidPlaybacks.AsNoTracking()
            .Where(x => x.RaidRunId == raidRunId && x.Lane == lane)
            .Select(x => new RaidPlaybackDto(
                x.RaidRunId,
                x.Lane,
                x.SchemaVersion,
                x.TicksPerSecond,
                x.TicksPerFrame,
                x.TotalTicks,
                x.FrameCount,
                x.BundleHash))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RaidPlaybackBundleContentDto?> GetPlaybackBundleAsync(
        Guid characterId,
        Guid raidRunId,
        RaidLane lane,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(lane) || !await CanViewPlaybackAsync(characterId, raidRunId, cancellationToken))
            return null;

        return await db.RaidPlaybacks.AsNoTracking()
            .Where(x => x.RaidRunId == raidRunId && x.Lane == lane)
            .Join(
                db.RaidPlaybackArtifacts.AsNoTracking(),
                playback => playback.Id,
                artifact => artifact.RaidPlaybackId,
                (playback, artifact) => new RaidPlaybackBundleContentDto(
                    artifact.BundleBytes,
                    playback.BundleContentType,
                    playback.BundleContentEncoding,
                    playback.BundleHash))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RaidOperationResult<RaidRunDto>> CommenceAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken)
    {
        var transaction = db.CurrentTransaction is null ? await db.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
            await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
            var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
            if (run is null)
                return RaidOperationResult<RaidRunDto>.Fail("Raid was not found.");
            if (run.LeaderCharacterId != characterId)
                return RaidOperationResult<RaidRunDto>.Fail("Only the raid leader can commence the raid.");
            if (run.Status == RaidRunStatus.Resolving)
                return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
            var validation = ValidateCommencement(run, ResolvePinnedTier(run));
            if (validation is not null)
                return RaidOperationResult<RaidRunDto>.Fail(validation);
            run.Status = RaidRunStatus.Resolving;
            run.CommencedAt = timeProvider.GetUtcNow();
            run.RowVersion++;
            await QueueRaidUpdateAsync(run, "Commenced", cancellationToken);
            await stateSync.InvalidateWorldScopeAsync(
                StateSyncScopes.Raids,
                "CommenceRaidCommand",
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return RaidOperationResult<RaidRunDto>.Success(await ToDtoAsync(run, characterId, cancellationToken));
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<RaidOperationResult<RaidRewardDto>> ClaimAsync(Guid characterId, Guid raidRunId, CancellationToken cancellationToken)
    {
        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        var raidStatus = await db.RaidRuns.AsNoTracking()
            .Where(x => x.Id == raidRunId)
            .Select(x => (RaidRunStatus?)x.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (raidStatus is not RaidRunStatus.Resolved and not RaidRunStatus.Settled)
            return RaidOperationResult<RaidRewardDto>.Fail(
                "Raid rewards become available after the battle playback concludes.");
        var claim = await db.RaidRewardClaims.SingleOrDefaultAsync(
            x => x.RaidRunId == raidRunId && x.CharacterId == characterId,
            cancellationToken);
        if (claim is null)
            return RaidOperationResult<RaidRewardDto>.Fail("No raid reward is available for this character.");
        if (claim.ClaimedAt.HasValue)
            return RaidOperationResult<RaidRewardDto>.Fail("This raid reward has already been claimed.");
        var character = await db.Characters.SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null)
            return RaidOperationResult<RaidRewardDto>.Fail("Character was not found.");
        var pending = JsonSerializer.Deserialize<IReadOnlyList<RaidPendingItem>>(claim.PendingItemsJson, jsonOptions) ?? [];
        var bases = await itemBases.GetItemBasesByIdsAsync(pending.Select(x => x.ItemId).Distinct().ToArray(), cancellationToken);
        var loot = pending.SelectMany(item =>
        {
            if (!bases.TryGetValue(item.ItemId, out var itemBase))
                throw new InvalidOperationException($"Raid reward item '{item.ItemId}' was not found.");
            return inventoryItemFactory.CreateForQuantity(itemBase, item.Quantity, characterId);
        }).ToList();
        if (loot.Count > 0)
            await inventory.AddItemsToInventory(characterId, loot, ItemAcquisitionSources.RaidReward, raidRunId, cancellationToken);
        character.RaidTrophies = checked(character.RaidTrophies + claim.Trophies);
        claim.ClaimedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidRewardDto>.Success(new RaidRewardDto(
            raidRunId,
            claim.Trophies,
            character.RaidTrophies,
            pending.Select(x => new RaidRewardItemDto(x.ItemId, x.Quantity)).ToArray(),
            claim.WasReduced,
            claim.ClaimedAt.Value));
    }

    public async Task<RaidTrophyVendorDto?> GetTrophyVendorAsync(
        Guid characterId,
        string raidBossId,
        CancellationToken cancellationToken)
    {
        if (definitions.Get(raidBossId) is null)
            return null;
        var character = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.RaidTrophies })
            .SingleOrDefaultAsync(cancellationToken);
        if (character is null)
            return null;

        var items = trophyVendor.GetForBoss(raidBossId);
        var itemIds = items.Select(x => x.Id).ToArray();
        var weekKey = GetWeekKey(timeProvider.GetUtcNow());
        var purchases = await db.RaidTrophyPurchases.AsNoTracking()
            .Where(x => x.CharacterId == characterId && itemIds.Contains(x.VendorItemId))
            .GroupBy(x => x.VendorItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                Lifetime = group.Sum(x => x.Quantity),
                Weekly = group.Where(x => x.WeekKey == weekKey).Sum(x => x.Quantity)
            })
            .ToDictionaryAsync(x => x.ItemId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var highestSlainTier = await db.RaidParticipantResults.AsNoTracking()
            .Where(x => x.CharacterId == characterId
                        && x.RaidRun.RaidBossId == raidBossId
                        && x.RaidRun.Outcome == RaidOutcome.Slain
                        && (x.RaidRun.Status == RaidRunStatus.Resolved
                            || x.RaidRun.Status == RaidRunStatus.Settled))
            .Select(x => (int?)x.RaidRun.Tier)
            .MaxAsync(cancellationToken) ?? 0;

        return new RaidTrophyVendorDto(
            raidBossId,
            character.RaidTrophies,
            items.Select(item =>
            {
                var counts = purchases.GetValueOrDefault(item.Id);
                var weekly = counts?.Weekly ?? 0;
                var lifetime = counts?.Lifetime ?? 0;
                var unlocked = highestSlainTier >= item.RequiredTier;
                var withinWeekly = !item.WeeklyPurchaseLimit.HasValue || weekly < item.WeeklyPurchaseLimit.Value;
                var withinLifetime = !item.LifetimePurchaseLimit.HasValue || lifetime < item.LifetimePurchaseLimit.Value;
                return new RaidTrophyVendorItemDto(
                    item.Id,
                    item.Name,
                    item.Description,
                    item.Category,
                    item.TrophyCost,
                    item.RewardItemId,
                    item.RewardQuantity,
                    item.WeeklyPurchaseLimit,
                    weekly,
                    item.LifetimePurchaseLimit,
                    lifetime,
                    item.RequiredTier,
                    unlocked,
                    unlocked && withinWeekly && withinLifetime && character.RaidTrophies >= item.TrophyCost);
            }).ToArray());
    }

    public async Task<RaidOperationResult<RaidTrophyPurchaseDto>> PurchaseTrophyVendorItemAsync(
        Guid characterId,
        string raidBossId,
        string itemId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (quantity is < 1 or > 99)
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail("Purchase quantity must be between 1 and 99.");
        var item = trophyVendor.Get(itemId);
        if (item is null
            || !item.IsEnabled
            || !item.RaidBossId.Equals(raidBossId, StringComparison.OrdinalIgnoreCase))
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail("Raid Trophy vendor item was not found.");

        await db.AcquireCharacterCommandLockAsync(characterId, cancellationToken);
        var character = await db.Characters.SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null)
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail("Character was not found.");
        var highestSlainTier = await db.RaidParticipantResults.AsNoTracking()
            .Where(x => x.CharacterId == characterId
                        && x.RaidRun.RaidBossId == raidBossId
                        && x.RaidRun.Outcome == RaidOutcome.Slain
                        && (x.RaidRun.Status == RaidRunStatus.Resolved
                            || x.RaidRun.Status == RaidRunStatus.Settled))
            .Select(x => (int?)x.RaidRun.Tier)
            .MaxAsync(cancellationToken) ?? 0;
        if (highestSlainTier < item.RequiredTier)
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail(
                $"Slay Tier {item.RequiredTier} of this raid boss to unlock this item.");

        var weekKey = GetWeekKey(timeProvider.GetUtcNow());
        var lifetimePurchased = await db.RaidTrophyPurchases.AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.VendorItemId == item.Id)
            .SumAsync(x => x.Quantity, cancellationToken);
        var weeklyPurchased = await db.RaidTrophyPurchases.AsNoTracking()
            .Where(x => x.CharacterId == characterId && x.VendorItemId == item.Id && x.WeekKey == weekKey)
            .SumAsync(x => x.Quantity, cancellationToken);
        if (item.LifetimePurchaseLimit.HasValue && lifetimePurchased + quantity > item.LifetimePurchaseLimit.Value)
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail("This purchase exceeds the lifetime limit.");
        if (item.WeeklyPurchaseLimit.HasValue && weeklyPurchased + quantity > item.WeeklyPurchaseLimit.Value)
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail("This purchase exceeds the weekly limit.");

        var totalCost = checked(item.TrophyCost * quantity);
        if (character.RaidTrophies < totalCost)
            return RaidOperationResult<RaidTrophyPurchaseDto>.Fail($"This purchase requires {totalCost} Raid Trophies.");
        var bases = await itemBases.GetItemBasesByIdsAsync([item.RewardItemId], cancellationToken);
        if (!bases.TryGetValue(item.RewardItemId, out var itemBase))
            throw new InvalidOperationException($"Raid Trophy vendor reward '{item.RewardItemId}' was not found.");

        var rewardQuantity = checked(item.RewardQuantity * quantity);
        var now = timeProvider.GetUtcNow();
        await inventory.AddItemsToInventory(
            characterId,
            inventoryItemFactory.CreateForQuantity(itemBase, rewardQuantity, characterId).ToList(),
            ItemAcquisitionSources.RaidTrophyVendor,
            cancellationToken);
        character.RaidTrophies -= totalCost;
        db.RaidTrophyPurchases.Add(new RaidTrophyPurchase
        {
            CharacterId = characterId,
            RaidBossId = raidBossId,
            VendorItemId = item.Id,
            Quantity = quantity,
            TrophiesSpent = totalCost,
            WeekKey = weekKey,
            PurchasedAt = now
        });
        await stateSync.InvalidateCharacterScopeAsync(
            characterId,
            StateSyncScopes.CharacterOverview,
            "RaidTrophyVendorPurchase",
            cancellationToken);
        await stateSync.InvalidateCharacterScopeAsync(
            characterId,
            StateSyncScopes.Inventory,
            "RaidTrophyVendorPurchase",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return RaidOperationResult<RaidTrophyPurchaseDto>.Success(new RaidTrophyPurchaseDto(
            raidBossId,
            item.Id,
            item.RewardItemId,
            rewardQuantity,
            totalCost,
            character.RaidTrophies,
            now));
    }

    public async Task ProcessDueRaidsAsync(string workerId, int batchSize, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expiredIds = await db.RaidRuns.AsNoTracking()
            .Where(x => x.Status == RaidRunStatus.Mustering && x.SignupClosesAt <= now)
            .OrderBy(x => x.SignupClosesAt)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
        foreach (var raidRunId in expiredIds)
            await HandleExpiredAsync(raidRunId, cancellationToken);

        var resolvingIds = await db.RaidRuns.AsNoTracking()
            .Where(x => x.Status == RaidRunStatus.Resolving && (x.SimulationLeaseUntil == null || x.SimulationLeaseUntil <= now))
            .OrderBy(x => x.CommencedAt)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
        foreach (var raidRunId in resolvingIds)
        {
            if (await TryClaimResolutionAsync(raidRunId, workerId, cancellationToken))
                await ResolveClaimedAsync(raidRunId, workerId, cancellationToken);
        }

        var playbackIds = await db.RaidRuns.AsNoTracking()
            .Where(x => x.Status == RaidRunStatus.Playback && x.PlaybackEndsAt <= now)
            .OrderBy(x => x.PlaybackEndsAt)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
        foreach (var raidRunId in playbackIds)
            await FinalizePlaybackAsync(raidRunId, cancellationToken);
    }

    private async Task HandleExpiredAsync(Guid raidRunId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        if (run is null || run.Status != RaidRunStatus.Mustering || run.SignupClosesAt > timeProvider.GetUtcNow())
            return;
        var tier = ResolvePinnedTier(run);
        if (ValidateCommencement(run, tier) is not null)
        {
            run.Status = RaidRunStatus.Cancelled;
            run.CancelledAt = timeProvider.GetUtcNow();
        }
        else
        {
            run.Status = RaidRunStatus.Resolving;
            run.CommencedAt = timeProvider.GetUtcNow();
        }
        run.RowVersion++;
        await stateSync.InvalidateWorldScopeAsync(
            StateSyncScopes.Raids,
            run.Status == RaidRunStatus.Cancelled ? "RaidExpired" : "RaidAutoCommenced",
            cancellationToken);
        await QueueRaidChatSnapshotAsync(run, cancellationToken);
        await QueueRaidUpdateAsync(
            run,
            run.Status == RaidRunStatus.Cancelled ? "Cancelled" : "AutoCommenced",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<bool> TryClaimResolutionAsync(Guid raidRunId, string workerId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var run = await db.RaidRuns.SingleOrDefaultAsync(x => x.Id == raidRunId, cancellationToken);
        if (run is null || run.Status != RaidRunStatus.Resolving || run.SimulationLeaseUntil > now)
            return false;
        run.SimulationLeaseOwner = workerId;
        run.SimulationLeaseUntil = now.AddMinutes(5);
        run.SimulationAttempts++;
        run.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task ResolveClaimedAsync(Guid raidRunId, string workerId, CancellationToken cancellationToken)
    {
        try
        {
            var source = await LoadRunAsync(raidRunId, includeSnapshots: true, cancellationToken)
                ?? throw new InvalidOperationException($"Raid '{raidRunId}' was not found for resolution.");
            var tier = ResolvePinnedTier(source);
            var resolution = await combatResolver.ResolveAsync(source, tier, cancellationToken);
            var playbacks = resolution.PlaybackCaptures
                .Select(capture => playbackBundles.Build(raidRunId, capture))
                .ToArray();
            foreach (var laneResult in resolution.LaneResults)
            {
                var playback = playbacks.Single(x => x.Lane == laneResult.Lane);
                laneResult.PlaybackId = playback.Id;
                laneResult.Playback = playback;
            }

            await using var transaction = await db.BeginTransactionAsync(cancellationToken);
            await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
            var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken)
                ?? throw new InvalidOperationException($"Raid '{raidRunId}' disappeared during resolution.");
            if (run.Status != RaidRunStatus.Resolving || run.SimulationLeaseOwner != workerId)
                return;
            var playbackStartedAt = timeProvider.GetUtcNow();
            run.ReinforcementPenalty = resolution.ReinforcementPenalty;
            run.WardBreak = resolution.WardBreak;
            run.BossHealthRemainingPercent = resolution.BossHealthRemainingPercent;
            run.Outcome = resolution.Outcome;
            run.PlaybackStartedAt = playbackStartedAt;
            run.PlaybackEndsAt = playbackStartedAt.Add(GetPlaybackDuration(playbacks));
            run.Status = RaidRunStatus.Playback;
            run.SimulationLeaseOwner = null;
            run.SimulationLeaseUntil = null;
            run.RowVersion++;
            db.RaidLaneResults.AddRange(resolution.LaneResults);
            db.RaidPlaybacks.AddRange(playbacks);
            db.RaidParticipantResults.AddRange(resolution.ParticipantResults);
            await CreateRewardsAsync(run, tier, resolution, cancellationToken);
            await QueueRaidChatSnapshotAsync(run, cancellationToken);
            await QueueRaidUpdateAsync(run, "PlaybackReady", cancellationToken);
            await stateSync.InvalidateWorldScopeAsync(
                StateSyncScopes.Raids,
                "RaidPlaybackReady",
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await ReleaseLeaseAsync(raidRunId, workerId, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Raid {RaidRunId} resolution failed.", raidRunId);
            await ReleaseLeaseAsync(raidRunId, workerId, CancellationToken.None);
        }
    }

    private async Task FinalizePlaybackAsync(Guid raidRunId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        await db.AcquireRaidRunLockAsync(raidRunId, cancellationToken);
        var run = await LoadRunAsync(raidRunId, includeSnapshots: false, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (run is null
            || run.Status != RaidRunStatus.Playback
            || !run.PlaybackEndsAt.HasValue
            || run.PlaybackEndsAt > now)
            return;

        var isFirstSlain = false;
        if (run.Outcome == RaidOutcome.Slain)
        {
            await db.AcquireRaidBossLockAsync(run.RaidBossId, cancellationToken);
            isFirstSlain = !await db.RaidRuns.AsNoTracking().AnyAsync(
                x => x.Id != run.Id
                     && x.RaidBossId == run.RaidBossId
                     && x.Outcome == RaidOutcome.Slain
                     && (x.Status == RaidRunStatus.Resolved || x.Status == RaidRunStatus.Settled),
                cancellationToken);
        }

        run.ResolvedAt = now;
        run.SettledAt = now;
        run.Status = RaidRunStatus.Settled;
        run.RowVersion++;

        if (run.Outcome == RaidOutcome.Slain)
        {
            if (isFirstSlain)
            {
                foreach (var signup in run.Signups)
                {
                    await achievements.UnlockTitleAsync(
                        signup.AccountId,
                        signup.CharacterId,
                        RealmFirstRaidTitleKey,
                        JsonSerializer.Serialize(new { run.RaidBossId, run.Id }, jsonOptions),
                        cancellationToken);
                }
            }

            await EnqueueSlainAnnouncementAsync(run, isFirstSlain, cancellationToken);
        }

        await QueueRaidChatSnapshotAsync(run, cancellationToken);
        await QueueRaidUpdateAsync(run, "Resolved", cancellationToken);
        await stateSync.InvalidateWorldScopeAsync(
            StateSyncScopes.Raids,
            "RaidResolved",
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static TimeSpan GetPlaybackDuration(IReadOnlyCollection<RaidPlayback> playbacks)
    {
        var combatSeconds = playbacks.Sum(playback =>
            playback.TotalTicks / (double)Math.Max(1, playback.TicksPerSecond));
        return TimeSpan.FromSeconds(combatSeconds)
            .Add(TimeSpan.FromTicks(PlaybackTransitionDelay.Ticks * playbacks.Count));
    }

    private async Task CreateRewardsAsync(
        RaidRun run,
        RaidBossTierDefinition tier,
        RaidCombatResolution resolution,
        CancellationToken cancellationToken)
    {
        var baseTrophies = resolution.Outcome switch
        {
            RaidOutcome.Slain => tier.Rewards.SlainTrophies,
            RaidOutcome.Broken => tier.Rewards.BrokenTrophies,
            RaidOutcome.Wounded => tier.Rewards.WoundedTrophies,
            _ => tier.Rewards.RepelledTrophies
        };
        var outcomeMultiplier = resolution.Outcome switch
        {
            RaidOutcome.Slain => 1m,
            RaidOutcome.Broken => 0.65m,
            RaidOutcome.Wounded => 0.40m,
            _ => 0.20m
        };
        var characterIds = resolution.ParticipantResults.Select(x => x.CharacterId).ToArray();
        var rewardWeekKey = GetWeekKey(timeProvider.GetUtcNow());
        var previouslyRewarded = await db.RaidRewardClaims.AsNoTracking()
            .Where(x => x.RaidBossId == run.RaidBossId && x.WeekKey == rewardWeekKey && characterIds.Contains(x.CharacterId))
            .Select(x => x.CharacterId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        foreach (var participant in resolution.ParticipantResults)
        {
            var reduced = previouslyRewarded.Contains(participant.CharacterId);
            var weeklyMultiplier = reduced ? 0.25m : 1m;
            var trophies = Math.Max(1, (int)Math.Round(baseTrophies * participant.PayoutMultiplier * weeklyMultiplier, MidpointRounding.AwayFromZero));
            var pendingItems = tier.Rewards.GuaranteedItems.Select(item => new RaidPendingItem(
                item.ItemId,
                Math.Max(1, (int)Math.Round(item.Quantity * outcomeMultiplier * participant.PayoutMultiplier * weeklyMultiplier, MidpointRounding.AwayFromZero)))).ToArray();
            db.RaidRewardClaims.Add(new RaidRewardClaim
            {
                RaidRunId = run.Id,
                RaidBossId = run.RaidBossId,
                CharacterId = participant.CharacterId,
                WeekKey = rewardWeekKey,
                Trophies = trophies,
                PendingItemsJson = JsonSerializer.Serialize(pendingItems, jsonOptions),
                CreatedAt = timeProvider.GetUtcNow(),
                WasReduced = reduced
            });
        }
    }

    private async Task ReleaseLeaseAsync(Guid raidRunId, string workerId, CancellationToken cancellationToken)
    {
        var run = await db.RaidRuns.SingleOrDefaultAsync(
            x => x.Id == raidRunId && x.Status == RaidRunStatus.Resolving && x.SimulationLeaseOwner == workerId,
            cancellationToken);
        if (run is null)
            return;
        run.SimulationLeaseOwner = null;
        run.SimulationLeaseUntil = timeProvider.GetUtcNow();
        run.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Eligibility> GetEligibilityAsync(
        Guid characterId,
        RaidBossDefinition boss,
        Guid? targetRaidId,
        CancellationToken cancellationToken)
    {
        var character = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Id, x.UserId, x.Name, x.Level })
            .SingleOrDefaultAsync(cancellationToken);
        if (character is null)
            return Eligibility.Fail("Character was not found.");
        var unlockError = await GetBossUnlockErrorAsync(characterId, character.Level, boss, cancellationToken);
        if (unlockError is not null)
            return Eligibility.Fail(unlockError);
        if (await HasActiveRaidAsync(characterId, targetRaidId, cancellationToken))
            return Eligibility.Fail("This character is already committed to an active raid.");
        if (targetRaidId.HasValue && await db.RaidSignups.AnyAsync(x => x.RaidRunId == targetRaidId && x.AccountId == character.UserId, cancellationToken))
            return Eligibility.Fail("This account already occupies a slot in this raid.");
        var rating = await powerRatings.GetCharacterRatingAsync(characterId, cancellationToken);
        if (rating.State != PowerAnalysisState.Available)
            return Eligibility.Fail(rating.StatusMessage ?? "Combat Rating is unavailable for this character.");
        return new Eligibility(
            character.Id,
            character.UserId,
            character.Name,
            CombatRatingDisplay.FromRaw(rating.Overall),
            rating.BuildFingerprint,
            null);
    }

    private Task<bool> HasActiveRaidAsync(Guid characterId, Guid? targetRaidId, CancellationToken cancellationToken) =>
        db.RaidSignups.AsNoTracking().AnyAsync(
            x => x.CharacterId == characterId
                 && (!targetRaidId.HasValue || x.RaidRunId != targetRaidId.Value)
                 && ActiveStatuses.Contains(x.RaidRun.Status),
            cancellationToken);

    private async Task<string?> GetBossUnlockErrorAsync(
        Guid characterId,
        int characterLevel,
        RaidBossDefinition boss,
        CancellationToken cancellationToken)
    {
        if (characterLevel < boss.LevelRequirement)
            return $"Requires level {boss.LevelRequirement}.";
        if (!string.IsNullOrWhiteSpace(boss.RequiredCompletedQuestId)
            && !await db.CharacterQuestProgresses.AsNoTracking().AnyAsync(
                x => x.CharacterId == characterId
                     && x.QuestId == boss.RequiredCompletedQuestId
                     && x.Status == QuestStatus.Completed,
                cancellationToken))
        {
            return $"Requires completion of quest '{boss.RequiredCompletedQuestId}'.";
        }
        if (boss.RequiredTowerFloor.HasValue
            && !await db.TowerFloorProgresses.AsNoTracking().AnyAsync(
                x => x.FloorNumber >= boss.RequiredTowerFloor.Value && x.IsCleared,
                cancellationToken))
        {
            return $"Requires World Tower floor {boss.RequiredTowerFloor.Value}.";
        }
        return null;
    }

    private static RaidSignup CreateSignup(
        RaidRun run,
        Domain.Models.Snapshots.CharacterSnapshot snapshot,
        Eligibility eligibility,
        DateTimeOffset now) => new()
    {
        RaidRun = run,
        CharacterId = eligibility.CharacterId,
        AccountId = eligibility.AccountId,
        CharacterName = eligibility.CharacterName,
        CharacterSnapshotId = snapshot.Id,
        CharacterSnapshot = snapshot,
        LoadoutHash = eligibility.LoadoutHash,
        PowerRating = eligibility.PowerRating,
        SignedUpAt = now
    };

    private static string? ValidateCommencement(RaidRun run, RaidBossTierDefinition tier)
    {
        if (run.Status != RaidRunStatus.Mustering)
            return "This raid cannot be commenced in its current state.";
        if (run.Signups.Count < tier.MinimumRoster)
            return $"At least {tier.MinimumRoster} characters are required to commence.";
        if (run.Signups.Any(x => !x.Lane.HasValue || !x.WingSlotIndex.HasValue))
            return "Every signup must be assigned to a wing before commencement.";
        if (Enum.GetValues<RaidLane>().Any(lane => run.Signups.All(x => x.Lane != lane)))
            return "Vanguard, Flank, and Ward must each have at least one character.";
        return null;
    }

    private async Task<RaidRun?> LoadRunAsync(Guid raidRunId, bool includeSnapshots, CancellationToken cancellationToken)
    {
        IQueryable<RaidRun> query = db.RaidRuns.AsSplitQuery()
            .Include(x => x.Signups)
            .Include(x => x.LaneResults)
            .Include(x => x.ParticipantResults)
            .Include(x => x.RewardClaims);
        if (includeSnapshots)
        {
            query = query
                .Include(x => x.Signups).ThenInclude(x => x.CharacterSnapshot).ThenInclude(x => x.BaseAttributes)
                .Include(x => x.Signups).ThenInclude(x => x.CharacterSnapshot).ThenInclude(x => x.Equipment).ThenInclude(x => x.InstanceModifiers)
                .Include(x => x.Signups).ThenInclude(x => x.CharacterSnapshot).ThenInclude(x => x.EquippedEssences);
        }
        return await query.SingleOrDefaultAsync(x => x.Id == raidRunId, cancellationToken);
    }

    private async Task<RaidRunDto> ToDtoAsync(RaidRun run, Guid characterId, CancellationToken cancellationToken)
    {
        var boss = definitions.Get(run.RaidBossId);
        var tier = ResolvePinnedTier(run);
        var currentAccountId = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => (Guid?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        var currentSignup = run.Signups.SingleOrDefault(x => x.CharacterId == characterId);
        var currentReward = run.RewardClaims.SingleOrDefault(x => x.CharacterId == characterId);
        var canJoin = run.Status == RaidRunStatus.Mustering
                      && currentSignup is null
                      && currentAccountId.HasValue
                      && run.Signups.All(x => x.AccountId != currentAccountId.Value)
                      && run.Signups.Count < tier.LaneSlots * 3
                      && !await HasActiveRaidAsync(characterId, run.Id, cancellationToken);
        return new RaidRunDto(
            run.Id,
            run.RaidBossId,
            boss?.Name ?? run.RaidBossId,
            boss?.ImagePath ?? string.Empty,
            boss?.Region ?? 1,
            run.Tier,
            run.Status,
            run.LeaderCharacterId,
            run.CreatedAt,
            run.SignupClosesAt,
            run.CommencedAt,
            run.PlaybackStartedAt,
            run.PlaybackEndsAt,
            timeProvider.GetUtcNow(),
            run.ResolvedAt,
            tier.LaneSlots,
            tier.MinimumRoster,
            run.Signups.OrderBy(x => x.Lane).ThenBy(x => x.WingSlotIndex).ThenBy(x => x.SignedUpAt).Select(x => new RaidSignupDto(
                x.CharacterId,
                x.CharacterName,
                x.PowerRating,
                x.Lane,
                x.WingSlotIndex,
                x.SignedUpAt,
                x.SnapshotRefreshedAt,
                x.CharacterId == run.LeaderCharacterId,
                x.CharacterId == characterId)).ToArray(),
            run.LaneResults.OrderBy(x => x.Lane).Select(x => new RaidLaneResultDto(
                x.Lane,
                x.DurationTicks,
                x.BattleOutcome,
                x.TotalFriendlyDamage,
                x.SurvivingHostileHealthFraction,
                x.DerivedModifier,
                x.PlaybackId.HasValue)).ToArray(),
            run.ParticipantResults.OrderBy(x => x.ContributionRank).Select(x => new RaidParticipantResultDto(
                x.CharacterId,
                x.Lane,
                x.DamageDone,
                x.ContributionScore,
                x.PayoutMultiplier,
                x.ContributionRank)).ToArray(),
            run.Outcome,
            run.ReinforcementPenalty,
            run.WardBreak,
            run.BossHealthRemainingPercent,
            canJoin,
            currentSignup is not null
            && run.Status == RaidRunStatus.Mustering
            && (run.LeaderCharacterId != characterId || run.Signups.Count == 1),
            run.LeaderCharacterId == characterId && run.Status == RaidRunStatus.Mustering,
            run.LeaderCharacterId == characterId && ValidateCommencement(run, tier) is null,
            currentSignup is not null && run.Status == RaidRunStatus.Mustering,
            currentReward is { ClaimedAt: null }
            && (run.Status == RaidRunStatus.Resolved || run.Status == RaidRunStatus.Settled),
            currentReward?.WasReduced == true,
            run.LeaderCharacterId == characterId
            && run.Status == RaidRunStatus.Mustering
            && ValidateBattlePlan(run) is null,
            run.LeaderCharacterId == characterId && run.Status == RaidRunStatus.Mustering,
            run.LeaderCharacterId == characterId
            && run.Status == RaidRunStatus.Mustering
            && run.Signups.Count > 1,
            options.Value.DevelopmentToolsEnabled);
    }

    private RaidBossTierDefinition ResolvePinnedTier(RaidRun run) =>
        JsonSerializer.Deserialize<RaidBossTierDefinition>(run.DefinitionSnapshotJson, jsonOptions)
        ?? throw new InvalidOperationException($"Raid '{run.Id}' has an invalid pinned definition.");

    private RaidRunSummaryDto ToSummary(RaidRun run, RaidBossDefinition boss, bool canJoin)
    {
        var tier = ResolvePinnedTier(run);
        var leader = run.Signups.SingleOrDefault(x => x.CharacterId == run.LeaderCharacterId);
        return new RaidRunSummaryDto(
            run.Id,
            run.RaidBossId,
            boss.Name,
            run.Tier,
            run.LeaderCharacterId,
            leader?.CharacterName ?? "Unknown",
            run.Status,
            run.SignupClosesAt,
            run.Signups.Count,
            tier.LaneSlots * 3,
            run.Signups.Count(x => x.Lane == RaidLane.Vanguard),
            run.Signups.Count(x => x.Lane == RaidLane.Flank),
            run.Signups.Count(x => x.Lane == RaidLane.Ward),
            canJoin);
    }

    private async Task<bool> CanViewPlaybackAsync(
        Guid characterId,
        Guid raidRunId,
        CancellationToken cancellationToken) =>
        await db.RaidSignups.AsNoTracking().AnyAsync(
            x => x.RaidRunId == raidRunId
                 && x.CharacterId == characterId
                 && (x.RaidRun.Status == RaidRunStatus.Playback
                     || x.RaidRun.Status == RaidRunStatus.Resolved
                     || x.RaidRun.Status == RaidRunStatus.Settled),
            cancellationToken);

    private bool TryReserveBattlePlanPreview(Guid characterId, out TimeSpan retryAfter)
    {
        var now = timeProvider.GetUtcNow();
        var key = $"raid:battle-plan-rate:{characterId:N}";
        var window = memoryCache.GetOrCreate(key, entry =>
        {
            var created = new BattlePlanRateWindow(now.AddHours(1));
            entry.AbsoluteExpiration = created.ResetsAt;
            entry.Size = 1;
            return created;
        })!;

        lock (window)
        {
            if (window.Count >= BattlePlanHourlyLimit)
            {
                retryAfter = window.ResetsAt - now;
                return false;
            }

            window.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private static string? ValidateBattlePlan(RaidRun run)
    {
        if (run.Signups.Count == 0)
            return "At least one character must be signed up before previewing the Battle Plan.";
        if (run.Signups.Any(x => !x.Lane.HasValue || !x.WingSlotIndex.HasValue))
            return "Assign every signed-up character before previewing the Battle Plan.";
        if (Enum.GetValues<RaidLane>().Any(lane => run.Signups.All(x => x.Lane != lane)))
            return "Vanguard, Flank, and Ward must each have at least one character for a Battle Plan preview.";
        return null;
    }

    private static RaidBattlePlanPreviewDto CreateBattlePlanDto(
        Guid raidRunId,
        IReadOnlyList<RaidCombatResolution> samples,
        DateTimeOffset generatedAt)
    {
        if (samples.Count == 0)
            throw new InvalidOperationException("A Battle Plan preview requires at least one sample.");

        var lanes = Enum.GetValues<RaidLane>().Select(lane =>
        {
            var results = samples.Select(x => x.LaneResults.Single(result => result.Lane == lane)).ToArray();
            var successes = lane == RaidLane.Vanguard
                ? samples.Count(x => x.Outcome == RaidOutcome.Slain)
                : results.Count(x => x.BattleOutcome == Domain.Models.Combat.BattleOutcome.Victory);
            var probability = successes / (decimal)samples.Count;
            var interval = DungeonReadinessService.WilsonInterval(successes, samples.Count);
            var modifiers = results.Select(x => x.DerivedModifier).Order().ToArray();
            return new RaidBattlePlanLaneDto(
                lane,
                ReadinessLabel(probability),
                probability,
                interval.Lower,
                interval.Upper,
                (int)Math.Round(results.Average(x => x.DurationTicks), MidpointRounding.AwayFromZero),
                results.Average(x => x.DerivedModifier),
                modifiers[0],
                modifiers[^1]);
        }).ToArray();
        var slainCount = samples.Count(x => x.Outcome == RaidOutcome.Slain);
        var slainProbability = slainCount / (decimal)samples.Count;
        var slainInterval = DungeonReadinessService.WilsonInterval(slainCount, samples.Count);
        var predictedOutcome = (RaidOutcome)Math.Clamp(
            (int)Math.Round(samples.Average(x => (int)x.Outcome), MidpointRounding.AwayFromZero),
            (int)RaidOutcome.Repelled,
            (int)RaidOutcome.Slain);
        var outcomeCounts = Enum.GetValues<RaidOutcome>()
            .ToDictionary(outcome => outcome, outcome => samples.Count(x => x.Outcome == outcome));

        return new RaidBattlePlanPreviewDto(
            raidRunId,
            generatedAt,
            samples.Count,
            ReadinessLabel(slainProbability),
            predictedOutcome,
            slainProbability,
            slainInterval.Lower,
            slainInterval.Upper,
            lanes,
            outcomeCounts);
    }

    private static string ReadinessLabel(decimal probability) =>
        DungeonReadinessService.GetBand(probability) switch
        {
            DungeonReadinessBand.VeryUnlikely => "Very Unlikely",
            DungeonReadinessBand.Risky => "Risky",
            DungeonReadinessBand.Uncertain => "Uncertain",
            DungeonReadinessBand.Favored => "Favored",
            DungeonReadinessBand.Comfortable => "Comfortable",
            _ => "Uncertain"
        };

    private Task EnqueueSlainAnnouncementAsync(
        RaidRun run,
        bool isFirstSlain,
        CancellationToken cancellationToken)
    {
        var bossName = definitions.Get(run.RaidBossId)?.Name ?? run.RaidBossId;
        var leaderName = run.Signups.SingleOrDefault(x => x.CharacterId == run.LeaderCharacterId)?.CharacterName
            ?? "A raid leader";
        var body = isFirstSlain
            ? $"Realm first! {leaderName}'s raid has slain {bossName}."
            : $"{leaderName}'s raid has slain {bossName}.";
        return outbox.EnqueueAsync(
            GameEventTypes.RaidChatAnnouncement,
            new RaidChatAnnouncementPayload(
                run.Id,
                StableRandom.Guid(
                    "raid-chat-announcement-v1",
                    run.Id.ToString("N"),
                    "slain"),
                body,
                $"/game/world/raid/{run.Id}",
                run.ResolvedAt ?? timeProvider.GetUtcNow()),
            characterId: null,
            accountId: null,
            cancellationToken);
    }

    private Task QueueRaidChatSnapshotAsync(
        RaidRun run,
        CancellationToken cancellationToken,
        string? lifecycleMessage = null,
        string? lifecycleEventKey = null)
    {
        var isOpen = ActiveStatuses.Contains(run.Status);
        var now = timeProvider.GetUtcNow();
        RaidChatLifecycleMessagePayload? message = null;
        if (isOpen
            && !string.IsNullOrWhiteSpace(lifecycleMessage)
            && !string.IsNullOrWhiteSpace(lifecycleEventKey))
        {
            message = new RaidChatLifecycleMessagePayload(
                StableRandom.Guid(
                    "raid-chat-lifecycle-v1",
                    run.Id.ToString("N"),
                    lifecycleEventKey,
                    run.RowVersion.ToString(CultureInfo.InvariantCulture)),
                lifecycleMessage,
                now);
        }

        return outbox.EnqueueAsync(
            GameEventTypes.RaidChatChannelSnapshot,
            new RaidChatChannelSnapshotPayload(
                run.Id,
                run.RowVersion,
                isOpen,
                isOpen
                    ? run.Signups.Select(x => x.CharacterId).Distinct().ToArray()
                    : [],
                now,
                message),
            characterId: null,
            accountId: null,
            cancellationToken);
    }

    private Task QueueRaidUpdateAsync(
        RaidRun run,
        string eventName,
        CancellationToken cancellationToken) =>
        outbox.EnqueueAsync(
            GameEventTypes.RaidUpdated,
            new RaidUpdated(
                run.Id,
                run.RaidBossId,
                eventName,
                run.Status.ToString(),
                run.Signups.Count,
                timeProvider.GetUtcNow()),
            characterId: null,
            accountId: null,
            cancellationToken);

    private static int GetWeekKey(DateTimeOffset value)
    {
        var date = value.UtcDateTime.Date;
        return ISOWeek.GetYear(date) * 100 + ISOWeek.GetWeekOfYear(date);
    }

    private static string HashDefinition(string definitionJson)
    {
        var identity = $"raid-rules:{RaidRules.Version}|power:{PowerRatingAlgorithm.Version}|combat:{PowerRatingAlgorithm.CombatRulesVersion}|{definitionJson}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
    }

    private sealed record Eligibility(
        Guid CharacterId,
        Guid AccountId,
        string CharacterName,
        int PowerRating,
        string LoadoutHash,
        string? Error)
    {
        public static Eligibility Fail(string error) => new(Guid.Empty, Guid.Empty, string.Empty, 0, string.Empty, error);
    }

    private sealed class BattlePlanRateWindow(DateTimeOffset resetsAt)
    {
        public DateTimeOffset ResetsAt { get; } = resetsAt;
        public int Count { get; set; }
    }
}
