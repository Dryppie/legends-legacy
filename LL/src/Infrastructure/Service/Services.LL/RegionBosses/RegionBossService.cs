using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.RegionBosses;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.UseCases.RegionBosses.Dtos;
using Application.WebSockets.Contracts;
using Domain.Models.RegionBosses;
using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services.LL.RegionBosses;

public sealed class RegionBossService(
    IDbContext db,
    IRegionBossDefinitionProvider definitions,
    IPowerRatingService powerRatings,
    IRegionBossCombatResolver combatResolver,
    IRegionBossPlaybackBundleBuilder playbackBundles,
    IGameRealtimeBroadcaster realtime,
    IGameEventOutbox outbox,
    TimeProvider timeProvider,
    JsonSerializerOptions jsonOptions,
    IOptions<RegionBossOptions> options,
    ILogger<RegionBossService> logger) : IRegionBossService
{
    private const int MaximumDevelopmentSignups = 95;
    private static readonly TimeSpan AutomaticSignupActivityWindow = TimeSpan.FromHours(24);
    private static readonly RegionBossEventStatus[] VisibleStatuses =
        [RegionBossEventStatus.Scheduled, RegionBossEventStatus.SignupOpen, RegionBossEventStatus.Matching,
            RegionBossEventStatus.Resolving, RegionBossEventStatus.Playback, RegionBossEventStatus.Settled];

    public async Task<IReadOnlyList<RegionBossStatusDto>> GetStatusAsync(
        Guid characterId,
        int? regionId,
        CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().AddDays(-14);
        var events = await EventQuery(characterId)
            .Where(x => VisibleStatuses.Contains(x.Status)
                && x.EncounterStartsAtUtc >= cutoff
                && (!regionId.HasValue || x.RegionId == regionId.Value))
            .OrderByDescending(x => x.Status != RegionBossEventStatus.Settled)
            .ThenBy(x => x.EncounterStartsAtUtc)
            .Take(20)
            .ToArrayAsync(cancellationToken);
        var level = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => (int?)x.Level)
            .SingleOrDefaultAsync(cancellationToken);
        var output = new List<RegionBossStatusDto>(events.Length);
        foreach (var item in events)
        {
            var unlockError = await GetUnlockErrorAsync(characterId, level, ReadDefinition(item), cancellationToken);
            output.Add(ToDto(item, characterId, unlockError));
        }
        return output;
    }

    public async Task<RegionBossStatusDto?> GetEventAsync(
        Guid characterId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var item = await EventQuery(characterId).SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (item is null)
            return null;
        var level = await db.Characters.AsNoTracking().Where(x => x.Id == characterId)
            .Select(x => (int?)x.Level).SingleOrDefaultAsync(cancellationToken);
        var unlockError = await GetUnlockErrorAsync(characterId, level, ReadDefinition(item), cancellationToken);
        return ToDto(item, characterId, unlockError);
    }

    public async Task<RegionBossOperationResult<RegionBossStatusDto>> SignupAsync(
        Guid characterId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = db.CurrentTransaction is null
            ? await db.BeginTransactionAsync(cancellationToken)
            : null;
        await db.AcquireRegionBossEventLockAsync(eventId, cancellationToken);
        var item = await LoadSignupEventForUpdateAsync(eventId, cancellationToken);
        if (item is null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Region Boss event was not found.");
        var now = timeProvider.GetUtcNow();
        if (item.Status != RegionBossEventStatus.SignupOpen || now < item.SignupStartsAtUtc || now >= item.SignupClosesAtUtc)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Signups are not open for this event.");
        if (item.Signups.Any(x => x.CharacterId == characterId))
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("This character is already signed up.");
        var character = await db.Characters.SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Character was not found.");
        if (item.Signups.Any(x => x.AccountId == character.UserId))
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Only one character per account may enter an event.");
        var definition = ReadDefinition(item);
        var unlockError = await GetUnlockErrorAsync(characterId, character.Level, definition, cancellationToken);
        if (unlockError is not null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail(unlockError);
        var rating = await powerRatings.GetCharacterRatingAsync(characterId, cancellationToken);
        var signup = new RegionBossSignup
        {
            RegionBossEventId = item.Id,
            CharacterId = character.Id,
            AccountId = character.UserId,
            CharacterName = character.Name,
            PowerRating = rating.Overall,
            PowerRatingAlgorithmVersion = rating.AlgorithmVersion,
            SignedUpAtUtc = now
        };
        item.Signups.Add(signup);
        db.RegionBossSignups.Add(signup);
        item.UpdatedAtUtc = now;
        item.RowVersion++;
        await QueueUpdateAsync(item, "SignupChanged", new Audience.World(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return RegionBossOperationResult<RegionBossStatusDto>.Success(
            (await GetEventAsync(characterId, eventId, cancellationToken))!);
    }

    public async Task<RegionBossOperationResult<RegionBossStatusDto>> WithdrawAsync(
        Guid characterId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await using var transaction = db.CurrentTransaction is null
            ? await db.BeginTransactionAsync(cancellationToken)
            : null;
        await db.AcquireRegionBossEventLockAsync(eventId, cancellationToken);
        var item = await LoadSignupEventForUpdateAsync(eventId, cancellationToken);
        if (item is null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Region Boss event was not found.");
        if (item.Status != RegionBossEventStatus.SignupOpen || timeProvider.GetUtcNow() >= item.SignupClosesAtUtc)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("The signup is locked because matching has begun.");
        var signup = item.Signups.SingleOrDefault(x => x.CharacterId == characterId);
        if (signup is null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("This character is not signed up.");
        db.RegionBossSignups.Remove(signup);
        item.Signups.Remove(signup);
        item.UpdatedAtUtc = timeProvider.GetUtcNow();
        item.RowVersion++;
        await QueueUpdateAsync(item, "SignupChanged", new Audience.World(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return RegionBossOperationResult<RegionBossStatusDto>.Success(
            (await GetEventAsync(characterId, eventId, cancellationToken))!);
    }

    public async Task<RegionBossOperationResult<RegionBossClaimResultDto>> ClaimAsync(
        Guid characterId,
        Guid grantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = db.CurrentTransaction is null
            ? await db.BeginTransactionAsync(cancellationToken)
            : null;
        await db.AcquireRegionBossRewardGrantLockAsync(grantId, cancellationToken);
        var grant = await db.RegionBossRewardGrants.SingleOrDefaultAsync(
            x => x.Id == grantId && x.CharacterId == characterId, cancellationToken);
        if (grant is null)
            return RegionBossOperationResult<RegionBossClaimResultDto>.Fail("Reward was not found.");
        if (grant.Status == RegionBossRewardStatus.Claimed)
            return RegionBossOperationResult<RegionBossClaimResultDto>.Fail("Reward has already been claimed.");
        var character = await db.Characters.SingleOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null)
            return RegionBossOperationResult<RegionBossClaimResultDto>.Fail("Character was not found.");
        var reward = JsonSerializer.Deserialize<RegionBossRewardSnapshot>(grant.RewardSnapshotJson, jsonOptions)
            ?? throw new InvalidOperationException("Region Boss reward snapshot is invalid.");
        character.Cinders = checked(character.Cinders + reward.Cinders);
        character.Soulstones = checked(character.Soulstones + reward.Soulstones);
        grant.Status = RegionBossRewardStatus.Claimed;
        grant.ClaimedAtUtc = timeProvider.GetUtcNow();
        await realtime.PublishAsync(
            new Audience.Character(characterId),
            new RegionBossUpdated(grant.RegionBossEventId, grant.RegionBossDefinitionId, "RewardClaimed", "Settled", timeProvider.GetUtcNow()),
            nameof(RegionBossService),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return RegionBossOperationResult<RegionBossClaimResultDto>.Success(new(
            grant.Id, reward.Cinders, reward.Soulstones, character.Cinders, character.Soulstones));
    }

    public async Task<RegionBossOperationResult<RegionBossStatusDto>> SpawnDevelopmentEventAsync(
        Guid characterId,
        int regionId,
        int additionalSignupCount,
        CancellationToken cancellationToken)
    {
        if (!options.Value.DevelopmentToolsEnabled)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Region Boss development tools are disabled.");
        if (regionId <= 0)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("A valid region is required.");
        if (additionalSignupCount is < 0 or > MaximumDevelopmentSignups)
        {
            return RegionBossOperationResult<RegionBossStatusDto>.Fail(
                $"Additional signup count must be between 0 and {MaximumDevelopmentSignups}.");
        }

        var definition = definitions.GetAll()
            .Where(x => x.RegionId == regionId)
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (definition is null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail($"Region {regionId} has no Region Boss definition.");

        await using var transaction = db.CurrentTransaction is null
            ? await db.BeginTransactionAsync(cancellationToken)
            : null;
        await db.AcquireRegionBossScheduleLockAsync(cancellationToken);
        var character = await db.Characters.AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Id, AccountId = x.UserId, x.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (character is null)
            return RegionBossOperationResult<RegionBossStatusDto>.Fail("Character was not found.");

        var guests = await db.Characters.AsNoTracking()
            .Where(x => x.Id != characterId
                && x.UserId != character.AccountId
                && x.User.IsGuest
                && x.User.Username.StartsWith("SeedGuest"))
            .OrderBy(x => x.Name)
            .Take(additionalSignupCount)
            .Select(x => new { x.Id, AccountId = x.UserId, x.Name })
            .ToArrayAsync(cancellationToken);
        if (guests.Length < additionalSignupCount)
        {
            return RegionBossOperationResult<RegionBossStatusDto>.Fail(
                $"Only {guests.Length} of {additionalSignupCount} requested local participants were available. Restart the API with local guest seeding enabled.");
        }

        var now = timeProvider.GetUtcNow();
        var definitionJson = JsonSerializer.Serialize(definition, jsonOptions);
        var item = new RegionBossEvent
        {
            RegionBossDefinitionId = definition.Id,
            RegionId = regionId,
            Status = RegionBossEventStatus.SignupOpen,
            SignupStartsAtUtc = now,
            SignupClosesAtUtc = now.AddSeconds(10),
            EncounterStartsAtUtc = now.AddSeconds(10),
            DefinitionSnapshotJson = definitionJson,
            DefinitionHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(definitionJson))).ToLowerInvariant(),
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.RegionBossEvents.Add(item);

        foreach (var participant in new[] { character }.Concat(guests))
        {
            var rating = await powerRatings.GetCharacterRatingAsync(participant.Id, cancellationToken);
            item.Signups.Add(new RegionBossSignup
            {
                CharacterId = participant.Id,
                AccountId = participant.AccountId,
                CharacterName = participant.Name,
                PowerRating = rating.Overall,
                PowerRatingAlgorithmVersion = rating.AlgorithmVersion,
                SignedUpAtUtc = now
            });
        }

        await EnqueueSignupOpenedChatAnnouncementAsync(item, definition, now, cancellationToken);
        await QueueUpdateAsync(item, "DevelopmentEventSpawned", new Audience.World(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return RegionBossOperationResult<RegionBossStatusDto>.Success(
            (await GetEventAsync(characterId, item.Id, cancellationToken))!);
    }

    public async Task<RegionBossPlaybackDto?> GetPlaybackAsync(
        Guid characterId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        return await db.RegionBossPlaybacks.AsNoTracking()
            .Where(x => x.RegionBossRunId == runId && x.Run.Members.Any(m => m.CharacterId == characterId))
            .Select(x => new RegionBossPlaybackDto(x.RegionBossRunId, x.SchemaVersion, x.TicksPerSecond,
                x.TicksPerFrame, x.TotalTicks, x.FrameCount, x.BundleHash))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RegionBossPlaybackBundleContentDto?> GetPlaybackBundleAsync(
        Guid characterId,
        Guid runId,
        CancellationToken cancellationToken)
    {
        return await db.RegionBossPlaybacks.AsNoTracking()
            .Where(x => x.RegionBossRunId == runId && x.Run.Members.Any(m => m.CharacterId == characterId))
            .Select(x => new RegionBossPlaybackBundleContentDto(
                x.Artifact.BundleBytes, x.BundleContentType, x.BundleContentEncoding, x.BundleHash))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task EnsureScheduledEventsAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        await db.AcquireRegionBossScheduleLockAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var definition in definitions.GetAll())
        {
            var latestEvent = await db.RegionBossEvents
                .Where(x => x.RegionBossDefinitionId == definition.Id
                    && x.Status != RegionBossEventStatus.Cancelled)
                .OrderByDescending(x => x.EncounterStartsAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var json = JsonSerializer.Serialize(definition, jsonOptions);
            if (latestEvent?.EncounterStartsAtUtc > now)
            {
                if (latestEvent.Status == RegionBossEventStatus.Scheduled
                    && latestEvent.EncounterStartsAtUtc > now.AddHours(definition.Schedule.MaximumIntervalHours))
                {
                    var replacementEncounter = NextOccurrence(now, definition.Schedule);
                    latestEvent.RegionId = definition.RegionId;
                    latestEvent.SignupStartsAtUtc = replacementEncounter
                        .AddMinutes(-definition.Schedule.SignupDurationMinutes);
                    latestEvent.SignupClosesAtUtc = replacementEncounter;
                    latestEvent.EncounterStartsAtUtc = replacementEncounter;
                    latestEvent.DefinitionSnapshotJson = json;
                    latestEvent.DefinitionHash = Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
                    latestEvent.UpdatedAtUtc = now;
                    latestEvent.RowVersion++;
                }
                continue;
            }

            var anchor = latestEvent?.EncounterStartsAtUtc ?? now;
            var encounter = NextOccurrence(anchor, definition.Schedule);
            if (encounter <= now)
                encounter = NextOccurrence(now, definition.Schedule);
            var signupStarts = encounter.AddMinutes(-definition.Schedule.SignupDurationMinutes);
            db.RegionBossEvents.Add(new RegionBossEvent
            {
                RegionBossDefinitionId = definition.Id,
                RegionId = definition.RegionId,
                Status = now >= signupStarts ? RegionBossEventStatus.SignupOpen : RegionBossEventStatus.Scheduled,
                SignupStartsAtUtc = signupStarts,
                SignupClosesAtUtc = encounter,
                EncounterStartsAtUtc = encounter,
                DefinitionSnapshotJson = json,
                DefinitionHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant(),
                MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
                CombatRulesVersion = RegionBossRules.Version,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ProgressEventsAsync(string workerId, CancellationToken cancellationToken)
    {
        await EnsureScheduledEventsAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var ids = await db.RegionBossEvents.AsNoTracking()
            .Where(x => x.Status != RegionBossEventStatus.Settled && x.Status != RegionBossEventStatus.Cancelled
                && (x.SignupStartsAtUtc <= now || x.SignupClosesAtUtc <= now || x.PlaybackEndsAtUtc <= now))
            .OrderBy(x => x.EncounterStartsAtUtc)
            .Select(x => x.Id)
            .Take(10)
            .ToArrayAsync(cancellationToken);
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProgressEventAsync(id, workerId, cancellationToken);
        }
    }

    private async Task ProgressEventAsync(Guid eventId, string workerId, CancellationToken cancellationToken)
    {
        RegionBossEvent? item;
        await using (var transaction = await db.BeginTransactionAsync(cancellationToken))
        {
            await db.AcquireRegionBossEventLockAsync(eventId, cancellationToken);
            item = db.RegionBossEvents.Local.SingleOrDefault(x => x.Id == eventId);
            if (item is not null)
            {
                // EnsureScheduledEventsAsync runs in this same scoped DbContext and
                // may have tracked the event before a signup request committed.
                // Refresh after taking the event lock so RowVersion and status are
                // current before the progression worker mutates the row.
                var entry = db.GetEntry(item);
                await entry.ReloadAsync(cancellationToken);
                await entry.Collection(x => x.Signups).LoadAsync(cancellationToken);
                await entry.Collection(x => x.Runs).LoadAsync(cancellationToken);
            }
            else
            {
                item = await db.RegionBossEvents.Include(x => x.Signups).Include(x => x.Runs)
                    .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
            }
            if (item is null)
                return;
            var now = timeProvider.GetUtcNow();
            var initialStatus = item.Status;
            if (item.Status == RegionBossEventStatus.Scheduled && now >= item.SignupStartsAtUtc)
            {
                item.Status = RegionBossEventStatus.SignupOpen;
                await AddAutomaticSignupsAsync(item, now, cancellationToken);
                if (now < item.SignupClosesAtUtc)
                {
                    await EnqueueSignupOpenedChatAnnouncementAsync(
                        item,
                        ReadDefinition(item),
                        now,
                        cancellationToken);
                }
            }
            if (item.Status == RegionBossEventStatus.SignupOpen && now >= item.SignupClosesAtUtc)
            {
                item.Status = RegionBossEventStatus.Matching;
                await RefreshPowerRatingsAsync(item.Signups, cancellationToken);
                var assignments = CreateRuns(item);
                if (assignments.Count > 0)
                {
                    // Existing signup rows cannot reference new run rows until the runs
                    // have been inserted. Keep both saves inside this transaction.
                    await db.SaveChangesAsync(cancellationToken);
                    foreach (var assignment in assignments)
                    {
                        assignment.Signup.Run = assignment.Run;
                        assignment.Signup.RegionBossRunId = assignment.Run.Id;
                        assignment.Signup.PartySlot = assignment.PartySlot;
                    }
                }
                item.Status = RegionBossEventStatus.Resolving;
            }
            if (item.Status != initialStatus)
            {
                item.UpdatedAtUtc = now;
                item.RowVersion++;
                await QueueUpdateAsync(item, "Progressed", new Audience.World(), cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }

        var runIds = await db.RegionBossRuns.AsNoTracking()
            .Where(x => x.RegionBossEventId == eventId
                && (x.Status == RegionBossRunStatus.Queued
                    || (x.Status == RegionBossRunStatus.Errored && x.SimulationAttempts < 3)
                    || (x.Status == RegionBossRunStatus.Resolving && x.SimulationLeaseUntil < timeProvider.GetUtcNow())))
            .Select(x => x.Id).ToArrayAsync(cancellationToken);
        foreach (var runId in runIds)
            await ResolveRunAsync(runId, workerId, cancellationToken);

        await using var finalTransaction = await db.BeginTransactionAsync(cancellationToken);
        await db.AcquireRegionBossEventLockAsync(eventId, cancellationToken);
        // Every preceding phase has been saved. Discard its tracked graph only
        // after taking the event lock so this transaction reads the current
        // event and run concurrency tokens instead of reusing stale instances.
        db.ClearTrackedEntities();
        item = await db.RegionBossEvents.Include(x => x.Runs).ThenInclude(x => x.Members)
            .Include(x => x.RewardGrants).SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        if (item is null)
            return;
        var current = timeProvider.GetUtcNow();
        var finalInitialStatus = item.Status;
        if (item.Status == RegionBossEventStatus.Resolving
            && item.Runs.All(x => x.Status is RegionBossRunStatus.Ready or RegionBossRunStatus.Errored))
        {
            item.Status = RegionBossEventStatus.Playback;
            item.PlaybackStartsAtUtc = current;
            var longestTicks = item.Runs.Where(x => x.Status == RegionBossRunStatus.Ready)
                .Select(x => x.DurationTicks).DefaultIfEmpty(0).Max();
            item.PlaybackEndsAtUtc = current.AddSeconds(Math.Max(10, longestTicks / 10d));
            foreach (var run in item.Runs.Where(x => x.Status == RegionBossRunStatus.Ready))
            {
                run.PlaybackStartsAtUtc = item.PlaybackStartsAtUtc;
                run.PlaybackEndsAtUtc = item.PlaybackEndsAtUtc;
                run.RowVersion++;
            }
            if (item.Runs.Any(x => x.Status == RegionBossRunStatus.Ready))
            {
                await EnqueueFightStartedChatAnnouncementAsync(
                    item,
                    ReadDefinition(item),
                    current,
                    cancellationToken);
            }
        }
        if (item.Status == RegionBossEventStatus.Playback && current >= item.PlaybackEndsAtUtc)
            Settle(item, current);
        if (item.Status != finalInitialStatus)
        {
            item.UpdatedAtUtc = current;
            item.RowVersion++;
            await QueueUpdateAsync(item, "Progressed", new Audience.World(), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        await finalTransaction.CommitAsync(cancellationToken);
    }

    private IReadOnlyList<PendingRunAssignment> CreateRuns(RegionBossEvent item)
    {
        if (item.Runs.Count != 0)
            return [];

        var assignments = new List<PendingRunAssignment>(item.Signups.Count);
        foreach (var party in RegionBossMatchmaker.Match(item.Id, item.Signups.ToArray()))
        {
            var run = new RegionBossRun
            {
                RegionBossEventId = item.Id,
                Event = item,
                PartyNumber = party.PartyNumber,
                PartySize = party.Members.Count,
                MatchmakingBand = party.MatchmakingBand,
                PartySizeScalingVersion = RegionBossRules.PartySizeScalingVersion,
                RandomSeed = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes($"region-boss-run-v1:{item.Id:N}:{party.PartyNumber}"))),
                Status = RegionBossRunStatus.Queued
            };
            db.RegionBossRuns.Add(run);
            for (var slot = 0; slot < party.Members.Count; slot++)
                assignments.Add(new PendingRunAssignment(party.Members[slot], run, slot));
        }

        return assignments;
    }

    private sealed record PendingRunAssignment(
        RegionBossSignup Signup,
        RegionBossRun Run,
        int PartySlot);

    private async Task ResolveRunAsync(Guid runId, string workerId, CancellationToken cancellationToken)
    {
        RegionBossRun run;
        RegionBossDefinition definition;
        await using (var claimTransaction = await db.BeginTransactionAsync(cancellationToken))
        {
            await db.AcquireRegionBossRunLockAsync(runId, cancellationToken);
            db.ClearTrackedEntities();
            run = await db.RegionBossRuns.Include(x => x.Event).Include(x => x.Members)
                .SingleAsync(x => x.Id == runId, cancellationToken);
            var claimedAt = timeProvider.GetUtcNow();
            var canClaim = run.Status == RegionBossRunStatus.Queued
                || (run.Status == RegionBossRunStatus.Errored && run.SimulationAttempts < 3)
                || (run.Status == RegionBossRunStatus.Resolving
                    && (!run.SimulationLeaseUntil.HasValue || run.SimulationLeaseUntil <= claimedAt));
            if (!canClaim)
                return;
            run.Status = RegionBossRunStatus.Resolving;
            run.StartedAtUtc ??= claimedAt;
            run.SimulationAttempts++;
            run.SimulationLeaseOwner = workerId;
            run.SimulationLeaseUntil = claimedAt.AddMinutes(5);
            run.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await claimTransaction.CommitAsync(cancellationToken);
            definition = ReadDefinition(run.Event);
        }

        try
        {
            var resolution = await combatResolver.ResolveAsync(run, definition, cancellationToken);
            var playback = playbackBundles.Build(run.Id, resolution);
            await using var persistTransaction = await db.BeginTransactionAsync(cancellationToken);
            await db.AcquireRegionBossRunLockAsync(runId, cancellationToken);
            db.ClearTrackedEntities();
            var tracked = await db.RegionBossRuns.Include(x => x.ParticipantResults)
                .SingleAsync(x => x.Id == runId, cancellationToken);
            if (tracked.Status != RegionBossRunStatus.Resolving
                || !string.Equals(tracked.SimulationLeaseOwner, workerId, StringComparison.Ordinal))
            {
                return;
            }
            tracked.HighestLevelDefeated = resolution.HighestLevelDefeated;
            tracked.CurrentBossLevel = resolution.CurrentBossLevel;
            tracked.CurrentBossHealthRemaining = resolution.CurrentBossHealthRemaining;
            tracked.CurrentBossMaxHealth = resolution.CurrentBossMaxHealth;
            tracked.CurrentBossProgressBasisPoints = resolution.CurrentBossProgressBasisPoints;
            tracked.DurationTicks = resolution.DurationTicks;
            tracked.FuryStacksAtEnd = resolution.FuryStacks;
            tracked.TerminationReason = resolution.TerminationReason;
            tracked.ResolvedAtUtc = timeProvider.GetUtcNow();
            tracked.Status = RegionBossRunStatus.Ready;
            tracked.SimulationLeaseOwner = null;
            tracked.SimulationLeaseUntil = null;
            tracked.ParticipantResults.Clear();
            foreach (var participant in resolution.ParticipantResults)
                tracked.ParticipantResults.Add(participant);
            tracked.Playback = playback;
            tracked.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await persistTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Region Boss run {RunId} resolution failed.", runId);
            await using var errorTransaction = await db.BeginTransactionAsync(cancellationToken);
            await db.AcquireRegionBossRunLockAsync(runId, cancellationToken);
            db.ClearTrackedEntities();
            var tracked = await db.RegionBossRuns.SingleAsync(x => x.Id == runId, cancellationToken);
            if (tracked.Status != RegionBossRunStatus.Resolving
                || !string.Equals(tracked.SimulationLeaseOwner, workerId, StringComparison.Ordinal))
            {
                return;
            }
            tracked.Status = tracked.SimulationAttempts >= 3 ? RegionBossRunStatus.Errored : RegionBossRunStatus.Queued;
            tracked.LastError = exception.ToString()[..Math.Min(exception.ToString().Length, 4000)];
            tracked.SimulationLeaseOwner = null;
            tracked.SimulationLeaseUntil = null;
            tracked.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await errorTransaction.CommitAsync(cancellationToken);
        }
    }

    private async Task RefreshPowerRatingsAsync(
        IEnumerable<RegionBossSignup> signups,
        CancellationToken cancellationToken)
    {
        foreach (var signup in signups)
        {
            var rating = await powerRatings.GetCharacterRatingAsync(signup.CharacterId, cancellationToken);
            signup.PowerRating = rating.Overall;
            signup.PowerRatingAlgorithmVersion = rating.AlgorithmVersion;
        }
    }

    private async Task AddAutomaticSignupsAsync(
        RegionBossEvent item,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var definition = ReadDefinition(item);
        if (definition.RequiredTowerFloor.HasValue
            && !await db.TowerFloorProgresses.AsNoTracking().AnyAsync(
                progress => progress.FloorNumber >= definition.RequiredTowerFloor.Value
                    && progress.IsCleared,
                cancellationToken))
        {
            return;
        }

        var activityCutoff = now.Subtract(AutomaticSignupActivityWindow);
        var candidates = await db.CharacterActions.AsNoTracking()
            .Where(action => action.UpdatedAt >= activityCutoff)
            .Join(
                db.Characters.AsNoTracking()
                    .Where(character => character.Level >= definition.LevelRequirement),
                action => action.CharacterId,
                character => character.Id,
                (action, character) => new AutomaticSignupCandidate(
                    character.Id,
                    character.UserId,
                    character.Name,
                    action.UpdatedAt))
            .ToArrayAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(definition.RequiredCompletedQuestId))
        {
            var candidateIds = candidates.Select(candidate => candidate.CharacterId).ToArray();
            var completedQuestCharacterIds = await db.CharacterQuestProgresses.AsNoTracking()
                .Where(progress => candidateIds.Contains(progress.CharacterId)
                    && progress.QuestId == definition.RequiredCompletedQuestId
                    && progress.Status == QuestStatus.Completed)
                .Select(progress => progress.CharacterId)
                .ToHashSetAsync(cancellationToken);
            candidates = candidates
                .Where(candidate => completedQuestCharacterIds.Contains(candidate.CharacterId))
                .ToArray();
        }

        var signedCharacterIds = item.Signups.Select(signup => signup.CharacterId).ToHashSet();
        var signedAccountIds = item.Signups.Select(signup => signup.AccountId).ToHashSet();
        var selectedCandidates = candidates
            .Where(candidate => !signedCharacterIds.Contains(candidate.CharacterId)
                && !signedAccountIds.Contains(candidate.AccountId))
            .GroupBy(candidate => candidate.AccountId)
            .Select(group => group
                .OrderByDescending(candidate => candidate.LastActivityAtUtc)
                .ThenBy(candidate => candidate.CharacterId)
                .First())
            .ToArray();

        foreach (var candidate in selectedCandidates)
        {
            try
            {
                var rating = await powerRatings.GetCharacterRatingAsync(
                    candidate.CharacterId,
                    cancellationToken);
                var signup = new RegionBossSignup
                {
                    RegionBossEventId = item.Id,
                    CharacterId = candidate.CharacterId,
                    AccountId = candidate.AccountId,
                    CharacterName = candidate.CharacterName,
                    PowerRating = rating.Overall,
                    PowerRatingAlgorithmVersion = rating.AlgorithmVersion,
                    SignedUpAtUtc = now
                };
                item.Signups.Add(signup);
                db.RegionBossSignups.Add(signup);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Could not automatically sign up character {CharacterId} for Region Boss event {EventId}.",
                    candidate.CharacterId,
                    item.Id);
            }
        }
    }

    private sealed record AutomaticSignupCandidate(
        Guid CharacterId,
        Guid AccountId,
        string CharacterName,
        DateTimeOffset LastActivityAtUtc);

    private async Task<RegionBossEvent?> LoadSignupEventForUpdateAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var item = db.RegionBossEvents.Local.SingleOrDefault(x => x.Id == eventId);
        if (item is null)
        {
            return await db.RegionBossEvents.Include(x => x.Signups)
                .SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        }

        // A scoped context can already contain this event after a preceding
        // withdrawal or progression pass. Refresh it after taking the event lock
        // so the concurrency token and signup collection match the database.
        var entry = db.GetEntry(item);
        await entry.ReloadAsync(cancellationToken);
        foreach (var signup in item.Signups.ToArray())
        {
            if (db.GetEntry(signup).State is EntityState.Deleted or EntityState.Detached)
                item.Signups.Remove(signup);
        }

        var signups = entry.Collection(x => x.Signups);
        signups.IsLoaded = false;
        await signups.LoadAsync(cancellationToken);
        return item;
    }

    private void Settle(RegionBossEvent item, DateTimeOffset now)
    {
        var definition = ReadDefinition(item);
        foreach (var run in item.Runs.Where(x => x.Status == RegionBossRunStatus.Ready))
        {
            foreach (var member in run.Members)
            foreach (var bracket in definition.RewardBrackets.Where(x => x.MinimumLevelDefeated <= run.HighestLevelDefeated))
            {
                var key = $"{definition.Id}:{bracket.Key}";
                if (item.RewardGrants.Any(x => x.CharacterId == member.CharacterId && x.RewardKey == key))
                    continue;
                item.RewardGrants.Add(new RegionBossRewardGrant
                {
                    RegionBossEventId = item.Id,
                    RegionBossRunId = run.Id,
                    RegionBossDefinitionId = definition.Id,
                    CharacterId = member.CharacterId,
                    RewardKey = key,
                    MilestoneLevel = bracket.MinimumLevelDefeated,
                    RewardSnapshotJson = JsonSerializer.Serialize(
                        new RegionBossRewardSnapshot(bracket.Cinders, bracket.Soulstones), jsonOptions),
                    Status = RegionBossRewardStatus.Unclaimed,
                    CreatedAtUtc = now
                });
            }
            run.Status = RegionBossRunStatus.Settled;
            run.RowVersion++;
        }
        item.Status = RegionBossEventStatus.Settled;
        item.CompletedAtUtc = now;
    }

    private IQueryable<RegionBossEvent> EventQuery(Guid characterId) =>
        db.RegionBossEvents.AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Signups)
            .Include(x => x.Runs.Where(r => r.Members.Any(m => m.CharacterId == characterId))).ThenInclude(x => x.Members)
            .Include(x => x.Runs.Where(r => r.Members.Any(m => m.CharacterId == characterId))).ThenInclude(x => x.ParticipantResults)
            .Include(x => x.Runs.Where(r => r.Members.Any(m => m.CharacterId == characterId))).ThenInclude(x => x.Playback)
            .Include(x => x.RewardGrants.Where(g => g.CharacterId == characterId));

    private RegionBossStatusDto ToDto(RegionBossEvent item, Guid characterId, string? unlockError)
    {
        var definition = ReadDefinition(item);
        var signup = item.Signups.SingleOrDefault(x => x.CharacterId == characterId);
        var run = item.Runs.SingleOrDefault(x => x.Members.Any(m => m.CharacterId == characterId));
        var unlocked = unlockError is null;
        return new RegionBossStatusDto(
            item.Id, definition.Id, definition.Name, definition.ImagePath, definition.RegionId, item.Status,
            item.SignupStartsAtUtc, item.SignupClosesAtUtc, item.EncounterStartsAtUtc,
            item.PlaybackStartsAtUtc, item.PlaybackEndsAtUtc, timeProvider.GetUtcNow(), unlocked,
            unlockError, signup is not null, item.Signups.Count,
            run is null ? null : ToRunDto(run),
            item.RewardGrants.OrderBy(x => x.MilestoneLevel).Select(ToRewardDto).ToArray());
    }

    private RegionBossRunSummaryDto ToRunDto(RegionBossRun run)
    {
        var results = run.ParticipantResults.ToDictionary(x => x.CharacterId);
        return new RegionBossRunSummaryDto(
            run.Id, run.PartyNumber, run.Status, run.HighestLevelDefeated, run.CurrentBossLevel,
            run.CurrentBossHealthRemaining, run.CurrentBossMaxHealth, run.CurrentBossProgressBasisPoints,
            run.DurationTicks, run.FuryStacksAtEnd, run.TerminationReason,
            run.Members.OrderBy(x => x.PartySlot).Select(x => new RegionBossPartyMemberDto(
                x.CharacterId, x.CharacterName, x.PartySlot ?? 0, x.PowerRating,
                results.TryGetValue(x.CharacterId, out var result) ? ToParticipantDto(result) : null)).ToArray(),
            run.Playback is not null);
    }

    private RegionBossRewardDto ToRewardDto(RegionBossRewardGrant grant)
    {
        var reward = JsonSerializer.Deserialize<RegionBossRewardSnapshot>(grant.RewardSnapshotJson, jsonOptions)
            ?? new RegionBossRewardSnapshot(0, 0);
        return new RegionBossRewardDto(grant.Id, grant.RewardKey, grant.MilestoneLevel, grant.Status,
            reward.Cinders, reward.Soulstones, grant.ClaimedAtUtc);
    }

    private static RegionBossParticipantResultDto ToParticipantDto(RegionBossParticipantResult result) => new(
        result.DamageDone, result.DamageTaken, result.HealingDone, result.HealingReceived,
        result.BarrierGenerated, result.DamagePrevented, result.ThreatGenerated,
        result.Deaths, result.Revivals, result.DownedTicks);

    private RegionBossDefinition ReadDefinition(RegionBossEvent item) =>
        JsonSerializer.Deserialize<RegionBossDefinition>(item.DefinitionSnapshotJson, jsonOptions)
        ?? definitions.Get(item.RegionBossDefinitionId)
        ?? throw new InvalidOperationException($"Region Boss definition '{item.RegionBossDefinitionId}' was not found.");

    private async Task<string?> GetUnlockErrorAsync(
        Guid characterId,
        int? characterLevel,
        RegionBossDefinition definition,
        CancellationToken cancellationToken)
    {
        if (!characterLevel.HasValue)
            return "Character was not found.";
        if (characterLevel.Value < definition.LevelRequirement)
            return $"Level {definition.LevelRequirement} is required.";
        if (!string.IsNullOrWhiteSpace(definition.RequiredCompletedQuestId)
            && !await db.CharacterQuestProgresses.AsNoTracking().AnyAsync(
                x => x.CharacterId == characterId
                    && x.QuestId == definition.RequiredCompletedQuestId
                    && x.Status == QuestStatus.Completed,
                cancellationToken))
        {
            return $"Quest '{definition.RequiredCompletedQuestId}' must be completed first.";
        }
        if (definition.RequiredTowerFloor.HasValue
            && !await db.TowerFloorProgresses.AsNoTracking().AnyAsync(
                x => x.FloorNumber >= definition.RequiredTowerFloor.Value && x.IsCleared,
                cancellationToken))
        {
            return $"World Tower floor {definition.RequiredTowerFloor.Value} must be cleared first.";
        }
        return null;
    }

    private static DateTimeOffset NextOccurrence(
        DateTimeOffset anchor,
        RegionBossScheduleDefinition schedule)
    {
        var minimumSeconds = checked(schedule.MinimumIntervalHours * 60 * 60);
        var maximumSeconds = checked(schedule.MaximumIntervalHours * 60 * 60);
        var delaySeconds = minimumSeconds == maximumSeconds
            ? minimumSeconds
            : RandomNumberGenerator.GetInt32(minimumSeconds, checked(maximumSeconds + 1));
        return anchor.AddSeconds(delaySeconds);
    }

    private Task QueueUpdateAsync(
        RegionBossEvent item,
        string eventName,
        Audience audience,
        CancellationToken cancellationToken) =>
        realtime.PublishAsync(
            audience,
            new RegionBossUpdated(
                item.Id,
                item.RegionBossDefinitionId,
                eventName,
                item.Status.ToString(),
                timeProvider.GetUtcNow()),
            nameof(RegionBossService),
            cancellationToken);

    private Task EnqueueSignupOpenedChatAnnouncementAsync(
        RegionBossEvent item,
        RegionBossDefinition definition,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken) =>
        EnqueueChatAnnouncementAsync(
            item,
            $"Region Boss signups are now open for {definition.Name}! "
                + "Players active within the last 24 hours have been signed up automatically.",
            "signup-opened",
            sentAt,
            cancellationToken);

    private Task EnqueueFightStartedChatAnnouncementAsync(
        RegionBossEvent item,
        RegionBossDefinition definition,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken) =>
        EnqueueChatAnnouncementAsync(
            item,
            $"The Region Boss battle against {definition.Name} has begun!",
            "fight-started",
            sentAt,
            cancellationToken);

    private Task EnqueueChatAnnouncementAsync(
        RegionBossEvent item,
        string body,
        string announcementKey,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken) =>
        outbox.EnqueueAsync(
            GameEventTypes.RegionBossChatAnnouncement,
            new RegionBossChatAnnouncementPayload(
                item.Id,
                CreateAnnouncementMessageId(item.Id, announcementKey),
                body,
                "/game/world/shenic",
                sentAt),
            characterId: null,
            accountId: null,
            cancellationToken);

    private static Guid CreateAnnouncementMessageId(Guid eventId, string announcementKey)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"region-boss:{eventId:N}:{announcementKey}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private sealed record RegionBossRewardSnapshot(int Cinders, int Soulstones);
}
