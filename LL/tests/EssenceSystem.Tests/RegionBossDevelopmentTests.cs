using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.RegionBosses;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.CharacterActions;
using Domain.Models.Entities.Characters;
using Domain.Models.RegionBosses;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Snapshots;
using Services.LL.Interfaces;
using Services.LL.RegionBosses;
using Services.LL.Snapshots;

namespace EssenceSystem.Tests;

public sealed class RegionBossDevelopmentTests
{
    [Fact]
    public async Task Automatic_schedule_keeps_region_boss_encounters_four_to_eight_hours_apart()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(db, now, developmentToolsEnabled: false);

        await service.EnsureScheduledEventsAsync(CancellationToken.None);
        await service.EnsureScheduledEventsAsync(CancellationToken.None);

        var first = Assert.Single(await db.RegionBossEvents.AsNoTracking().ToArrayAsync());
        Assert.InRange(first.EncounterStartsAtUtc, now.AddHours(4), now.AddHours(8));
        Assert.Equal(first.EncounterStartsAtUtc.AddMinutes(-10), first.SignupStartsAtUtc);

        var afterFirstEncounter = first.EncounterStartsAtUtc.AddSeconds(1);
        var nextService = CreateService(db, afterFirstEncounter, developmentToolsEnabled: false);
        await nextService.EnsureScheduledEventsAsync(CancellationToken.None);

        var events = await db.RegionBossEvents.AsNoTracking()
            .OrderBy(x => x.EncounterStartsAtUtc)
            .ToArrayAsync();
        Assert.Equal(2, events.Length);
        Assert.InRange(
            events[1].EncounterStartsAtUtc - events[0].EncounterStartsAtUtc,
            TimeSpan.FromHours(4),
            TimeSpan.FromHours(8));
    }

    [Fact]
    public async Task Automatic_schedule_moves_an_untouched_legacy_event_into_the_new_window()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var legacyEvent = new RegionBossEvent
        {
            RegionBossDefinitionId = "test-region-boss",
            RegionId = 1,
            Status = RegionBossEventStatus.Scheduled,
            SignupStartsAtUtc = now.AddDays(2).AddMinutes(-10),
            SignupClosesAtUtc = now.AddDays(2),
            EncounterStartsAtUtc = now.AddDays(2),
            DefinitionSnapshotJson = "{}",
            DefinitionHash = "legacy",
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            CreatedAtUtc = now.AddDays(-1),
            UpdatedAtUtc = now.AddDays(-1)
        };
        db.RegionBossEvents.Add(legacyEvent);
        await db.SaveChangesAsync();
        var service = CreateService(db, now, developmentToolsEnabled: false);

        await service.EnsureScheduledEventsAsync(CancellationToken.None);

        var rescheduled = Assert.Single(await db.RegionBossEvents.AsNoTracking().ToArrayAsync());
        Assert.Equal(legacyEvent.Id, rescheduled.Id);
        Assert.InRange(rescheduled.EncounterStartsAtUtc, now.AddHours(4), now.AddHours(8));
        Assert.Equal(rescheduled.EncounterStartsAtUtc.AddMinutes(-10), rescheduled.SignupStartsAtUtc);
        Assert.NotEqual("legacy", rescheduled.DefinitionHash);
        Assert.Equal(1, rescheduled.RowVersion);
    }

    [Fact]
    public async Task Spawn_creates_immediate_event_with_creator_and_requested_guests()
    {
        await using var db = CreateDbContext();
        var creator = SeedCharacter(db, "LocalDeveloper", "Developer", isGuest: false);
        for (var index = 1; index <= 30; index++)
            SeedCharacter(db, $"SeedGuest_{index:D2}", $"Local Guest {index:D2}", isGuest: true);
        await db.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(db, now, developmentToolsEnabled: true);

        var result = await service.SpawnDevelopmentEventAsync(creator.Id, 1, 24, CancellationToken.None);

        Assert.True(result.Succeeded, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(25, result.Value.SignupCount);
        Assert.True(result.Value.IsSignedUp);
        Assert.Equal(RegionBossEventStatus.SignupOpen, result.Value.Status);
        Assert.Equal(now.AddSeconds(10), result.Value.EncounterStartsAtUtc);
        var signups = await db.RegionBossSignups.Where(x => x.RegionBossEventId == result.Value.EventId).ToArrayAsync();
        Assert.Equal(25, signups.Length);
        Assert.Equal(25, signups.Select(x => x.AccountId).Distinct().Count());
        Assert.Empty(db.CharacterSnapshots);
    }

    [Fact]
    public async Task Withdrawn_character_can_rejoin_the_same_open_event()
    {
        await using var db = CreateDbContext();
        var character = SeedCharacter(db, "ReturningPlayer", "Returning Player", isGuest: false);
        await db.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(db, now, developmentToolsEnabled: true);
        var spawned = await service.SpawnDevelopmentEventAsync(
            character.Id,
            1,
            0,
            CancellationToken.None);
        Assert.True(spawned.Succeeded, spawned.Error);

        var withdrawn = await service.WithdrawAsync(
            character.Id,
            spawned.Value!.EventId,
            CancellationToken.None);
        var rejoined = await service.SignupAsync(
            character.Id,
            spawned.Value.EventId,
            CancellationToken.None);

        Assert.True(withdrawn.Succeeded, withdrawn.Error);
        Assert.False(withdrawn.Value!.IsSignedUp);
        Assert.True(rejoined.Succeeded, rejoined.Error);
        Assert.True(rejoined.Value!.IsSignedUp);
        Assert.Equal(1, rejoined.Value.SignupCount);
    }

    [Fact]
    public async Task Progression_refreshes_an_event_tracked_before_a_signup_change()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var workerDb = CreateDbContext(databaseName);
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var item = new RegionBossEvent
        {
            RegionBossDefinitionId = "test-region-boss",
            RegionId = 1,
            Status = RegionBossEventStatus.SignupOpen,
            SignupStartsAtUtc = now.AddMinutes(-10),
            SignupClosesAtUtc = now,
            EncounterStartsAtUtc = now,
            DefinitionSnapshotJson = "{}",
            DefinitionHash = "test",
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            CreatedAtUtc = now.AddMinutes(-10),
            UpdatedAtUtc = now.AddMinutes(-1)
        };
        workerDb.RegionBossEvents.Add(item);
        await workerDb.SaveChangesAsync();

        await using (var requestDb = CreateDbContext(databaseName))
        {
            var concurrent = await requestDb.RegionBossEvents.SingleAsync(x => x.Id == item.Id);
            concurrent.RowVersion++;
            concurrent.UpdatedAtUtc = now;
            await requestDb.SaveChangesAsync();
        }

        var service = CreateService(workerDb, now, developmentToolsEnabled: false);
        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var progressed = await workerDb.RegionBossEvents.AsNoTracking()
            .SingleAsync(x => x.Id == item.Id);
        Assert.Equal(RegionBossEventStatus.Cancelled, progressed.Status);
        Assert.True(progressed.RowVersion >= 2);
    }

    [Fact]
    public async Task Signup_close_removes_ineligible_players_and_cancels_an_undersized_event()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new FixedDefinitionProvider().GetAll().Single();
        var eligibleOne = SeedCharacter(db, "EligibleOne", "Eligible One", isGuest: false);
        var eligibleTwo = SeedCharacter(db, "EligibleTwo", "Eligible Two", isGuest: false);
        var ineligible = SeedCharacter(db, "Ineligible", "Ineligible", isGuest: false);
        ineligible.Level = definition.LevelRequirement - 1;
        var item = CreateEvent(definition, now, RegionBossEventStatus.SignupOpen);
        foreach (var character in new[] { eligibleOne, eligibleTwo, ineligible })
        {
            item.Signups.Add(new RegionBossSignup
            {
                Event = item,
                RegionBossEventId = item.Id,
                CharacterId = character.Id,
                AccountId = character.UserId,
                CharacterName = character.Name,
                SignedUpAtUtc = now.AddMinutes(-5)
            });
        }
        db.RegionBossEvents.Add(item);
        await db.SaveChangesAsync();
        var service = CreateService(db, now, developmentToolsEnabled: false);

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var progressed = await db.RegionBossEvents.AsNoTracking()
            .Include(x => x.Signups)
            .Include(x => x.Runs)
            .SingleAsync(x => x.Id == item.Id);
        Assert.Equal(RegionBossEventStatus.Cancelled, progressed.Status);
        Assert.Equal(now, progressed.CancelledAtUtc);
        Assert.Contains("at least 3", progressed.CancellationReason);
        Assert.Equal(
            new[] { eligibleOne.Id, eligibleTwo.Id }.Order(),
            progressed.Signups.Select(x => x.CharacterId).Order());
        Assert.Empty(progressed.Runs);
    }

    [Fact]
    public async Task Finalization_refreshes_an_event_changed_while_a_run_is_resolving()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var workerDb = CreateDbContext(databaseName);
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new FixedDefinitionProvider().GetAll().Single();
        var item = new RegionBossEvent
        {
            RegionBossDefinitionId = definition.Id,
            RegionId = definition.RegionId,
            Status = RegionBossEventStatus.Resolving,
            SignupStartsAtUtc = now.AddMinutes(-10),
            SignupClosesAtUtc = now.AddMinutes(-1),
            EncounterStartsAtUtc = now.AddMinutes(-1),
            DefinitionSnapshotJson = JsonSerializer.Serialize(
                definition,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            DefinitionHash = "test",
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            CreatedAtUtc = now.AddMinutes(-10),
            UpdatedAtUtc = now.AddMinutes(-1)
        };
        item.Runs.Add(new RegionBossRun
        {
            Event = item,
            RegionBossEventId = item.Id,
            PartyNumber = 1,
            PartySize = 1,
            Status = RegionBossRunStatus.Queued
        });
        workerDb.RegionBossEvents.Add(item);
        await workerDb.SaveChangesAsync();
        var service = CreateService(
            workerDb,
            now,
            developmentToolsEnabled: false,
            combatResolver: new ConcurrentEventUpdateResolver(databaseName, item.Id, now),
            playbackBundles: new StubPlaybackBundleBuilder(now));

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var progressed = await workerDb.RegionBossEvents.AsNoTracking()
            .Include(x => x.Runs)
            .SingleAsync(x => x.Id == item.Id);
        Assert.Equal(RegionBossEventStatus.Playback, progressed.Status);
        Assert.Equal(2, progressed.RowVersion);
        Assert.Equal(RegionBossRunStatus.Ready, Assert.Single(progressed.Runs).Status);
    }

    [Fact]
    public async Task Finalization_serializes_run_updates_with_postgres_advisory_locks()
    {
        var connectionString = Environment.GetEnvironmentVariable("LL_TEST_REGION_BOSS_POSTGRES_CONNECTION")
            ?? Environment.GetEnvironmentVariable("LL_TEST_TOURNAMENT_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var schemaName = $"ll_region_boss_lock_tests_{Guid.NewGuid():N}";
        await using var adminDb = CreatePostgresDbContext(connectionString);
        var createSchemaSql = $"CREATE SCHEMA \"{schemaName}\"";
        var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
        await adminDb.Database.ExecuteSqlRawAsync(createSchemaSql);
        try
        {
            var isolatedConnectionString = WithSearchPath(connectionString, schemaName);
            await using (var migrationDb = CreatePostgresDbContext(isolatedConnectionString, schemaName))
                await migrationDb.Database.MigrateAsync();

            var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
            var definition = new FixedDefinitionProvider().GetAll().Single();
            Guid eventId;
            Guid runId;
            await using (var seedDb = CreatePostgresDbContext(isolatedConnectionString, schemaName))
            {
                var item = CreateEvent(definition, now, RegionBossEventStatus.Resolving);
                var run = new RegionBossRun
                {
                    Event = item,
                    RegionBossEventId = item.Id,
                    PartyNumber = 1,
                    PartySize = 1,
                    Status = RegionBossRunStatus.Ready,
                    DurationTicks = 100
                };
                item.Runs.Add(run);
                seedDb.RegionBossEvents.Add(item);
                await seedDb.SaveChangesAsync();
                eventId = item.Id;
                runId = run.Id;
            }

            var realtime = new BlockingRealtimeBroadcaster();
            await using var progressionDb = CreatePostgresDbContext(isolatedConnectionString, schemaName);
            var service = CreateService(
                progressionDb,
                now,
                developmentToolsEnabled: false,
                realtime: realtime);
            var progression = service.ProgressEventsAsync("postgres-worker", CancellationToken.None);
            await realtime.PublishEntered.Task.WaitAsync(TimeSpan.FromSeconds(15));

            var runLockAcquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var concurrentUpdate = UpdateRunAfterLockAsync(
                isolatedConnectionString,
                schemaName,
                runId,
                runLockAcquired);
            var completedWhileFinalizerHeldLock = await Task.WhenAny(
                runLockAcquired.Task,
                Task.Delay(TimeSpan.FromMilliseconds(500))) == runLockAcquired.Task;
            realtime.Release.SetResult();
            await Task.WhenAll(progression, concurrentUpdate);
            Assert.False(completedWhileFinalizerHeldLock);

            await using var verifyDb = CreatePostgresDbContext(isolatedConnectionString, schemaName);
            var progressed = await verifyDb.RegionBossEvents.AsNoTracking()
                .Include(x => x.Runs)
                .SingleAsync(x => x.Id == eventId);
            Assert.Equal(RegionBossEventStatus.Playback, progressed.Status);
            Assert.Equal(2, Assert.Single(progressed.Runs).RowVersion);
        }
        finally
        {
            await adminDb.Database.ExecuteSqlRawAsync(dropSchemaSql);
        }
    }

    [Fact]
    public async Task Progression_limits_run_resolution_work_per_event()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new FixedDefinitionProvider().GetAll().Single();
        var item = CreateEvent(definition, now, RegionBossEventStatus.Resolving);
        for (var partyNumber = 1; partyNumber <= 3; partyNumber++)
        {
            item.Runs.Add(new RegionBossRun
            {
                Event = item,
                RegionBossEventId = item.Id,
                PartyNumber = partyNumber,
                PartySize = 1,
                Status = RegionBossRunStatus.Queued
            });
        }
        db.RegionBossEvents.Add(item);
        await db.SaveChangesAsync();
        var resolver = new CountingCombatResolver();
        var service = CreateService(
            db,
            now,
            developmentToolsEnabled: false,
            combatResolver: resolver,
            playbackBundles: new StubPlaybackBundleBuilder(now),
            maximumRunResolutionsPerEvent: 1);

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var progressed = await db.RegionBossEvents.AsNoTracking()
            .Include(x => x.Runs)
            .SingleAsync(x => x.Id == item.Id);
        Assert.Equal(RegionBossEventStatus.Resolving, progressed.Status);
        Assert.Equal(1, resolver.InvocationCount);
        Assert.Equal(1, progressed.Runs.Count(x => x.Status == RegionBossRunStatus.Ready));
        Assert.Equal(2, progressed.Runs.Count(x => x.Status == RegionBossRunStatus.Queued));
        Assert.Equal(1, await db.RegionBossParticipantResults.CountAsync());
    }

    [Fact]
    public async Task Oversized_playback_keeps_the_resolved_run_and_does_not_retry_combat()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new FixedDefinitionProvider().GetAll().Single();
        var item = CreateEvent(definition, now, RegionBossEventStatus.Resolving);
        item.Runs.Add(new RegionBossRun
        {
            Event = item,
            RegionBossEventId = item.Id,
            PartyNumber = 1,
            PartySize = 1,
            Status = RegionBossRunStatus.Queued
        });
        db.RegionBossEvents.Add(item);
        await db.SaveChangesAsync();
        var resolver = new CountingCombatResolver();
        var service = CreateService(
            db,
            now,
            developmentToolsEnabled: false,
            combatResolver: resolver,
            playbackBundles: new OversizedPlaybackBundleBuilder());

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var run = await db.RegionBossRuns.AsNoTracking()
            .Include(candidate => candidate.Playback)
            .SingleAsync(candidate => candidate.RegionBossEventId == item.Id);
        Assert.Equal(RegionBossRunStatus.Ready, run.Status);
        Assert.Equal(1, run.SimulationAttempts);
        Assert.Equal(1, resolver.InvocationCount);
        Assert.Null(run.Playback);
        Assert.Null(run.LastError);
        Assert.Equal(1, await db.RegionBossParticipantResults.CountAsync());
    }

    [Fact]
    public async Task A_failed_event_does_not_prevent_later_events_from_progressing()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new FixedDefinitionProvider().GetAll().Single();
        var malformed = CreateEvent(definition, now.AddMinutes(-1), RegionBossEventStatus.Playback);
        malformed.DefinitionSnapshotJson = "{";
        malformed.PlaybackEndsAtUtc = now;
        var valid = CreateEvent(definition, now, RegionBossEventStatus.Playback);
        valid.PlaybackEndsAtUtc = now;
        db.RegionBossEvents.AddRange(malformed, valid);
        await db.SaveChangesAsync();
        var service = CreateService(db, now, developmentToolsEnabled: false);

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var statuses = await db.RegionBossEvents.AsNoTracking()
            .Where(x => x.Id == malformed.Id || x.Id == valid.Id)
            .ToDictionaryAsync(x => x.Id, x => x.Status);
        Assert.Equal(RegionBossEventStatus.Playback, statuses[malformed.Id]);
        Assert.Equal(RegionBossEventStatus.Settled, statuses[valid.Id]);
    }

    [Theory]
    [InlineData(false, 0, null)]
    [InlineData(true, 1, "test-region-boss:level-5")]
    public async Task Settlement_respects_reward_enablement_and_non_cumulative_brackets(
        bool rewardsEnabled,
        int expectedGrantCount,
        string? expectedRewardKey)
    {
        var databaseName = Guid.NewGuid().ToString();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new RegionBossDefinition
        {
            Id = "test-region-boss",
            Name = "Test Region Boss",
            ImagePath = "test_boss",
            RegionId = 1,
            CreatureId = Guid.NewGuid(),
            LevelRequirement = 1,
            RewardsEnabled = rewardsEnabled,
            CumulativeRewards = false,
            RewardBrackets =
            [
                new RegionBossRewardBracketDefinition { Key = "level-1", MinimumLevelDefeated = 1, Cinders = 10 },
                new RegionBossRewardBracketDefinition { Key = "level-3", MinimumLevelDefeated = 3, Cinders = 30 },
                new RegionBossRewardBracketDefinition { Key = "level-5", MinimumLevelDefeated = 5, Cinders = 50 }
            ]
        };
        Guid eventId;
        await using (var seedDb = CreateDbContext(databaseName))
        {
            var item = CreateEvent(definition, now, RegionBossEventStatus.Playback);
            item.PlaybackStartsAtUtc = now.AddMinutes(-1);
            item.PlaybackEndsAtUtc = now;
            var run = new RegionBossRun
            {
                Event = item,
                RegionBossEventId = item.Id,
                PartyNumber = 1,
                PartySize = 1,
                Status = RegionBossRunStatus.Ready,
                HighestLevelDefeated = 5
            };
            item.Runs.Add(run);
            seedDb.RegionBossEvents.Add(item);
            await seedDb.SaveChangesAsync();
            seedDb.RegionBossSignups.Add(new RegionBossSignup
            {
                Event = item,
                RegionBossEventId = item.Id,
                Run = run,
                RegionBossRunId = run.Id,
                CharacterId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                CharacterName = "Reward Tester",
                PartySlot = 1,
                SignedUpAtUtc = now.AddMinutes(-5)
            });
            await seedDb.SaveChangesAsync();
            eventId = item.Id;
        }

        await using (var workerDb = CreateDbContext(databaseName))
        {
            var logger = new RecordingLogger();
            var service = CreateService(
                workerDb,
                now,
                developmentToolsEnabled: false,
                logger: logger);
            await service.ProgressEventsAsync("test-worker", CancellationToken.None);
            Assert.True(logger.Errors.Count == 0, string.Join(Environment.NewLine, logger.Errors));
        }

        await using var verifyDb = CreateDbContext(databaseName);
        var settled = await verifyDb.RegionBossEvents.AsNoTracking()
            .Include(x => x.Runs)
            .Include(x => x.RewardGrants)
            .SingleAsync(x => x.Id == eventId);
        Assert.Equal(RegionBossEventStatus.Settled, settled.Status);
        Assert.Equal(RegionBossRunStatus.Settled, Assert.Single(settled.Runs).Status);
        Assert.Equal(expectedGrantCount, settled.RewardGrants.Count);
        if (expectedRewardKey is not null)
        {
            var grant = Assert.Single(settled.RewardGrants);
            Assert.Equal(expectedRewardKey, grant.RewardKey);
            Assert.Equal(5, grant.MilestoneLevel);
        }
    }

    [Fact]
    public async Task Progression_does_not_increment_row_version_without_a_state_change()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var updatedAt = now.AddMinutes(-1);
        var item = new RegionBossEvent
        {
            RegionBossDefinitionId = "test-region-boss",
            RegionId = 1,
            Status = RegionBossEventStatus.Playback,
            SignupStartsAtUtc = now.AddMinutes(-10),
            SignupClosesAtUtc = now.AddMinutes(-2),
            EncounterStartsAtUtc = now.AddMinutes(-2),
            PlaybackStartsAtUtc = now.AddMinutes(-1),
            PlaybackEndsAtUtc = now.AddMinutes(1),
            DefinitionSnapshotJson = "{}",
            DefinitionHash = "test",
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            RowVersion = 7,
            CreatedAtUtc = now.AddMinutes(-10),
            UpdatedAtUtc = updatedAt
        };
        db.RegionBossEvents.Add(item);
        await db.SaveChangesAsync();
        var service = CreateService(db, now, developmentToolsEnabled: false);

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var unchanged = await db.RegionBossEvents.AsNoTracking().SingleAsync(x => x.Id == item.Id);
        Assert.Equal(7, unchanged.RowVersion);
        Assert.Equal(updatedAt, unchanged.UpdatedAtUtc);
    }

    [Fact]
    public async Task Signup_opening_automatically_enrolls_eligible_characters_active_within_twenty_four_hours()
    {
        await using var db = CreateDbContext();
        var outbox = new RecordingGameEventOutbox();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var recent = SeedCharacter(db, "RecentPlayer", "Recent Player", isGuest: false);
        var boundary = SeedCharacter(db, "BoundaryPlayer", "Boundary Player", isGuest: false);
        var stale = SeedCharacter(db, "StalePlayer", "Stale Player", isGuest: false);
        var locked = SeedCharacter(db, "LockedPlayer", "Locked Player", isGuest: false);
        locked.Level = 19;
        db.CharacterActions.AddRange(
            new CharacterAction
            {
                CharacterId = recent.Id,
                UpdatedAt = now.AddHours(-2),
                IsDeleted = true
            },
            new CharacterAction
            {
                CharacterId = boundary.Id,
                UpdatedAt = now.AddHours(-24),
                IsDeleted = true
            },
            new CharacterAction
            {
                CharacterId = stale.Id,
                UpdatedAt = now.AddHours(-24).AddTicks(-1),
                IsDeleted = true
            },
            new CharacterAction
            {
                CharacterId = locked.Id,
                UpdatedAt = now.AddMinutes(-1),
                IsDeleted = true
            });
        await db.SaveChangesAsync();
        var service = CreateService(db, now, developmentToolsEnabled: false, outbox);
        await service.EnsureScheduledEventsAsync(CancellationToken.None);
        var item = await db.RegionBossEvents.SingleAsync();
        item.SignupStartsAtUtc = now;
        item.SignupClosesAtUtc = now.AddMinutes(10);
        item.EncounterStartsAtUtc = now.AddMinutes(10);
        await db.SaveChangesAsync();

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var signups = await db.RegionBossSignups.AsNoTracking()
            .Where(signup => signup.RegionBossEventId == item.Id)
            .OrderBy(signup => signup.CharacterName)
            .ToArrayAsync();
        Assert.Equal(2, signups.Length);
        Assert.Equal(
            new[] { boundary.Id, recent.Id }.Order().ToArray(),
            signups.Select(signup => signup.CharacterId).Order().ToArray());
        Assert.All(signups, signup => Assert.Equal(now, signup.SignedUpAtUtc));
        Assert.Equal(2, signups.Select(signup => signup.AccountId).Distinct().Count());
        Assert.Empty(db.CharacterSnapshots);
        Assert.Equal(
            RegionBossEventStatus.SignupOpen,
            (await db.RegionBossEvents.AsNoTracking().SingleAsync()).Status);
        var announcement = Assert.Single(outbox.Announcements);
        Assert.Equal(item.Id, announcement.RegionBossEventId);
        Assert.Contains("signups are now open", announcement.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("last 24 hours", announcement.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/game/world/shenic", announcement.TargetUrl);
    }

    [Fact]
    public async Task Playback_start_announces_that_the_region_boss_fight_has_begun()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var definition = new FixedDefinitionProvider().GetAll().Single();
        var item = new RegionBossEvent
        {
            RegionBossDefinitionId = definition.Id,
            RegionId = definition.RegionId,
            Status = RegionBossEventStatus.Resolving,
            SignupStartsAtUtc = now.AddMinutes(-10),
            SignupClosesAtUtc = now.AddMinutes(-1),
            EncounterStartsAtUtc = now.AddMinutes(-1),
            DefinitionSnapshotJson = JsonSerializer.Serialize(
                definition,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            DefinitionHash = "test",
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            CreatedAtUtc = now.AddMinutes(-10),
            UpdatedAtUtc = now.AddMinutes(-1)
        };
        item.Runs.Add(new RegionBossRun
        {
            Event = item,
            RegionBossEventId = item.Id,
            PartyNumber = 1,
            PartySize = 1,
            Status = RegionBossRunStatus.Ready,
            DurationTicks = 100
        });
        db.RegionBossEvents.Add(item);
        await db.SaveChangesAsync();
        var outbox = new RecordingGameEventOutbox();
        var service = CreateService(db, now, developmentToolsEnabled: false, outbox);

        await service.ProgressEventsAsync("test-worker", CancellationToken.None);

        var progressed = await db.RegionBossEvents.AsNoTracking().SingleAsync(x => x.Id == item.Id);
        Assert.Equal(RegionBossEventStatus.Playback, progressed.Status);
        var announcement = Assert.Single(outbox.Announcements);
        Assert.Equal(item.Id, announcement.RegionBossEventId);
        Assert.Equal(
            "The Region Boss battle against Test Region Boss has begun!",
            announcement.Body);
    }

    [Fact]
    public async Task Spawn_is_rejected_when_development_tools_are_disabled()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, DateTimeOffset.UtcNow, developmentToolsEnabled: false);

        var result = await service.SpawnDevelopmentEventAsync(Guid.NewGuid(), 2, 24, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("Region Boss development tools are disabled.", result.Error);
        Assert.Empty(db.RegionBossEvents);
    }

    [Fact]
    public async Task Progress_persists_runs_before_assigning_existing_signups()
    {
        var saveOrderGuard = new RunBeforeMembershipUpdateGuard();
        await using var db = CreateDbContext(saveOrderGuard);
        var creator = SeedCharacter(db, "LocalDeveloper", "Developer", isGuest: false);
        for (var index = 1; index <= 5; index++)
            SeedCharacter(db, $"SeedGuest_{index:D2}", $"Local Guest {index:D2}", isGuest: true);
        await db.SaveChangesAsync();
        var now = new DateTimeOffset(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
        var service = CreateService(db, now, developmentToolsEnabled: true);
        var spawned = await service.SpawnDevelopmentEventAsync(creator.Id, 1, 5, CancellationToken.None);
        Assert.True(spawned.Succeeded, spawned.Error);
        Assert.NotNull(spawned.Value);
        var eventId = spawned.Value.EventId;
        var item = await db.RegionBossEvents.SingleAsync(x => x.Id == eventId);
        item.SignupClosesAtUtc = now;
        item.EncounterStartsAtUtc = now;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await service.ProgressEventsAsync("test-development-worker", CancellationToken.None);

        var progressed = await db.RegionBossEvents
            .AsNoTracking()
            .Include(x => x.Runs)
            .Include(x => x.Signups)
            .SingleAsync(x => x.Id == eventId);
        var runIds = progressed.Runs.Select(x => x.Id).ToHashSet();
        Assert.Equal(RegionBossEventStatus.Resolving, progressed.Status);
        Assert.Equal(2, progressed.Runs.Count);
        Assert.All(progressed.Signups, signup =>
        {
            Assert.NotNull(signup.RegionBossRunId);
            Assert.Contains(signup.RegionBossRunId.Value, runIds);
            Assert.NotNull(signup.PartySlot);
            Assert.NotNull(signup.CharacterSnapshotId);
        });
        Assert.True(saveOrderGuard.RunOnlySaveObserved);
    }

    private static RegionBossService CreateService(
        LLDbContext db,
        DateTimeOffset now,
        bool developmentToolsEnabled,
        IGameEventOutbox? outbox = null,
        IRegionBossCombatResolver? combatResolver = null,
        IRegionBossPlaybackBundleBuilder? playbackBundles = null,
        IGameRealtimeBroadcaster? realtime = null,
        int maximumRunResolutionsPerEvent = 25,
        ILogger<RegionBossService>? logger = null) =>
        new(
            db,
            new FixedDefinitionProvider(),
            new FixedPowerRatingService(now),
            new CharacterSnapshotService(new CharacterSnapshotRepository(db)),
            combatResolver!,
            playbackBundles!,
            realtime ?? new NoopRealtimeBroadcaster(),
            outbox ?? new RecordingGameEventOutbox(),
            new FixedTimeProvider(now),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            Options.Create(new RegionBossOptions
            {
                DevelopmentToolsEnabled = developmentToolsEnabled,
                MaximumRunResolutionsPerEvent = maximumRunResolutionsPerEvent
            }),
            logger ?? NullLogger<RegionBossService>.Instance);

    private static RegionBossEvent CreateEvent(
        RegionBossDefinition definition,
        DateTimeOffset now,
        RegionBossEventStatus status) => new()
        {
            RegionBossDefinitionId = definition.Id,
            RegionId = definition.RegionId,
            Status = status,
            SignupStartsAtUtc = now.AddMinutes(-10),
            SignupClosesAtUtc = now,
            EncounterStartsAtUtc = now,
            DefinitionSnapshotJson = JsonSerializer.Serialize(
                definition,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            DefinitionHash = "test",
            MatchmakingAlgorithmVersion = RegionBossRules.MatchmakingAlgorithmVersion,
            CombatRulesVersion = RegionBossRules.Version,
            CreatedAtUtc = now.AddMinutes(-10),
            UpdatedAtUtc = now.AddMinutes(-1)
        };

    private sealed class CountingCombatResolver : IRegionBossCombatResolver
    {
        public int InvocationCount { get; private set; }

        public Task<RegionBossCombatResolution> ResolveAsync(
            RegionBossRun run,
            RegionBossDefinition definition,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(new RegionBossCombatResolution(
                1,
                2,
                500,
                1_000,
                5_000,
                100,
                0,
                RegionBossTerminationReason.PartyDefeated,
                [new RegionBossParticipantResult
                {
                    RegionBossRunId = run.Id,
                    CharacterId = Guid.NewGuid()
                }],
                null!,
                []));
        }
    }

    private sealed class ConcurrentEventUpdateResolver(
        string databaseName,
        Guid eventId,
        DateTimeOffset now) : IRegionBossCombatResolver
    {
        public async Task<RegionBossCombatResolution> ResolveAsync(
            RegionBossRun run,
            RegionBossDefinition definition,
            CancellationToken cancellationToken)
        {
            await using var concurrentDb = CreateDbContext(databaseName);
            var concurrent = await concurrentDb.RegionBossEvents
                .SingleAsync(x => x.Id == eventId, cancellationToken);
            concurrent.RowVersion++;
            concurrent.UpdatedAtUtc = now;
            await concurrentDb.SaveChangesAsync(cancellationToken);

            return new RegionBossCombatResolution(
                1,
                2,
                500,
                1_000,
                5_000,
                100,
                0,
                RegionBossTerminationReason.PartyDefeated,
                [],
                null!,
                []);
        }
    }

    private sealed class StubPlaybackBundleBuilder(DateTimeOffset now) : IRegionBossPlaybackBundleBuilder
    {
        public RegionBossPlayback Build(Guid runId, RegionBossCombatResolution resolution)
        {
            var playback = new RegionBossPlayback
            {
                RegionBossRunId = runId,
                TotalTicks = resolution.DurationTicks,
                FrameCount = 1,
                BundleHash = "test",
                BundleLength = 1,
                CreatedAtUtc = now
            };
            playback.Artifact = new RegionBossPlaybackArtifact
            {
                RegionBossRunId = runId,
                Playback = playback,
                BundleBytes = [1]
            };
            return playback;
        }
    }

    private sealed class OversizedPlaybackBundleBuilder : IRegionBossPlaybackBundleBuilder
    {
        public RegionBossPlayback Build(Guid runId, RegionBossCombatResolution resolution) =>
            throw new RegionBossPlaybackSizeLimitExceededException("uncompressed", 25, 24);
    }

    private static LLDbContext CreateDbContext(params IInterceptor[] interceptors) =>
        CreateDbContext(Guid.NewGuid().ToString(), interceptors);

    private static LLDbContext CreateDbContext(
        string databaseName,
        params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(interceptors)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new LLDbContext(options);
    }

    private static LLDbContext CreatePostgresDbContext(
        string connectionString,
        string? migrationsSchema = null)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql(connectionString, postgres =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsSchema))
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema);
            })
            .Options;
        return new LLDbContext(options);
    }

    private static string WithSearchPath(string connectionString, string schemaName) =>
        $"{connectionString.Trim().TrimEnd(';')};Search Path={schemaName}";

    private static async Task UpdateRunAfterLockAsync(
        string connectionString,
        string schemaName,
        Guid runId,
        TaskCompletionSource runLockAcquired)
    {
        await using var db = CreatePostgresDbContext(connectionString, schemaName);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.AcquireRegionBossRunLockAsync(runId);
        runLockAcquired.SetResult();
        var run = await db.RegionBossRuns.SingleAsync(x => x.Id == runId);
        run.RowVersion++;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static Character SeedCharacter(
        LLDbContext db,
        string username,
        string characterName,
        bool isGuest)
    {
        var user = isGuest ? AppUser.Guest() : new AppUser();
        user.Username = username;
        user.IsGuest = isGuest;
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Name = characterName,
            Level = 30
        };
        db.Users.Add(user);
        db.Characters.Add(character);
        return character;
    }

    private sealed class FixedDefinitionProvider : IRegionBossDefinitionProvider
    {
        private static readonly RegionBossDefinition Definition = new()
        {
            Id = "test-region-boss",
            Name = "Test Region Boss",
            ImagePath = "test_boss",
            RegionId = 1,
            CreatureId = Guid.NewGuid(),
            LevelRequirement = 20,
            RewardBrackets =
            [
                new RegionBossRewardBracketDefinition
                {
                    Key = "level-1",
                    MinimumLevelDefeated = 1,
                    Cinders = 1
                }
            ]
        };

        public IReadOnlyList<RegionBossDefinition> GetAll() => [Definition];
        public RegionBossDefinition? Get(string definitionId) => Definition.Id == definitionId ? Definition : null;
    }

    private sealed class FixedPowerRatingService(DateTimeOffset now) : IPowerRatingService
    {
        public Task<OverallPowerRating> GetCharacterOverallRatingAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new OverallPowerRating(10_000, PowerAnalysisState.Available));

        public Task<OverallPowerRating> GetCharacterOverallRatingAsync(Character character, CancellationToken cancellationToken) =>
            GetCharacterOverallRatingAsync(character.Id, cancellationToken);

        public Task<PowerRatingSnapshot> GetCharacterRatingAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new PowerRatingSnapshot(
                PowerRatingAlgorithm.Version,
                $"local-{characterId:N}",
                10_000,
                1_000,
                1_000,
                1_000,
                1_000,
                1_000,
                1_000,
                now,
                PowerRatingConfidence.High,
                PowerAnalysisState.Available));

        public Task<PowerRatingSnapshot> GetPartyRatingAsync(
            Guid characterId,
            DungeonPartySelection partySelection,
            CancellationToken cancellationToken) => GetCharacterRatingAsync(characterId, cancellationToken);
    }

    private sealed class NoopRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class BlockingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public TaskCompletionSource PublishEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            PublishEntered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class RecordingLogger : ILogger<RegionBossService>
    {
        public List<string> Errors { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                Errors.Add($"{formatter(state, exception)}{Environment.NewLine}{exception}");
        }
    }

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<RegionBossChatAnnouncementPayload> Announcements { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            if (eventType == GameEventTypes.RegionBossChatAnnouncement
                && payload is RegionBossChatAnnouncementPayload announcement)
            {
                Announcements.Add(announcement);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RunBeforeMembershipUpdateGuard : SaveChangesInterceptor
    {
        public bool RunOnlySaveObserved { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context!;
            var insertsRuns = context.ChangeTracker.Entries<RegionBossRun>()
                .Any(entry => entry.State == EntityState.Added);
            var assignsSignups = context.ChangeTracker.Entries<RegionBossSignup>()
                .Any(entry => entry.State == EntityState.Modified
                    && entry.Entity.RegionBossRunId is not null);
            if (insertsRuns && assignsSignups)
            {
                throw new InvalidOperationException(
                    "Region Boss runs and existing signup memberships must be persisted in separate saves.");
            }

            RunOnlySaveObserved |= insertsRuns;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
