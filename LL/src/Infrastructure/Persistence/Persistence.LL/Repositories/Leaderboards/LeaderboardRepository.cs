using Application.Common.Interfaces;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Guilds.Missions;
using Domain.Models.Leaderboards;
using Domain.Models.Professions;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Leaderboards;

public sealed class LeaderboardRepository : ILeaderboardRepository
{
    private readonly IDbContext _context;

    public LeaderboardRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<LeaderboardBoard> GetLeaderboardAsync(
        Guid characterId,
        string boardKey,
        int limit,
        string? cursor,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedKey = boardKey.Trim().ToLowerInvariant();
        if (!LeaderboardBoardKey.IsKnown(normalizedKey))
        {
            throw new ArgumentOutOfRangeException(
                nameof(boardKey),
                boardKey,
                "Unknown leaderboard board key.");
        }

        var now = DateTimeOffset.UtcNow;
        var definition = GetDefinition(normalizedKey);
        var scores = definition.Profession is { } profession
            ? await GetProfessionScoresAsync(profession, cancellationToken)
            : LeaderboardBoardKey.TryGetFastestRaidBossId(normalizedKey, out var fastestRaidBossId)
                ? await GetFastestRaidSlainScoresAsync(fastestRaidBossId, cancellationToken)
            : normalizedKey switch
            {
                LeaderboardBoardKey.SoulArchiveCompletion =>
                    await GetSoulArchiveScoresAsync(cancellationToken),
                LeaderboardBoardKey.AchievementRenown =>
                    await GetAchievementRenownScoresAsync(cancellationToken),
                LeaderboardBoardKey.DungeonMastery =>
                    await GetDungeonMasteryScoresAsync(cancellationToken),
                LeaderboardBoardKey.MostDungeonClears =>
                    await GetMostDungeonClearsScoresAsync(cancellationToken),
                LeaderboardBoardKey.ArenaRating =>
                    await GetArenaRatingScoresAsync(cancellationToken),
                LeaderboardBoardKey.TournamentPoints =>
                    await GetTournamentPointsScoresAsync(now, cancellationToken),
                LeaderboardBoardKey.WeeklyGuildContribution =>
                    await GetWeeklyGuildContributionScoresAsync(now, cancellationToken),
                LeaderboardBoardKey.GuildRenown =>
                    await GetGuildRenownScoresAsync(cancellationToken),
                LeaderboardBoardKey.RaidBossKills =>
                    await GetRaidBossKillScoresAsync(cancellationToken),
                _ => await GetCharacterScoresAsync(normalizedKey, cancellationToken)
            };
        var ranked = LeaderboardRanking.Rank(scores, definition.PrimaryAscending).ToList();
        var viewerParticipantId = definition.IsGuildBoard
            ? await GetViewerGuildIdAsync(characterId, cancellationToken)
            : characterId;
        var viewer = viewerParticipantId is { } participantId
            ? ranked.FirstOrDefault(x => x.ParticipantId == participantId)
            : null;
        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim();
        var searchMatch = normalizedSearch is null
            ? null
            : FindParticipant(ranked, normalizedSearch);
        var pageStartIndex = GetPageStartIndex(
            ranked,
            normalizedKey,
            limit,
            cursor,
            searchMatch);
        var entries = ranked
            .Skip(pageStartIndex)
            .Take(limit)
            .ToList();
        var previousCursor = pageStartIndex > 0 && entries.Count > 0
            ? LeaderboardCursor.Encode(
                normalizedKey,
                LeaderboardCursorDirection.Before,
                entries[0].ParticipantId)
            : null;
        var nextCursor = pageStartIndex + entries.Count < ranked.Count && entries.Count > 0
            ? LeaderboardCursor.Encode(
                normalizedKey,
                LeaderboardCursorDirection.After,
                entries[^1].ParticipantId)
            : null;

        return new LeaderboardBoard
        {
            Key = definition.Key,
            Category = definition.Category,
            Title = definition.Title,
            Description = definition.Description,
            ParticipantLabel = definition.ParticipantLabel,
            MetricLabel = definition.MetricLabel,
            SecondaryMetricLabel = definition.SecondaryMetricLabel,
            PeriodLabel = definition.PeriodLabel,
            UpdatedAt = now,
            TotalParticipants = ranked.Count,
            PageStartRank = entries.FirstOrDefault()?.Rank ?? 0,
            PageEndRank = entries.LastOrDefault()?.Rank ?? 0,
            PreviousCursor = previousCursor,
            NextCursor = nextCursor,
            SearchQuery = normalizedSearch,
            SearchMatch = searchMatch,
            IsViewerRanked = viewer is not null,
            ViewerUnrankedReason = GetViewerUnrankedReason(
                viewer,
                definition.UnrankedReason),
            Entries = entries,
            ViewerEntry = viewer
        };
    }

    private static LeaderboardBoardEntry? FindParticipant(
        IReadOnlyList<LeaderboardBoardEntry> ranked,
        string search)
    {
        return ranked.FirstOrDefault(entry =>
                entry.ParticipantName.Equals(search, StringComparison.OrdinalIgnoreCase))
            ?? ranked.FirstOrDefault(entry =>
                entry.ParticipantName.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            ?? ranked.FirstOrDefault(entry =>
                entry.ParticipantName.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetPageStartIndex(
        IReadOnlyList<LeaderboardBoardEntry> ranked,
        string boardKey,
        int limit,
        string? cursor,
        LeaderboardBoardEntry? searchMatch)
    {
        if (searchMatch is not null)
        {
            return ((searchMatch.Rank - 1) / limit) * limit;
        }

        if (!LeaderboardCursor.TryDecode(boardKey, cursor, out var position))
        {
            return 0;
        }

        var anchorIndex = -1;
        for (var index = 0; index < ranked.Count; index++)
        {
            if (ranked[index].ParticipantId == position.AnchorParticipantId)
            {
                anchorIndex = index;
                break;
            }
        }

        if (anchorIndex < 0)
        {
            return 0;
        }

        return position.Direction == LeaderboardCursorDirection.After
            ? Math.Min(anchorIndex + 1, ranked.Count)
            : Math.Max(0, anchorIndex - limit);
    }

    private async Task<List<LeaderboardScore>> GetCharacterScoresAsync(
        string boardKey,
        CancellationToken cancellationToken)
    {
        var query = EligibleCharacters();

        if (boardKey == LeaderboardBoardKey.CombatLevel)
        {
            return await query
                .Select(x => new LeaderboardScore(
                    x.Id,
                    x.Name,
                    x.Level,
                    x.Experience))
                .ToListAsync(cancellationToken);
        }

        return await query
            .Select(x => new LeaderboardScore(
                x.Id,
                x.Name,
                x.Level + x.Professions.Sum(p => p.Level),
                null))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetProfessionScoresAsync(
        ProfessionType profession,
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .SelectMany(
                character => character.Professions
                    .Where(characterProfession => characterProfession.ProfessionType == profession),
                (character, characterProfession) => new LeaderboardScore(
                    character.Id,
                    character.Name,
                    characterProfession.Level,
                    (long)characterProfession.Experience))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetSoulArchiveScoresAsync(
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .Select(character => new LeaderboardScore(
                character.Id,
                character.Name,
                _context.PlayerEssences.LongCount(
                    essence => essence.CharacterId == character.Id),
                null))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetAchievementRenownScoresAsync(
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .Select(character => new LeaderboardScore(
                character.Id,
                character.Name,
                _context.PlayerAchievementProgresses
                    .Where(progress =>
                        progress.AccountId == character.UserId &&
                        progress.IsCompleted)
                    .Join(
                        _context.AchievementDefinitions,
                        progress => progress.AchievementDefinitionId,
                        definition => definition.Id,
                        (_, definition) => (long?)definition.Points)
                    .Sum() ?? 0,
                _context.PlayerAchievementProgresses.LongCount(
                    progress =>
                        progress.AccountId == character.UserId &&
                        progress.IsCompleted)))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetDungeonMasteryScoresAsync(
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .Where(character => _context.CharacterDungeonMasteries.Any(
                mastery => mastery.CharacterId == character.Id))
            .Select(character => new LeaderboardScore(
                character.Id,
                character.Name,
                _context.CharacterDungeonMasteries
                    .Where(mastery => mastery.CharacterId == character.Id)
                    .Sum(mastery => (long?)mastery.Level) ?? 0,
                _context.CharacterDungeonMasteries
                    .Where(mastery => mastery.CharacterId == character.Id)
                    .Sum(mastery => (long?)mastery.Experience) ?? 0))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetMostDungeonClearsScoresAsync(
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .Where(character => _context.DungeonCompletionRecords.Any(
                completion => completion.CharacterId == character.Id))
            .Select(character => new LeaderboardScore(
                character.Id,
                character.Name,
                _context.DungeonCompletionRecords
                    .Where(completion => completion.CharacterId == character.Id)
                    .Sum(completion => (long?)completion.CompletionCount) ?? 0,
                _context.DungeonCompletionRecords.LongCount(
                    completion => completion.CharacterId == character.Id)))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetWeeklyGuildContributionScoresAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var weekKey = GetGuildWeekKey(now);
        return await _context.GuildMemberContributionPeriods
            .AsNoTracking()
            .Where(period =>
                period.PeriodType == GuildMissionPeriodType.Weekly &&
                period.PeriodKey == weekKey &&
                (period.ContributionScore > 0 ||
                    period.WeeklyMissionContribution > 0 ||
                    period.OrdersCompleted > 0))
            .Join(
                EligibleCharacters(),
                period => period.CharacterId,
                character => character.Id,
                (period, character) => new LeaderboardScore(
                    character.Id,
                    character.Name,
                    period.ContributionScore,
                    period.WeeklyMissionContribution))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetArenaRatingScoresAsync(
        CancellationToken cancellationToken)
    {
        return await _context.CharacterArenaProfiles
            .AsNoTracking()
            .Join(
                EligibleCharacters(),
                profile => profile.CharacterId,
                character => character.Id,
                (profile, character) => new LeaderboardScore(
                    character.Id,
                    character.Name,
                    profile.Rating,
                    profile.LifetimeHighestRating))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetTournamentPointsScoresAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var seasonStart = new DateTimeOffset(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        var seasonEnd = seasonStart.AddMonths(1);
        var placements = await _context.TournamentParticipants
            .AsNoTracking()
            .Where(participant =>
                participant.Tournament.Status == TournamentStatus.Completed &&
                participant.Tournament.CompletedAtUtc >= seasonStart &&
                participant.Tournament.CompletedAtUtc < seasonEnd &&
                participant.FinalPlacement.HasValue &&
                !_context.AccountRestrictions.Any(restriction =>
                    restriction.AccountId == participant.AccountId &&
                    restriction.RevokedAt == null &&
                    (restriction.ExpiresAt == null || restriction.ExpiresAt > now) &&
                    (restriction.RestrictionType == Domain.Models.Administration.AccountRestrictionType.Ban ||
                     restriction.RestrictionType == Domain.Models.Administration.AccountRestrictionType.MultiplayerRestriction)))
            .Select(participant => new
            {
                participant.CharacterId,
                Placement = participant.FinalPlacement!.Value,
                CompletedAt = participant.Tournament.CompletedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);

        if (placements.Count == 0)
        {
            return [];
        }

        var characterIds = placements
            .Select(placement => placement.CharacterId)
            .Distinct()
            .ToList();
        var characterNames = await EligibleCharacters()
            .Where(character => characterIds.Contains(character.Id))
            .Select(character => new { character.Id, character.Name })
            .ToDictionaryAsync(
                character => character.Id,
                character => character.Name,
                cancellationToken);

        return placements
            .GroupBy(placement => placement.CharacterId)
            .Select(group =>
            {
                var bestPlacement = group.Min(placement => placement.Placement);
                var latestCompletion = group.Max(placement => placement.CompletedAt);
                return new LeaderboardScore(
                    group.Key,
                    characterNames.GetValueOrDefault(group.Key, "Unknown"),
                    group.Sum(placement =>
                        TournamentScoring.CalculatePoints(placement.Placement)),
                    group.LongCount(placement => placement.Placement == 1),
                    -bestPlacement,
                    latestCompletion.UtcDateTime.Ticks);
            })
            .ToList();
    }

    private async Task<List<LeaderboardScore>> GetGuildRenownScoresAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Guilds
            .AsNoTracking()
            .Select(guild => new LeaderboardScore(
                guild.Id,
                guild.Name,
                guild.GuildLevel,
                guild.GuildXp))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetRaidBossKillScoresAsync(
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .Where(character => _context.RaidParticipantResults.Any(result =>
                result.CharacterId == character.Id
                && result.RaidRun.Outcome == Domain.Models.Raids.RaidOutcome.Slain
                && (result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Resolved
                    || result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Settled)))
            .Select(character => new LeaderboardScore(
                character.Id,
                character.Name,
                _context.RaidParticipantResults.LongCount(result =>
                    result.CharacterId == character.Id
                    && result.RaidRun.Outcome == Domain.Models.Raids.RaidOutcome.Slain
                    && (result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Resolved
                        || result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Settled)),
                _context.RaidParticipantResults
                    .Where(result => result.CharacterId == character.Id
                                     && result.RaidRun.Outcome == Domain.Models.Raids.RaidOutcome.Slain
                                     && (result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Resolved
                                         || result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Settled))
                    .Select(result => result.RaidRun.RaidBossId)
                    .Distinct()
                    .LongCount()))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<LeaderboardScore>> GetFastestRaidSlainScoresAsync(
        string raidBossId,
        CancellationToken cancellationToken)
    {
        return await EligibleCharacters()
            .Where(character => _context.RaidParticipantResults.Any(result =>
                result.CharacterId == character.Id
                && result.RaidRun.RaidBossId == raidBossId
                && result.RaidRun.Outcome == Domain.Models.Raids.RaidOutcome.Slain
                && (result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Resolved
                    || result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Settled)))
            .Select(character => new LeaderboardScore(
                character.Id,
                character.Name,
                _context.RaidLaneResults
                    .Where(lane => lane.Lane == Domain.Models.Raids.RaidLane.Vanguard
                                   && lane.RaidRun.RaidBossId == raidBossId
                                   && lane.RaidRun.Outcome == Domain.Models.Raids.RaidOutcome.Slain
                                   && (lane.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Resolved
                                       || lane.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Settled)
                                   && lane.RaidRun.ParticipantResults.Any(result => result.CharacterId == character.Id))
                    .Min(lane => (long)lane.DurationTicks),
                _context.RaidParticipantResults.LongCount(result =>
                    result.CharacterId == character.Id
                    && result.RaidRun.RaidBossId == raidBossId
                    && result.RaidRun.Outcome == Domain.Models.Raids.RaidOutcome.Slain
                    && (result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Resolved
                        || result.RaidRun.Status == Domain.Models.Raids.RaidRunStatus.Settled))))
            .ToListAsync(cancellationToken);
    }

    private async Task<Guid?> GetViewerGuildIdAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        return await _context.GuildMembers
            .AsNoTracking()
            .Where(member => member.CharacterId == characterId)
            .Select(member => (Guid?)member.GuildId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Domain.Models.Entities.Characters.Character> EligibleCharacters()
    {
        var now = DateTimeOffset.UtcNow;
        return _context.Characters
            .AsNoTracking()
            .Where(character => !_context.AccountRestrictions.Any(restriction =>
                restriction.AccountId == character.UserId &&
                restriction.RevokedAt == null &&
                (restriction.ExpiresAt == null || restriction.ExpiresAt > now) &&
                (restriction.RestrictionType == Domain.Models.Administration.AccountRestrictionType.Ban ||
                 restriction.RestrictionType == Domain.Models.Administration.AccountRestrictionType.MultiplayerRestriction)));
    }

    private static string GetGuildWeekKey(DateTimeOffset now)
    {
        var utcDate = now.UtcDateTime.Date;
        var daysSinceMonday =
            ((int)utcDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return utcDate.AddDays(-daysSinceMonday).ToString("yyyyMMdd");
    }

    private static string? GetViewerUnrankedReason(
        LeaderboardBoardEntry? viewer,
        string? unrankedReason)
    {
        return viewer is null
            ? unrankedReason ?? "No ranking is available for this character yet."
            : null;
    }

    private static BoardDefinition GetDefinition(string boardKey) => boardKey switch
    {
        LeaderboardBoardKey.TotalLevel => new(
            boardKey,
            "Overall",
            "Total Level",
            "Combined combat and profession levels across your character.",
            "Total level",
            null),
        LeaderboardBoardKey.CombatLevel => new(
            boardKey,
            "Overall",
            "Combat Level",
            "The realm's most experienced adventurers.",
            "Combat level",
            "Experience"),
        LeaderboardBoardKey.SoulArchiveCompletion => new(
            boardKey,
            "Overall",
            "Soul Archive Completion",
            "The realm's most dedicated Essence collectors.",
            "Essences collected",
            null),
        LeaderboardBoardKey.AchievementRenown => new(
            boardKey,
            "Overall",
            "Achievement Renown",
            "Recognition earned by completing achievements across the realm.",
            "Achievement points",
            "Achievements completed"),
        LeaderboardBoardKey.DungeonMastery => new(
            boardKey,
            "PvE",
            "Dungeon Mastery",
            "Combined mastery earned across every dungeon.",
            "Mastery levels",
            "Mastery experience",
            UnrankedReason: "Complete a dungeon to begin earning Dungeon Mastery."),
        LeaderboardBoardKey.MostDungeonClears => new(
            boardKey,
            "PvE",
            "Most Dungeon Clears",
            "The realm's most persistent dungeon delvers.",
            "Dungeon clears",
            "Dungeons completed",
            UnrankedReason: "Complete a dungeon to earn a place on this leaderboard."),
        LeaderboardBoardKey.ArenaRating => new(
            boardKey,
            "PvP",
            "Arena Rating",
            "The realm's highest-rated Colosseum contenders.",
            "Arena rating",
            "Lifetime highest",
            UnrankedReason: "Enter the Colosseum to establish an Arena Rating.",
            PeriodLabel: "Current standings"),
        LeaderboardBoardKey.TournamentPoints => new(
            boardKey,
            "PvP",
            "Tournament Points",
            "Points earned from completed Colosseum tournaments during the current month.",
            "Tournament points",
            "Championships",
            UnrankedReason: "Complete a tournament this month to earn Tournament Points.",
            PeriodLabel: "Current month"),
        LeaderboardBoardKey.WeeklyGuildContribution => new(
            boardKey,
            "Guilds",
            "Weekly Guild Contribution",
            "The characters contributing most to their guild this week.",
            "Contribution score",
            "Mission contribution",
            UnrankedReason: "Contribute to a guild activity this week to earn a place.",
            PeriodLabel: "Current week"),
        LeaderboardBoardKey.GuildRenown => new(
            boardKey,
            "Guilds",
            "Guild Renown",
            "The realm's most established guilds.",
            "Guild level",
            "Guild experience",
            UnrankedReason: "Join a guild to see its standing.",
            ParticipantLabel: "Guild",
            IsGuildBoard: true),
        LeaderboardBoardKey.RaidBossKills => new(
            boardKey,
            "PvE",
            "Raid Boss Kills",
            "Raid boss victories earned across the realm.",
            "Victorious raids",
            "Raid bosses slain",
            UnrankedReason: "Slay a raid boss to earn a place on this leaderboard."),
        _ when LeaderboardBoardKey.TryGetFastestRaidBossId(boardKey, out var raidBossId) => new(
            boardKey,
            "PvE",
            "Fastest Raid Boss Slain",
            $"Fastest Vanguard victories against {raidBossId}.",
            "Duration (ticks)",
            "Victories",
            UnrankedReason: "Slay this raid boss to record a time.",
            PrimaryAscending: true),
        LeaderboardBoardKey.Crafting => ProfessionDefinition(boardKey, ProfessionType.Crafting),
        LeaderboardBoardKey.Mining => ProfessionDefinition(boardKey, ProfessionType.Mining),
        LeaderboardBoardKey.Woodcutting => ProfessionDefinition(boardKey, ProfessionType.Woodcutting),
        LeaderboardBoardKey.Skinning => ProfessionDefinition(boardKey, ProfessionType.Skinning),
        _ => throw new ArgumentOutOfRangeException(nameof(boardKey), boardKey, null)
    };

    private static BoardDefinition ProfessionDefinition(
        string boardKey,
        ProfessionType profession) => new(
            boardKey,
            "Professions",
            profession.ToString(),
            $"The realm's most accomplished {profession.ToString().ToLowerInvariant()} specialists.",
            "Level",
            "Experience",
            profession,
            $"Start {profession} to earn a place on this leaderboard.");

    private sealed record BoardDefinition(
        string Key,
        string Category,
        string Title,
        string Description,
        string MetricLabel,
        string? SecondaryMetricLabel,
        ProfessionType? Profession = null,
        string? UnrankedReason = null,
        string ParticipantLabel = "Character",
        string PeriodLabel = "All-time",
        bool IsGuildBoard = false,
        bool PrimaryAscending = false);
}
