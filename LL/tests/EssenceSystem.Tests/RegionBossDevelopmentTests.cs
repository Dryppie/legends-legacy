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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Interfaces;
using Services.LL.RegionBosses;

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
        Assert.Equal(RegionBossEventStatus.Playback, progressed.Status);
        Assert.True(progressed.RowVersion >= 2);
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
        });
        Assert.True(saveOrderGuard.RunOnlySaveObserved);
    }

    private static RegionBossService CreateService(
        LLDbContext db,
        DateTimeOffset now,
        bool developmentToolsEnabled,
        IGameEventOutbox? outbox = null,
        IRegionBossCombatResolver? combatResolver = null,
        IRegionBossPlaybackBundleBuilder? playbackBundles = null) =>
        new(
            db,
            new FixedDefinitionProvider(),
            new FixedPowerRatingService(now),
            combatResolver!,
            playbackBundles!,
            new NoopRealtimeBroadcaster(),
            outbox ?? new RecordingGameEventOutbox(),
            new FixedTimeProvider(now),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            Options.Create(new RegionBossOptions { DevelopmentToolsEnabled = developmentToolsEnabled }),
            NullLogger<RegionBossService>.Instance);

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
