using Domain.Models.Achievements;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Missions;
using Domain.Models.Leaderboards;
using Domain.Models.Raids;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Leaderboards;

namespace EssenceSystem.Tests;

public sealed partial class LeaderboardRepositoryTests
{
    [Fact]
    public async Task GetLeaderboardAsync_includes_all_profiles_and_returns_viewer_separately()
    {
        await using var db = CreateDb();
        var champion = AddCharacter(db, "Champion", level: 30);
        var viewer = AddCharacter(db, "Viewer", level: 10);
        var admin = AddCharacter(db, "Admin", level: 99);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            viewer.Id,
            LeaderboardBoardKey.CombatLevel,
            2,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(3, board.TotalParticipants);
        Assert.Equal(admin.Id, board.Entries[0].ParticipantId);
        Assert.Equal(champion.Id, board.Entries[1].ParticipantId);
        Assert.DoesNotContain(board.Entries, entry => entry.ParticipantId == viewer.Id);
        Assert.Equal(viewer.Id, board.ViewerEntry?.ParticipantId);
        Assert.True(board.IsViewerRanked);
    }

    [Fact]
    public async Task GetLeaderboardAsync_pages_with_opaque_cursors_and_jumps_to_name_match()
    {
        await using var db = CreateDb();
        var characters = Enumerable.Range(1, 25)
            .Select(index => AddCharacter(
                db,
                $"Player{index:D2}",
                level: 101 - index))
            .ToList();
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var firstPage = await repository.GetLeaderboardAsync(
            characters[0].Id,
            LeaderboardBoardKey.CombatLevel,
            10,
            null,
            null,
            CancellationToken.None);
        var secondPage = await repository.GetLeaderboardAsync(
            characters[0].Id,
            LeaderboardBoardKey.CombatLevel,
            10,
            firstPage.NextCursor,
            null,
            CancellationToken.None);
        var returnedFirstPage = await repository.GetLeaderboardAsync(
            characters[0].Id,
            LeaderboardBoardKey.CombatLevel,
            10,
            secondPage.PreviousCursor,
            null,
            CancellationToken.None);
        var searchPage = await repository.GetLeaderboardAsync(
            characters[0].Id,
            LeaderboardBoardKey.CombatLevel,
            10,
            null,
            "player17",
            CancellationToken.None);

        Assert.Equal((1, 10), (firstPage.PageStartRank, firstPage.PageEndRank));
        Assert.Null(firstPage.PreviousCursor);
        Assert.NotNull(firstPage.NextCursor);
        Assert.Equal(
            Enumerable.Range(11, 10),
            secondPage.Entries.Select(entry => entry.Rank));
        Assert.NotNull(secondPage.PreviousCursor);
        Assert.NotNull(secondPage.NextCursor);
        Assert.Equal(
            Enumerable.Range(1, 10),
            returnedFirstPage.Entries.Select(entry => entry.Rank));
        Assert.Equal((11, 20), (searchPage.PageStartRank, searchPage.PageEndRank));
        Assert.Equal(characters[16].Id, searchPage.SearchMatch?.ParticipantId);
        Assert.Equal(17, searchPage.SearchMatch?.Rank);
        Assert.Contains(
            searchPage.Entries,
            entry => entry.ParticipantId == characters[16].Id);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_soul_archive_completion()
    {
        await using var db = CreateDb();
        var collector = AddCharacter(db, "Collector", level: 1);
        var starter = AddCharacter(db, "Starter", level: 1);
        var empty = AddCharacter(db, "Empty", level: 1);
        AddEssence(db, collector.Id, "flame-imp");
        AddEssence(db, collector.Id, "large-rat");
        AddEssence(db, starter.Id, "flame-imp");
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            empty.Id,
            LeaderboardBoardKey.SoulArchiveCompletion,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(3, board.TotalParticipants);
        Assert.Equal(
            [collector.Id, starter.Id, empty.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([2L, 1L, 0L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.True(board.IsViewerRanked);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_achievement_renown()
    {
        await using var db = CreateDb();
        var renowned = AddCharacter(db, "Renowned", level: 1);
        var known = AddCharacter(db, "Known", level: 1);
        var newcomer = AddCharacter(db, "Newcomer", level: 1);
        var majorAchievement = AddAchievementDefinition(db, "major", points: 50);
        var minorAchievement = AddAchievementDefinition(db, "minor", points: 25);
        AddCompletedAchievement(db, renowned.UserId, majorAchievement.Id);
        AddCompletedAchievement(db, renowned.UserId, minorAchievement.Id);
        AddCompletedAchievement(db, known.UserId, majorAchievement.Id);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            newcomer.Id,
            LeaderboardBoardKey.AchievementRenown,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(3, board.TotalParticipants);
        Assert.Equal(
            [renowned.Id, known.Id, newcomer.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([75L, 50L, 0L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.Equal([2L, 1L, 0L], board.Entries.Select(entry => entry.SecondaryValue));
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_combined_dungeon_mastery_and_omits_nonparticipants()
    {
        await using var db = CreateDb();
        var broadMaster = AddCharacter(db, "BroadMaster", level: 1);
        var focusedMaster = AddCharacter(db, "FocusedMaster", level: 1);
        var unranked = AddCharacter(db, "Unranked", level: 1);
        AddMastery(db, broadMaster.Id, "crypt", level: 3, experience: 500);
        AddMastery(db, broadMaster.Id, "mine", level: 2, experience: 300);
        AddMastery(db, focusedMaster.Id, "crypt", level: 5, experience: 700);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            unranked.Id,
            LeaderboardBoardKey.DungeonMastery,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(2, board.TotalParticipants);
        Assert.Equal(
            [broadMaster.Id, focusedMaster.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([5L, 5L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.Equal([800L, 700L], board.Entries.Select(entry => entry.SecondaryValue));
        Assert.False(board.IsViewerRanked);
        Assert.Equal(
            "Complete a dungeon to begin earning Dungeon Mastery.",
            board.ViewerUnrankedReason);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_total_dungeon_clears_across_dungeons()
    {
        await using var db = CreateDb();
        var broadDelver = AddCharacter(db, "BroadDelver", level: 1);
        var focusedDelver = AddCharacter(db, "FocusedDelver", level: 1);
        var unranked = AddCharacter(db, "Unranked", level: 1);
        AddDungeonCompletion(db, broadDelver.Id, "crypt", completionCount: 10);
        AddDungeonCompletion(db, broadDelver.Id, "mine", completionCount: 5);
        AddDungeonCompletion(db, focusedDelver.Id, "crypt", completionCount: 14);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            unranked.Id,
            LeaderboardBoardKey.MostDungeonClears,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal(2, board.TotalParticipants);
        Assert.Equal(
            [broadDelver.Id, focusedDelver.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([15L, 14L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.Equal([2L, 1L], board.Entries.Select(entry => entry.SecondaryValue));
        Assert.False(board.IsViewerRanked);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_raid_kills_and_fastest_boss_slaying()
    {
        await using var db = CreateDb();
        var veteran = AddCharacter(db, "Veteran", level: 60);
        var sprinter = AddCharacter(db, "Sprinter", level: 60);
        AddRaidSlain(db, veteran.Id, "raid-boss.hives-abyss", 3900);
        AddRaidSlain(db, veteran.Id, "raid-boss.sanguine-horror", 3700);
        AddRaidSlain(db, sprinter.Id, "raid-boss.hives-abyss", 2800);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var kills = await repository.GetLeaderboardAsync(
            veteran.Id,
            LeaderboardBoardKey.RaidBossKills,
            10,
            null,
            null,
            CancellationToken.None);
        Assert.Equal([veteran.Id, sprinter.Id], kills.Entries.Select(x => x.ParticipantId));
        Assert.Equal([2L, 1L], kills.Entries.Select(x => x.PrimaryValue));

        var fastest = await repository.GetLeaderboardAsync(
            veteran.Id,
            LeaderboardBoardKey.FastestRaidSlain("raid-boss.hives-abyss"),
            10,
            null,
            null,
            CancellationToken.None);
        Assert.Equal([sprinter.Id, veteran.Id], fastest.Entries.Select(x => x.ParticipantId));
        Assert.Equal([2800L, 3900L], fastest.Entries.Select(x => x.PrimaryValue));
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_current_arena_rating_then_lifetime_high()
    {
        await using var db = CreateDb();
        var topLifetime = AddCharacter(db, "TopLifetime", level: 1);
        var runnerUp = AddCharacter(db, "RunnerUp", level: 1);
        var lowerRated = AddCharacter(db, "LowerRated", level: 1);
        var unranked = AddCharacter(db, "Unranked", level: 1);
        AddArenaProfile(db, topLifetime.Id, rating: 1_200, lifetimeHighestRating: 1_350);
        AddArenaProfile(db, runnerUp.Id, rating: 1_200, lifetimeHighestRating: 1_300);
        AddArenaProfile(db, lowerRated.Id, rating: 1_150, lifetimeHighestRating: 1_400);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            unranked.Id,
            LeaderboardBoardKey.ArenaRating,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal("PvP", board.Category);
        Assert.Equal("Current standings", board.PeriodLabel);
        Assert.Equal(3, board.TotalParticipants);
        Assert.Equal(
            [topLifetime.Id, runnerUp.Id, lowerRated.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([1_200L, 1_200L, 1_150L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.Equal([1_350L, 1_300L, 1_400L], board.Entries.Select(entry => entry.SecondaryValue));
        Assert.False(board.IsViewerRanked);
        Assert.Equal(
            "Enter the Colosseum to establish an Arena Rating.",
            board.ViewerUnrankedReason);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_current_month_tournament_points()
    {
        await using var db = CreateDb();
        var champion = AddCharacter(db, "Champion", level: 1);
        var finalist = AddCharacter(db, "Finalist", level: 1);
        var staleChampion = AddCharacter(db, "StaleChampion", level: 1);
        var monthStart = new DateTimeOffset(
            DateTimeOffset.UtcNow.Year,
            DateTimeOffset.UtcNow.Month,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        AddCompletedTournament(
            db,
            monthStart.AddDays(1),
            (champion, 1),
            (finalist, 2));
        AddCompletedTournament(
            db,
            monthStart.AddTicks(-1),
            (staleChampion, 1));
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            finalist.Id,
            LeaderboardBoardKey.TournamentPoints,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal("Current month", board.PeriodLabel);
        Assert.Equal(2, board.TotalParticipants);
        Assert.Equal(
            [champion.Id, finalist.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([100L, 60L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.Equal([1L, 0L], board.Entries.Select(entry => entry.SecondaryValue));
        Assert.DoesNotContain(
            board.Entries,
            entry => entry.ParticipantId == staleChampion.Id);
        Assert.Equal(finalist.Id, board.ViewerEntry?.ParticipantId);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_current_week_guild_contributors()
    {
        await using var db = CreateDb();
        var leader = AddCharacter(db, "WeeklyLeader", level: 1);
        var runnerUp = AddCharacter(db, "WeeklyRunnerUp", level: 1);
        var staleLeader = AddCharacter(db, "StaleLeader", level: 1);
        var currentWeek = GetCurrentGuildWeekKey();
        AddWeeklyContribution(db, leader.Id, currentWeek, score: 300, missionContribution: 80);
        AddWeeklyContribution(db, runnerUp.Id, currentWeek, score: 250, missionContribution: 100);
        AddWeeklyContribution(db, staleLeader.Id, "19990104", score: 999, missionContribution: 999);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            leader.Id,
            LeaderboardBoardKey.WeeklyGuildContribution,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal("Current week", board.PeriodLabel);
        Assert.Equal(2, board.TotalParticipants);
        Assert.Equal(
            [leader.Id, runnerUp.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal([300L, 250L], board.Entries.Select(entry => entry.PrimaryValue));
        Assert.Equal([80L, 100L], board.Entries.Select(entry => entry.SecondaryValue));
    }

    [Fact]
    public async Task GetLeaderboardAsync_ranks_guilds_once_and_returns_viewers_guild()
    {
        await using var db = CreateDb();
        var highOwner = AddCharacter(db, "HighOwner", level: 1);
        var viewer = AddCharacter(db, "Viewer", level: 1);
        var highGuild = AddGuild(db, highOwner, "High Guild", level: 5, experience: 45_000);
        var viewerGuild = AddGuild(db, viewer, "Viewer Guild", level: 4, experience: 39_000);
        await db.SaveChangesAsync();
        var repository = new LeaderboardRepository(db);

        var board = await repository.GetLeaderboardAsync(
            viewer.Id,
            LeaderboardBoardKey.GuildRenown,
            10,
            null,
            null,
            CancellationToken.None);

        Assert.Equal("Guild", board.ParticipantLabel);
        Assert.Equal(2, board.TotalParticipants);
        Assert.Equal(
            [highGuild.Id, viewerGuild.Id],
            board.Entries.Select(entry => entry.ParticipantId));
        Assert.Equal(["High Guild", "Viewer Guild"], board.Entries.Select(entry => entry.ParticipantName));
        Assert.Equal(viewerGuild.Id, board.ViewerEntry?.ParticipantId);
        Assert.True(board.IsViewerRanked);
    }

    private static Character AddCharacter(
        LLDbContext db,
        string name,
        int level)
    {
        var user = AppUser.Guest();
        user.Username = $"{name}-{Guid.NewGuid():N}";
        var character = new Character
        {
            Name = name,
            Level = level,
            Experience = level * 100,
            UserId = user.Id,
            User = user
        };

        db.Users.Add(user);
        db.Characters.Add(character);
        return character;
    }

    private static void AddEssence(LLDbContext db, Guid characterId, string definitionId)
    {
        db.PlayerEssences.Add(new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = definitionId
        });
    }

    private static AchievementDefinition AddAchievementDefinition(
        LLDbContext db,
        string key,
        int points)
    {
        var definition = new AchievementDefinition
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = key,
            Points = points
        };
        db.AchievementDefinitions.Add(definition);
        return definition;
    }

    private static void AddCompletedAchievement(
        LLDbContext db,
        Guid accountId,
        Guid definitionId)
    {
        db.PlayerAchievementProgresses.Add(new PlayerAchievementProgress
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            AchievementDefinitionId = definitionId,
            IsCompleted = true
        });
    }

    private static void AddMastery(
        LLDbContext db,
        Guid characterId,
        string dungeonId,
        int level,
        long experience)
    {
        db.CharacterDungeonMasteries.Add(new CharacterDungeonMastery
        {
            CharacterId = characterId,
            DungeonDefinitionId = dungeonId,
            Level = level,
            Experience = experience
        });
    }

    private static void AddDungeonCompletion(
        LLDbContext db,
        Guid characterId,
        string dungeonId,
        int completionCount)
    {
        db.DungeonCompletionRecords.Add(new DungeonCompletionRecord
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            DungeonDefinitionId = dungeonId,
            CompletionCount = completionCount
        });
    }

    private static void AddArenaProfile(
        LLDbContext db,
        Guid characterId,
        int rating,
        int lifetimeHighestRating)
    {
        db.CharacterArenaProfiles.Add(new CharacterArenaProfile
        {
            CharacterId = characterId,
            Rating = rating,
            LifetimeHighestRating = lifetimeHighestRating
        });
    }

    private static void AddRaidSlain(
        LLDbContext db,
        Guid characterId,
        string raidBossId,
        int durationTicks)
    {
        var run = new RaidRun
        {
            Id = Guid.NewGuid(),
            RaidBossId = raidBossId,
            Tier = 1,
            DefinitionHash = "test",
            DefinitionSnapshotJson = "{}",
            LeaderCharacterId = characterId,
            Status = RaidRunStatus.Settled,
            Outcome = RaidOutcome.Slain,
            CreatedAt = DateTimeOffset.UtcNow,
            SignupClosesAt = DateTimeOffset.UtcNow,
            ResolvedAt = DateTimeOffset.UtcNow
        };
        run.LaneResults.Add(new RaidLaneResult
        {
            RaidRun = run,
            RaidRunId = run.Id,
            Lane = RaidLane.Vanguard,
            DurationTicks = durationTicks
        });
        run.ParticipantResults.Add(new RaidParticipantResult
        {
            RaidRun = run,
            RaidRunId = run.Id,
            CharacterId = characterId,
            Lane = RaidLane.Vanguard,
            ContributionRank = 1
        });
        db.RaidRuns.Add(run);
    }

    private static void AddCompletedTournament(
        LLDbContext db,
        DateTimeOffset completedAt,
        params (Character Character, int Placement)[] placements)
    {
        var tournament = new TournamentInstance
        {
            Id = Guid.NewGuid(),
            DefinitionId = Guid.NewGuid(),
            TournamentNumber = db.ArenaTournaments.Count() + 1,
            Name = $"Tournament {completedAt:yyyyMMddHHmmss}",
            Status = TournamentStatus.Completed,
            RegistrationStartsAtUtc = completedAt.AddDays(-2),
            RegistrationEndsAtUtc = completedAt.AddDays(-1),
            StartsAtUtc = completedAt.AddHours(-1),
            CompletedAtUtc = completedAt,
            MinParticipants = 2,
            MaxParticipants = 32,
            RoundIntervalMinutes = 5,
            RegisteredParticipantCount = placements.Length,
            CreatedAtUtc = completedAt.AddDays(-2),
            UpdatedAtUtc = completedAt
        };
        db.ArenaTournaments.Add(tournament);

        foreach (var (character, placement) in placements)
        {
            db.TournamentParticipants.Add(new TournamentParticipant
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                Tournament = tournament,
                CharacterId = character.Id,
                AccountId = character.UserId,
                SnapshotId = Guid.NewGuid(),
                EntryArenaRating = 1_000,
                EntryRankTier = "Bronze",
                Status = placement == 1
                    ? TournamentParticipantStatus.Champion
                    : TournamentParticipantStatus.Eliminated,
                FinalPlacement = placement,
                RegisteredAtUtc = tournament.RegistrationStartsAtUtc,
                UpdatedAtUtc = completedAt
            });
        }
    }

    private static void AddWeeklyContribution(
        LLDbContext db,
        Guid characterId,
        string periodKey,
        long score,
        long missionContribution)
    {
        db.GuildMemberContributionPeriods.Add(new GuildMemberContributionPeriod
        {
            Id = Guid.NewGuid(),
            GuildId = Guid.NewGuid(),
            CharacterId = characterId,
            PeriodType = GuildMissionPeriodType.Weekly,
            PeriodKey = periodKey,
            ContributionScore = score,
            WeeklyMissionContribution = missionContribution
        });
    }

    private static Guild AddGuild(
        LLDbContext db,
        Character owner,
        string name,
        int level,
        long experience)
    {
        var guild = new Guild
        {
            Id = Guid.NewGuid(),
            Name = name,
            OwnerId = owner.Id,
            GuildLevel = level,
            GuildXp = experience
        };
        db.Guilds.Add(guild);
        db.GuildMembers.Add(new GuildMember
        {
            GuildId = guild.Id,
            CharacterId = owner.Id,
            Role = GuildRole.Leader
        });
        return guild;
    }

    private static string GetCurrentGuildWeekKey()
    {
        var utcDate = DateTimeOffset.UtcNow.UtcDateTime.Date;
        var daysSinceMonday =
            ((int)utcDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return utcDate.AddDays(-daysSinceMonday).ToString("yyyyMMdd");
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
