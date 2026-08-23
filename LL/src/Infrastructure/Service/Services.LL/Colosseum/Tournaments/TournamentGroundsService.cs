using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Outbox;
using Application.UseCases.Colosseum.Tournaments;
using Application.UseCases.Inventories.SelectionCrates;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Snapshots;
using Domain.Models.Essences;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics.Metrics;
using TournamentGroundsUpdated = Application.WebSockets.Contracts.TournamentGroundsUpdated;

namespace Services.LL.Colosseum.Tournaments;

public sealed class TournamentGroundsService : ITournamentGroundsService
{
    private const int PlaybackKeyframeIntervalTicks = 30 * FastCombatEngine.TicksPerSecond;
    private const string TournamentGroundsTargetUrl = "/game/city/colosseum?tab=tournaments";
    private static readonly Meter TournamentMeter = new("LegendsLegacy.TournamentGrounds");
    private static readonly Histogram<double> CombatDurationMilliseconds =
        TournamentMeter.CreateHistogram<double>("tournament_ground.combat.engine.duration", "ms");
    private static readonly Histogram<long> CombatAllocatedBytes =
        TournamentMeter.CreateHistogram<long>("tournament_ground.combat.engine.allocated", "By");
    private static readonly Histogram<long> PlaybackBundleBytes =
        TournamentMeter.CreateHistogram<long>("tournament_ground.playback.bundle.size", "By");
    private static readonly IReadOnlyList<TournamentRewardTierOptions> DefaultRewardTiers =
    [
        new() { Key = "champion", MaxPlacement = 1, ArenaGlory = 500, Soulstones = 50, CatalystSelectionCaches = 1, BlueprintSelectionBoxes = 1, SigilFragments = 20 },
        new() { Key = "finalist", MaxPlacement = 2, ArenaGlory = 425, Soulstones = 40, CatalystSelectionCaches = 1, BlueprintSelectionBoxes = 1, SigilFragments = 20 },
        new() { Key = "semi-finalist", MaxPlacement = 4, ArenaGlory = 350, Soulstones = 30, CatalystSelectionCaches = 1, BlueprintSelectionBoxes = 1 },
        new() { Key = "quarter-finalist", MaxPlacement = 8, ArenaGlory = 300, Soulstones = 25, CatalystSelectionCaches = 1 },
        new() { Key = "participant", MaxPlacement = null, ArenaGlory = 250, Soulstones = 20 }
    ];

    private static readonly JsonSerializerOptions ReplayJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ITournamentGroundsRepository _tournaments;
    private readonly IEntityService _entityService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly ICharacterSnapshotService _characterSnapshotService;
    private readonly IItemBaseRepository _itemBaseRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly ICombatEngineExecutor _combatEngineExecutor;
    private readonly ICombatEncounterResultFactory _combatEncounterResultFactory;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly IStateSyncService _stateSync;
    private readonly ITournamentLockService _tournamentLockService;
    private readonly TimeProvider _timeProvider;
    private readonly TournamentGroundsOptions _options;
    private readonly IAchievementService? _achievementService;
    private readonly IGameEventOutbox? _outbox;

    public TournamentGroundsService(
        ITournamentGroundsRepository tournaments,
        IEntityService entityService,
        ICombatSetupService combatSetupService,
        ICharacterSnapshotService characterSnapshotService,
        IItemBaseRepository itemBaseRepository,
        IInventoryService inventoryService,
        IInventoryItemFactory inventoryItemFactory,
        ICombatEngineExecutor combatEngineExecutor,
        ICombatEncounterResultFactory combatEncounterResultFactory,
        IGameRealtimeBroadcaster gameRealtime,
        ITournamentLockService tournamentLockService,
        IStateSyncService stateSync,
        TimeProvider timeProvider,
        IOptions<TournamentGroundsOptions> options,
        IAchievementService? achievementService = null,
        IGameEventOutbox? outbox = null)
    {
        _tournaments = tournaments;
        _entityService = entityService;
        _combatSetupService = combatSetupService;
        _characterSnapshotService = characterSnapshotService;
        _itemBaseRepository = itemBaseRepository;
        _inventoryService = inventoryService;
        _inventoryItemFactory = inventoryItemFactory;
        _combatEngineExecutor = combatEngineExecutor;
        _combatEncounterResultFactory = combatEncounterResultFactory;
        _gameRealtime = gameRealtime;
        _tournamentLockService = tournamentLockService;
        _stateSync = stateSync;
        _timeProvider = timeProvider;
        _options = options.Value;
        _achievementService = achievementService;
        _outbox = outbox;
    }

    public async Task EnsureUpcomingTournamentsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;

        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentScheduleAsync(cancellationToken);

        var now = UtcNow();
        var definition = await EnsureDefaultDefinitionAsync(now, cancellationToken);
        await AlignUpcomingTournamentStartsAsync(definition, now, cancellationToken);

        var hasUpcoming = await _tournaments.Tournaments.AnyAsync(t =>
            t.DefinitionId == definition.Id &&
            t.Status != TournamentStatus.Completed &&
            t.Status != TournamentStatus.Cancelled &&
            t.RegistrationEndsAtUtc >= now.AddDays(-7),
            cancellationToken);

        if (hasUpcoming)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var registrationWindow = BuildNextRegistrationWindow(now);

        var nextNumber = await _tournaments.Tournaments
            .Select(t => (int?)t.TournamentNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var tournament = new TournamentInstance
        {
            Id = Guid.NewGuid(),
            DefinitionId = definition.Id,
            TournamentNumber = nextNumber + 1,
            Name = definition.Name,
            Status = TournamentStatus.Scheduled,
            RegistrationStartsAtUtc = registrationWindow.StartsAtUtc,
            RegistrationEndsAtUtc = registrationWindow.EndsAtUtc,
            StartsAtUtc = registrationWindow.EndsAtUtc.AddMinutes(definition.StartDelayAfterRegistrationMinutes),
            MinParticipants = definition.MinParticipants,
            MaxParticipants = definition.MaxParticipants,
            RoundIntervalMinutes = definition.RoundIntervalMinutes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _tournaments.AddAsync(tournament, cancellationToken);
        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AdvanceDueTournamentsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;

        var now = UtcNow();
        var preparationCutoff = now.AddSeconds(
            Math.Max(0, _options.MatchPreparationLeadSeconds));
        var dueIds = await _tournaments.Tournaments
            .Where(t => t.Status != TournamentStatus.Completed && t.Status != TournamentStatus.Cancelled)
            .Where(t =>
                (t.Status == TournamentStatus.Scheduled && t.RegistrationStartsAtUtc <= now) ||
                (t.Status == TournamentStatus.RegistrationOpen && t.RegistrationEndsAtUtc <= now) ||
                (t.Status == TournamentStatus.RegistrationClosed) ||
                (t.Status == TournamentStatus.BracketGenerated && t.StartsAtUtc <= preparationCutoff) ||
                (t.Status == TournamentStatus.InProgress &&
                    _tournaments.Rounds.Any(r => r.TournamentId == t.Id &&
                        r.Status != TournamentRoundStatus.Completed &&
                        r.StartsAtUtc <= preparationCutoff)))
            .Select(t => t.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var id in dueIds)
        {
            await AdvanceTournamentAsync(id, cancellationToken);
        }
    }

    public async Task<StartDevelopmentTournamentResult> StartDevelopmentTournamentAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        if (!_options.DevelopmentToolsEnabled)
        {
            return StartDevelopmentTournamentResult.Failure(
                "Tournament Grounds development tools are disabled.");
        }

        if (!_options.Enabled)
        {
            return StartDevelopmentTournamentResult.Failure(
                "Tournament Grounds are disabled.");
        }

        await EnsureUpcomingTournamentsAsync(cancellationToken);

        var tournamentId = await _tournaments.Tournaments
            .Where(t => t.Status != TournamentStatus.Completed && t.Status != TournamentStatus.Cancelled)
            .OrderBy(t => t.RegistrationStartsAtUtc)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!tournamentId.HasValue)
        {
            return StartDevelopmentTournamentResult.Failure(
                "No tournament was available to start.");
        }

        var tournament = await OpenDevelopmentRegistrationAsync(
            tournamentId.Value,
            cancellationToken);
        if (tournament is null)
        {
            return StartDevelopmentTournamentResult.Failure(
                "Only a scheduled or registering tournament can be started with development tools.");
        }

        var playerIsRegistered = await _tournaments.Participants.AnyAsync(
            participant =>
                participant.TournamentId == tournament.Id
                && participant.CharacterId == characterId
                && participant.Status != TournamentParticipantStatus.Withdrawn,
            cancellationToken);
        if (!playerIsRegistered)
        {
            var playerRegistration = await RegisterAsync(
                characterId,
                tournament.Id,
                cancellationToken);
            if (playerRegistration is null)
            {
                return StartDevelopmentTournamentResult.Failure(
                    "Your character could not be registered. Ensure it has an Arena profile and meets the tournament requirements.");
            }
        }

        var registeredCount = await _tournaments.Participants.CountAsync(
            participant =>
                participant.TournamentId == tournament.Id
                && participant.Status != TournamentParticipantStatus.Withdrawn,
            cancellationToken);
        var desiredRegisteredCount = GetMaxRegisteredParticipants(tournament);

        var registeredAccountIds = await _tournaments.Participants
            .Where(participant =>
                participant.TournamentId == tournament.Id
                && participant.Status != TournamentParticipantStatus.Withdrawn)
            .Select(participant => participant.AccountId)
            .ToListAsync(cancellationToken);
        var candidates = await _tournaments.Characters
            .AsNoTracking()
            .Where(candidate =>
                candidate.User.IsGuest
                && candidate.User.Username.StartsWith("SeedGuest")
                && candidate.ArenaProfile != null
                && !registeredAccountIds.Contains(candidate.UserId))
            .OrderBy(candidate => candidate.Name)
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken);

        foreach (var candidateId in candidates)
        {
            if (registeredCount >= desiredRegisteredCount)
            {
                break;
            }

            var registration = await RegisterAsync(
                candidateId,
                tournament.Id,
                cancellationToken);
            if (registration is not null)
            {
                registeredCount++;
            }
        }

        if (registeredCount < desiredRegisteredCount)
        {
            return StartDevelopmentTournamentResult.Failure(
                $"Only {registeredCount} of {desiredRegisteredCount} required development participants were available. Restart the API with local guest seeding enabled.");
        }

        var closedForStart = await CloseDevelopmentRegistrationAsync(
            tournament.Id,
            registeredCount,
            cancellationToken);
        if (!closedForStart)
        {
            return StartDevelopmentTournamentResult.Failure(
                "The tournament changed state before it could be started.");
        }

        await AdvanceTournamentAsync(tournament.Id, cancellationToken);

        var finalStatus = await _tournaments.Tournaments
            .Where(item => item.Id == tournament.Id)
            .Select(item => item.Status)
            .SingleAsync(cancellationToken);
        var teamCount = await _tournaments.Teams.CountAsync(
            team => team.TournamentId == tournament.Id
                    && team.Status != TournamentTeamStatus.Disbanded,
            cancellationToken);

        if (finalStatus is not TournamentStatus.InProgress and not TournamentStatus.Completed)
        {
            return StartDevelopmentTournamentResult.Failure(
                $"The test tournament reached {finalStatus} instead of starting.");
        }

        return StartDevelopmentTournamentResult.Success(
            tournament.Id,
            registeredCount,
            teamCount);
    }

    public async Task<TournamentGroundsStatus> GetStatusAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var tournaments = await _tournaments.Tournaments
            .Where(t => t.Status != TournamentStatus.Completed && t.Status != TournamentStatus.Cancelled)
            .OrderBy(t => t.RegistrationStartsAtUtc)
            .Take(4)
            .ToListAsync(cancellationToken);

        var summaries = new List<TournamentSummary>();
        foreach (var tournament in tournaments)
        {
            summaries.Add(await MapSummaryAsync(tournament, characterId, now, cancellationToken));
        }

        var recentTournaments = await _tournaments.Tournaments
            .Where(t => t.Status == TournamentStatus.Completed || t.Status == TournamentStatus.Cancelled)
            .OrderByDescending(t => t.CompletedAtUtc ?? t.CancelledAtUtc ?? t.UpdatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentSummaries = new List<TournamentSummary>();
        foreach (var tournament in recentTournaments)
        {
            recentSummaries.Add(await MapSummaryAsync(tournament, characterId, now, cancellationToken));
        }

        return new TournamentGroundsStatus(
            now,
            summaries.FirstOrDefault(),
            summaries.Skip(1).ToList(),
            recentSummaries,
            _options.DevelopmentToolsEnabled);
    }

    public async Task<TournamentDetails?> GetDetailsAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null) return null;

        var participants = await MapParticipantsAsync(tournamentId, cancellationToken);
        var teams = await MapTeamsAsync(tournamentId, characterId, cancellationToken);
        var rewards = await GetRewardsAsync(characterId, tournamentId, cancellationToken);
        return new TournamentDetails(await MapSummaryAsync(tournament, characterId, now, cancellationToken), participants, teams, rewards);
    }

    public async Task<IReadOnlyList<TournamentHistoryEntry>> GetHistoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var entries = await _tournaments.Participants
            .Include(p => p.Tournament)
            .Where(p => p.CharacterId == characterId)
            .Where(p => p.Tournament.Status == TournamentStatus.Completed || p.Tournament.Status == TournamentStatus.Cancelled)
            .OrderByDescending(p => p.Tournament.CompletedAtUtc ?? p.Tournament.CancelledAtUtc ?? p.Tournament.UpdatedAtUtc)
            .Take(30)
            .Select(p => new
            {
                Participant = p,
                RewardStatus = _tournaments.RewardGrants
                    .Where(r => r.TournamentId == p.TournamentId && r.CharacterId == characterId)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Select(r => (TournamentRewardStatus?)r.Status)
                    .FirstOrDefault(),
                ReplayCount = _tournaments.CombatReplays.Count(r => r.TournamentId == p.TournamentId)
            })
            .ToListAsync(cancellationToken);

        return entries.Select(entry => new TournamentHistoryEntry(
            entry.Participant.TournamentId,
            entry.Participant.Tournament.TournamentNumber,
            entry.Participant.Tournament.Name,
            entry.Participant.Tournament.Status.ToString(),
            entry.Participant.Tournament.CompletedAtUtc,
            entry.Participant.Tournament.CancelledAtUtc,
            entry.Participant.Tournament.CancellationReason,
            entry.Participant.Id,
            entry.Participant.Seed,
            entry.Participant.EntryArenaRating,
            entry.Participant.EntryRankTier,
            entry.Participant.Status.ToString(),
            entry.Participant.FinalPlacement,
            entry.RewardStatus?.ToString(),
            entry.ReplayCount)).ToList();
    }

    public async Task<IReadOnlyList<TournamentHallOfFameEntry>> GetHallOfFameAsync(CancellationToken cancellationToken)
    {
        var championTeams = await _tournaments.Teams
            .Include(t => t.Tournament)
            .Where(t => t.Status == TournamentTeamStatus.Champion)
            .Where(t => t.Tournament.Status == TournamentStatus.Completed && t.Tournament.CompletedAtUtc.HasValue)
            .OrderByDescending(t => t.Tournament.CompletedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (championTeams.Count == 0) return [];

        var ownerIds = championTeams.Select(t => t.OwnerParticipantId).ToList();
        var owners = await EligibleParticipants(UtcNow())
            .Where(p => ownerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var characterIds = owners.Values.Select(p => p.CharacterId).ToList();
        var characterNames = await _tournaments.Characters
            .Where(c => characterIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var entries = new List<TournamentHallOfFameEntry>();
        foreach (var team in championTeams)
        {
            if (!owners.TryGetValue(team.OwnerParticipantId, out var owner))
            {
                continue;
            }

            entries.Add(new TournamentHallOfFameEntry(
                team.TournamentId,
                team.Tournament.TournamentNumber,
                team.Tournament.Name,
                team.Tournament.CompletedAtUtc!.Value,
                team.Tournament.RegisteredParticipantCount,
                owner.Id,
                owner.CharacterId,
                characterNames.GetValueOrDefault(owner.CharacterId, "Unknown"),
                team.Seed,
                owner.EntryArenaRating,
                owner.EntryRankTier,
                await _tournaments.CombatReplays.CountAsync(r => r.TournamentId == team.TournamentId, cancellationToken)));
        }

        return entries;
    }

    public async Task<IReadOnlyList<TournamentSeasonLeaderboardEntry>> GetSeasonLeaderboardAsync(CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var seasonStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var seasonEnd = seasonStart.AddMonths(1);
        var seasonKey = seasonStart.ToString("yyyy-MM");

        var placements = await EligibleParticipants(now)
            .Include(p => p.Tournament)
            .Where(p => p.Tournament.Status == TournamentStatus.Completed)
            .Where(p => p.Tournament.CompletedAtUtc >= seasonStart && p.Tournament.CompletedAtUtc < seasonEnd)
            .Where(p => p.FinalPlacement.HasValue)
            .Select(p => new
            {
                p.CharacterId,
                p.FinalPlacement,
                p.Tournament.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);

        if (placements.Count == 0) return [];

        var characterIds = placements.Select(p => p.CharacterId).Distinct().ToList();
        var characterNames = await _tournaments.Characters
            .Where(c => characterIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return placements
            .GroupBy(p => p.CharacterId)
            .Select(group =>
            {
                var bestPlacement = group.Min(p => p.FinalPlacement);
                return new
                {
                    CharacterId = group.Key,
                    CharacterName = characterNames.GetValueOrDefault(group.Key, "Unknown"),
                    Points = group.Sum(p => TournamentScoring.CalculatePoints(p.FinalPlacement)),
                    TournamentsEntered = group.Count(),
                    Championships = group.Count(p => p.FinalPlacement == 1),
                    FinalistFinishes = group.Count(p => p.FinalPlacement <= 2),
                    BestPlacement = bestPlacement,
                    LatestCompletedAtUtc = group.Max(p => p.CompletedAtUtc)
                };
            })
            .OrderByDescending(entry => entry.Points)
            .ThenByDescending(entry => entry.Championships)
            .ThenBy(entry => entry.BestPlacement ?? int.MaxValue)
            .ThenByDescending(entry => entry.LatestCompletedAtUtc)
            .Take(20)
            .Select((entry, index) => new TournamentSeasonLeaderboardEntry(
                index + 1,
                entry.CharacterId,
                entry.CharacterName,
                entry.Points,
                entry.TournamentsEntered,
                entry.Championships,
                entry.FinalistFinishes,
                entry.BestPlacement,
                entry.LatestCompletedAtUtc,
                seasonKey))
            .ToList();
    }

    public async Task<TournamentBracket?> GetBracketAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null) return null;

        var teamMap = (await MapTeamsAsync(tournamentId, characterId, cancellationToken))
            .ToDictionary(t => t.TeamId);

        var rounds = await _tournaments.Rounds
            .Where(r => r.TournamentId == tournamentId)
            .Include(r => r.Matches)
            .OrderBy(r => r.RoundNumber)
            .ToListAsync(cancellationToken);
        var replayMatchIds = (await _tournaments.CombatReplays
                .Where(replay => replay.TournamentId == tournamentId
                                 && replay.Match.PlaybackStartedAtUtc.HasValue
                                 && replay.Match.PlaybackStartedAtUtc.Value <= now)
                .Select(replay => replay.MatchId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return new TournamentBracket(
            tournament.Id,
            tournament.Status.ToString(),
            rounds.Select(r => new TournamentBracketRound(
                r.Id,
                r.RoundNumber,
                r.Name,
                r.Status.ToString(),
                r.StartsAtUtc,
                r.ResolvedAtUtc,
                r.Matches
                    .OrderBy(m => m.MatchNumber)
                    .Select(m => new TournamentBracketMatch(
                        m.Id,
                        m.RoundNumber,
                        m.MatchNumber,
                        m.Status.ToString(),
                        m.Outcome.ToString(),
                        m.PlayerOneParticipantId.HasValue && teamMap.TryGetValue(m.PlayerOneParticipantId.Value, out var p1) ? p1 : null,
                        m.PlayerTwoParticipantId.HasValue && teamMap.TryGetValue(m.PlayerTwoParticipantId.Value, out var p2) ? p2 : null,
                        m.WinnerParticipantId,
                        m.CombatSessionId,
                        m.BattleHistoryId,
                        m.ScheduledAtUtc,
                        m.PlaybackStartedAtUtc,
                        m.PlaybackEndsAtUtc,
                        replayMatchIds.Contains(m.Id)))
                    .ToList()))
                .ToList());
    }

    public async Task<CombatResult?> GetMatchReplayAsync(
        Guid characterId,
        Guid tournamentId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var replay = await _tournaments.CombatReplays
            .FirstOrDefaultAsync(r => r.TournamentId == tournamentId
                                      && r.MatchId == matchId
                                      && r.Match.PlaybackStartedAtUtc.HasValue
                                      && r.Match.PlaybackStartedAtUtc.Value <= now,
                cancellationToken);
        if (replay is null) return null;

        if (string.IsNullOrWhiteSpace(replay.CombatResultJson)) return null;
        return JsonSerializer.Deserialize<CombatResult>(replay.CombatResultJson, ReplayJsonOptions);
    }

    public async Task<TournamentPlaybackManifestDto?> GetMatchPlaybackAsync(
        Guid characterId,
        Guid tournamentId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var playback = await _tournaments.CombatReplays
            .Where(replay => replay.TournamentId == tournamentId
                             && replay.MatchId == matchId
                             && replay.SchemaVersion >= TournamentCombatReplay.MinimumCompactBundleSchemaVersion)
            .Select(replay => new
            {
                replay.SchemaVersion,
                replay.TicksPerSecond,
                replay.TicksPerFrame,
                replay.Duration,
                replay.FrameCount,
                replay.BundleHash,
                replay.Match.PlaybackStartedAtUtc,
                replay.Match.PlaybackEndsAtUtc,
                replay.Match.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (playback is null
            || !playback.PlaybackStartedAtUtc.HasValue
            || !playback.PlaybackEndsAtUtc.HasValue
            || string.IsNullOrWhiteSpace(playback.BundleHash))
            return null;

        var elapsedTicks = Math.Max(0, (int)Math.Floor(
            (now - playback.PlaybackStartedAtUtc.Value).TotalSeconds * playback.TicksPerSecond));
        var currentSequence = now >= playback.PlaybackEndsAtUtc.Value
            ? Math.Max(0, playback.FrameCount - 1)
            : Math.Clamp(
                elapsedTicks / Math.Max(1, playback.TicksPerFrame),
                0,
                Math.Max(0, playback.FrameCount - 1));
        return new TournamentPlaybackManifestDto(
            tournamentId,
            matchId,
            playback.SchemaVersion,
            playback.TicksPerSecond,
            playback.TicksPerFrame,
            playback.Duration,
            GetCombatDurationTicks(_options.RegulationDurationMinutes, playback.TicksPerSecond),
            GetCombatDurationTicks(_options.OvertimeDurationMinutes, playback.TicksPerSecond),
            checked(_options.OvertimePowerIncreaseIntervalSeconds * playback.TicksPerSecond),
            _options.OvertimePowerIncreasePercent,
            playback.FrameCount,
            playback.PlaybackStartedAtUtc.Value,
            playback.PlaybackEndsAtUtc.Value,
            now,
            currentSequence,
            playback.Status == TournamentMatchStatus.Completed,
            playback.BundleHash);
    }

    public async Task<TournamentPlaybackBundleContentDto?> GetMatchPlaybackBundleAsync(
        Guid characterId,
        Guid tournamentId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var bundle = await _tournaments.CombatReplays
            .Where(replay => replay.TournamentId == tournamentId
                             && replay.MatchId == matchId
                             && replay.SchemaVersion >= TournamentCombatReplay.MinimumCompactBundleSchemaVersion
                             && replay.Match.PlaybackStartedAtUtc.HasValue
                             && replay.Match.PlaybackStartedAtUtc.Value <= now)
            .Select(replay => new
            {
                replay.BundleHash,
                replay.BundleContentType,
                replay.BundleContentEncoding,
                Bytes = replay.Artifact!.BundleBytes
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (bundle is null
            || string.IsNullOrWhiteSpace(bundle.BundleHash)
            || string.IsNullOrWhiteSpace(bundle.BundleContentType)
            || string.IsNullOrWhiteSpace(bundle.BundleContentEncoding))
            return null;

        return new TournamentPlaybackBundleContentDto(
            bundle.Bytes,
            bundle.BundleContentType,
            bundle.BundleContentEncoding,
            bundle.BundleHash);
    }

    public async Task<IReadOnlyList<TournamentRewardGrantEntry>> GetRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken)
    {
        var query = _tournaments.RewardGrants
            .Include(r => r.Tournament)
            .Where(r => r.CharacterId == characterId);

        if (tournamentId.HasValue)
        {
            query = query.Where(r => r.TournamentId == tournamentId.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new TournamentRewardGrantEntry(
                r.Id,
                r.TournamentId,
                r.Tournament.Name,
                r.RewardKey,
                r.Placement,
                r.ArenaGlory,
                r.Cinders,
                r.Soulstones,
                r.CatalystSelectionCaches,
                r.BlueprintSelectionBoxes,
                r.SigilFragments,
                r.Status.ToString(),
                r.CreatedAtUtc,
                r.ClaimedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public IReadOnlyList<TournamentRewardTier> GetRewardTiers() =>
        GetConfiguredRewardTiers()
            .Select(tier => new TournamentRewardTier(
                tier.Key,
                tier.MaxPlacement,
                tier.ArenaGlory,
                tier.Cinders,
                tier.Soulstones,
                tier.CatalystSelectionCaches,
                tier.BlueprintSelectionBoxes,
                tier.SigilFragments))
            .ToList();

    public async Task<RegisterTournamentResult?> RegisterAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return null;

        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null || tournament.Status != TournamentStatus.RegistrationOpen) return null;
        if (now < tournament.RegistrationStartsAtUtc || now >= tournament.RegistrationEndsAtUtc) return null;
        if (tournament.RegisteredParticipantCount >= GetMaxRegisteredParticipants(tournament)) return null;

        var character = await _tournaments.Characters
            .Include(c => c.ArenaProfile)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);
        if (character?.ArenaProfile is null) return null;

        var definition = await _tournaments.Definitions.FirstOrDefaultAsync(d => d.Id == tournament.DefinitionId, cancellationToken);
        if (definition is null || !MeetsEligibility(character, definition)) return null;

        var existing = await _tournaments.Participants
            .AnyAsync(p => p.TournamentId == tournamentId && p.Status != TournamentParticipantStatus.Withdrawn &&
                (p.CharacterId == characterId || p.AccountId == character.UserId), cancellationToken);
        if (existing) return null;

        var withdrawnParticipant = await _tournaments.Participants
            .FirstOrDefaultAsync(p =>
                p.TournamentId == tournamentId &&
                p.Status == TournamentParticipantStatus.Withdrawn &&
                (p.CharacterId == characterId || p.AccountId == character.UserId),
                cancellationToken);
        if (withdrawnParticipant is not null && withdrawnParticipant.CharacterId != characterId)
        {
            return null;
        }

        var snapshot = await _characterSnapshotService.CreateAsync(characterId, EssenceCombatActivity.Tournament, cancellationToken);
        var tier = ArenaRank.GetTier(character.ArenaProfile.Rating);
        var tournamentSnapshot = await _tournaments.CombatSnapshots
            .FirstOrDefaultAsync(s => s.TournamentId == tournamentId && s.CharacterId == characterId, cancellationToken);
        if (tournamentSnapshot is null)
        {
            tournamentSnapshot = new TournamentCombatSnapshot
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                CharacterId = characterId,
                CreatedAtUtc = now
            };
            await _tournaments.AddAsync(tournamentSnapshot, cancellationToken);
        }

        tournamentSnapshot.CharacterSnapshotId = snapshot.Id;
        tournamentSnapshot.CharacterSnapshot = snapshot;
        tournamentSnapshot.SnapshotVersion = "character-snapshot-v1";
        tournamentSnapshot.SnapshotJson = BuildSnapshotJson(
            snapshot,
            character.ArenaProfile.Rating,
            tier.Id,
            now);
        tournamentSnapshot.ArenaRatingAtSnapshot = character.ArenaProfile.Rating;
        tournamentSnapshot.RankTierAtSnapshot = tier.Id;

        var participant = withdrawnParticipant ?? new TournamentParticipant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            CharacterId = characterId,
            AccountId = character.UserId,
            RegisteredAtUtc = now
        };

        participant.SnapshotId = tournamentSnapshot.Id;
        participant.Snapshot = tournamentSnapshot;
        participant.TeamId = null;
        participant.Team = null;
        participant.IsTeamOwner = false;
        participant.Seed = null;
        participant.EntryArenaRating = character.ArenaProfile.Rating;
        participant.EntryRankTier = tier.Name;
        participant.Status = TournamentParticipantStatus.Registered;
        participant.EliminatedInRoundNumber = null;
        participant.FinalPlacement = null;
        participant.RegisteredAtUtc = now;
        participant.UpdatedAtUtc = now;

        tournament.RegisteredParticipantCount++;
        tournament.UpdatedAtUtc = now;
        if (withdrawnParticipant is null)
        {
            await _tournaments.AddAsync(participant, cancellationToken);
        }

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            "TournamentRegistrationUpdated",
            now,
            cancellationToken);

        return new RegisterTournamentResult(
            true,
            participant.Id,
            tournamentSnapshot.Id,
            participant.EntryArenaRating,
            participant.EntryRankTier,
            "Registered. Your current combat setup has been saved for this tournament. You can update it while registration is open after joining a team.");
    }

    public async Task<TournamentTeamActionResult?> UpdateLoadoutAsync(
        Guid characterId,
        Guid tournamentId,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return null;

        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(
            item => item.Id == tournamentId,
            cancellationToken);
        if (tournament is null) return null;
        if (tournament.Status != TournamentStatus.RegistrationOpen
            || now < tournament.RegistrationStartsAtUtc
            || now >= tournament.RegistrationEndsAtUtc)
        {
            return new TournamentTeamActionResult(
                false,
                "Tournament loadouts can only be updated while registration is open.");
        }

        var participant = await _tournaments.Participants.FirstOrDefaultAsync(
            item => item.TournamentId == tournamentId
                    && item.CharacterId == characterId
                    && item.Status != TournamentParticipantStatus.Withdrawn,
            cancellationToken);
        if (participant is null)
        {
            return new TournamentTeamActionResult(
                false,
                "Register for this tournament before updating your loadout.");
        }

        if (!participant.TeamId.HasValue)
        {
            return new TournamentTeamActionResult(
                false,
                "Join a tournament team before updating your loadout.");
        }

        var tournamentSnapshot = await _tournaments.CombatSnapshots.FirstOrDefaultAsync(
            item => item.Id == participant.SnapshotId
                    && item.TournamentId == tournamentId
                    && item.CharacterId == characterId,
            cancellationToken);
        if (tournamentSnapshot is null) return null;

        var snapshot = await _characterSnapshotService.CreateAsync(
            characterId,
            EssenceCombatActivity.Tournament,
            cancellationToken);
        tournamentSnapshot.CharacterSnapshotId = snapshot.Id;
        tournamentSnapshot.CharacterSnapshot = snapshot;
        tournamentSnapshot.SnapshotVersion = "character-snapshot-v1";
        tournamentSnapshot.SnapshotJson = BuildSnapshotJson(
            snapshot,
            tournamentSnapshot.ArenaRatingAtSnapshot,
            tournamentSnapshot.RankTierAtSnapshot,
            now);
        tournamentSnapshot.CreatedAtUtc = now;
        participant.UpdatedAtUtc = now;

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            "TournamentLoadoutUpdated",
            now,
            cancellationToken);

        return new TournamentTeamActionResult(true);
    }

    public async Task<WithdrawTournamentResult?> WithdrawAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.AllowWithdrawDuringRegistration) return null;

        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null || tournament.Status != TournamentStatus.RegistrationOpen || now >= tournament.RegistrationEndsAtUtc) return null;

        var participant = await _tournaments.Participants
            .FirstOrDefaultAsync(p => p.TournamentId == tournamentId && p.CharacterId == characterId, cancellationToken);
        if (participant is null || participant.Status == TournamentParticipantStatus.Withdrawn) return null;

        if (participant.TeamId.HasValue)
        {
            var team = await _tournaments.Teams
                .FirstOrDefaultAsync(t => t.Id == participant.TeamId.Value, cancellationToken);
            if (team is not null)
            {
                if (team.OwnerParticipantId == participant.Id)
                {
                    var replacementOwner = await _tournaments.Participants
                        .Where(p => p.TeamId == team.Id && p.Id != participant.Id && p.Status != TournamentParticipantStatus.Withdrawn)
                        .OrderBy(p => p.RegisteredAtUtc)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (replacementOwner is null)
                    {
                        team.Status = TournamentTeamStatus.Disbanded;
                        team.MemberCount = 0;
                    }
                    else
                    {
                        replacementOwner.IsTeamOwner = true;
                        replacementOwner.UpdatedAtUtc = now;
                        team.OwnerParticipantId = replacementOwner.Id;
                        team.MemberCount = Math.Max(0, team.MemberCount - 1);
                    }
                }
                else
                {
                    team.MemberCount = Math.Max(0, team.MemberCount - 1);
                }

                team.UpdatedAtUtc = now;
            }
        }

        await CancelPendingRequestsForParticipantAsync(tournamentId, participant.Id, now, cancellationToken);
        participant.Status = TournamentParticipantStatus.Withdrawn;
        participant.TeamId = null;
        participant.Team = null;
        participant.IsTeamOwner = false;
        participant.UpdatedAtUtc = now;
        tournament.RegisteredParticipantCount = Math.Max(0, tournament.RegisteredParticipantCount - 1);
        tournament.UpdatedAtUtc = now;
        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            "TournamentRegistrationUpdated",
            now,
            cancellationToken);

        return new WithdrawTournamentResult(true);
    }

    public async Task<CreateTournamentTeamResult?> CreateTeamAsync(
        Guid characterId,
        Guid tournamentId,
        string name,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return null;

        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null || tournament.Status != TournamentStatus.RegistrationOpen || now >= tournament.RegistrationEndsAtUtc) return null;

        var participant = await GetRegisteredParticipantAsync(characterId, tournamentId, cancellationToken);
        if (participant is null || participant.TeamId.HasValue) return null;
        if (await GetCurrentTeamCountAsync(tournamentId, cancellationToken) >= tournament.MaxParticipants) return null;

        var teamName = NormalizeTeamName(name, participant.CharacterId);
        var nameExists = await _tournaments.Teams.AnyAsync(t => t.TournamentId == tournamentId && t.Name == teamName, cancellationToken);
        if (nameExists) return null;

        var team = new TournamentTeam
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            Name = teamName,
            OwnerParticipantId = participant.Id,
            Status = TournamentTeamStatus.Forming,
            MemberCount = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        participant.TeamId = team.Id;
        participant.Team = team;
        participant.IsTeamOwner = true;
        participant.UpdatedAtUtc = now;

        await _tournaments.AddAsync(team, cancellationToken);
        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            "TournamentTeamUpdated",
            now,
            cancellationToken);

        return new CreateTournamentTeamResult(true, team.Id);
    }

    public async Task<TournamentTeamActionResult?> InviteToTeamAsync(
        Guid characterId,
        Guid tournamentId,
        Guid teamId,
        Guid invitedParticipantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        if (!await CanMutateTeamsAsync(tournamentId, now, cancellationToken)) return null;

        var team = await _tournaments.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.TournamentId == tournamentId, cancellationToken);
        if (team is null || team.MemberCount >= 3) return null;
        if (!await IsTeamOwnerAsync(characterId, team, cancellationToken)) return null;

        var invited = await _tournaments.Participants.FirstOrDefaultAsync(p => p.Id == invitedParticipantId && p.TournamentId == tournamentId, cancellationToken);
        if (invited is null || invited.Status == TournamentParticipantStatus.Withdrawn || invited.TeamId.HasValue) return null;

        var exists = await _tournaments.TeamInvites.AnyAsync(i =>
            i.TeamId == teamId &&
            i.InvitedParticipantId == invitedParticipantId &&
            i.Status == TournamentTeamRequestStatus.Pending,
            cancellationToken);
        if (exists) return new TournamentTeamActionResult(true);

        var inviter = await GetRegisteredParticipantAsync(characterId, tournamentId, cancellationToken);
        if (inviter is null) return null;

        await _tournaments.AddAsync(new TournamentTeamInvite
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            TeamId = teamId,
            InviterParticipantId = inviter.Id,
            InvitedParticipantId = invitedParticipantId,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournamentId,
            "TournamentTeamUpdated",
            now,
            cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> AcceptTeamInviteAsync(Guid characterId, Guid inviteId, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);

        var tournamentId = await _tournaments.TeamInvites
            .Where(i => i.Id == inviteId)
            .Select(i => (Guid?)i.TournamentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!tournamentId.HasValue)
            return new TournamentTeamActionResult(false, "This tournament team invite is no longer available.");

        await _tournamentLockService.LockTournamentAsync(tournamentId.Value, cancellationToken);

        var invite = await _tournaments.TeamInvites
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);
        if (invite is null || invite.Status != TournamentTeamRequestStatus.Pending)
            return new TournamentTeamActionResult(false, "This tournament team invite is no longer available.");

        var now = UtcNow();
        if (!await CanMutateTeamsAsync(invite.TournamentId, now, cancellationToken))
            return new TournamentTeamActionResult(false, "Tournament teams can no longer be changed.");

        if (invite.Team.Status != TournamentTeamStatus.Forming)
        {
            await CancelPendingRequestsForTeamAsync(invite.TournamentId, invite.TeamId, now, cancellationToken);
            await SaveCommitAndPublishTournamentEventAsync(
                transaction,
                invite.TournamentId,
                "TournamentTeamUpdated",
                now,
                cancellationToken);
            return new TournamentTeamActionResult(false, "This tournament team invite is no longer available.");
        }

        var memberCount = await GetActiveTeamMemberCountAsync(invite.TeamId, cancellationToken);
        invite.Team.MemberCount = memberCount;
        if (memberCount >= 3)
        {
            invite.Team.UpdatedAtUtc = now;
            await CancelPendingRequestsForTeamAsync(invite.TournamentId, invite.TeamId, now, cancellationToken);
            await SaveCommitAndPublishTournamentEventAsync(
                transaction,
                invite.TournamentId,
                "TournamentTeamUpdated",
                now,
                cancellationToken);
            return new TournamentTeamActionResult(
                false,
                "That tournament team is already full. Ask the team owner for a new invite if a slot opens.");
        }

        var participant = await GetRegisteredParticipantAsync(characterId, invite.TournamentId, cancellationToken);
        if (participant is null || participant.Id != invite.InvitedParticipantId) return null;
        if (participant.TeamId.HasValue)
        {
            await CancelPendingRequestsForParticipantAsync(invite.TournamentId, participant.Id, now, cancellationToken);
            await SaveCommitAndPublishTournamentEventAsync(
                transaction,
                invite.TournamentId,
                "TournamentTeamUpdated",
                now,
                cancellationToken);
            return new TournamentTeamActionResult(false, "You already belong to a tournament team.");
        }

        participant.TeamId = invite.TeamId;
        participant.IsTeamOwner = false;
        participant.UpdatedAtUtc = now;
        invite.Team.MemberCount = memberCount + 1;
        invite.Team.UpdatedAtUtc = now;
        await CancelPendingRequestsForParticipantAsync(invite.TournamentId, participant.Id, now, cancellationToken);
        if (invite.Team.MemberCount >= 3)
        {
            await CancelPendingRequestsForTeamAsync(invite.TournamentId, invite.TeamId, now, cancellationToken);
        }
        invite.Status = TournamentTeamRequestStatus.Accepted;
        invite.UpdatedAtUtc = now;

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            invite.TournamentId,
            "TournamentTeamUpdated",
            now,
            cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> ApplyToTeamAsync(
        Guid characterId,
        Guid tournamentId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        if (!await CanMutateTeamsAsync(tournamentId, now, cancellationToken)) return null;

        var team = await _tournaments.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.TournamentId == tournamentId, cancellationToken);
        if (team is null || team.MemberCount >= 3) return null;

        var participant = await GetRegisteredParticipantAsync(characterId, tournamentId, cancellationToken);
        if (participant is null || participant.TeamId.HasValue) return null;

        var exists = await _tournaments.TeamApplications.AnyAsync(a =>
            a.TeamId == teamId &&
            a.ApplicantParticipantId == participant.Id &&
            a.Status == TournamentTeamRequestStatus.Pending,
            cancellationToken);
        if (exists) return new TournamentTeamActionResult(true);

        await _tournaments.AddAsync(new TournamentTeamApplication
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            TeamId = teamId,
            ApplicantParticipantId = participant.Id,
            Status = TournamentTeamRequestStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);
        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournamentId,
            "TournamentTeamUpdated",
            now,
            cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> AcceptTeamApplicationAsync(
        Guid characterId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);

        var tournamentId = await _tournaments.TeamApplications
            .Where(a => a.Id == applicationId)
            .Select(a => (Guid?)a.TournamentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (!tournamentId.HasValue)
            return new TournamentTeamActionResult(false, "This tournament team application is no longer available.");

        await _tournamentLockService.LockTournamentAsync(tournamentId.Value, cancellationToken);

        var application = await _tournaments.TeamApplications
            .Include(a => a.Team)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (application is null || application.Status != TournamentTeamRequestStatus.Pending)
            return new TournamentTeamActionResult(false, "This tournament team application is no longer available.");

        var now = UtcNow();
        if (!await CanMutateTeamsAsync(application.TournamentId, now, cancellationToken))
            return new TournamentTeamActionResult(false, "Tournament teams can no longer be changed.");

        if (!await IsTeamOwnerAsync(characterId, application.Team, cancellationToken)) return null;

        if (application.Team.Status != TournamentTeamStatus.Forming)
        {
            await CancelPendingRequestsForTeamAsync(application.TournamentId, application.TeamId, now, cancellationToken);
            await SaveCommitAndPublishTournamentEventAsync(
                transaction,
                application.TournamentId,
                "TournamentTeamUpdated",
                now,
                cancellationToken);
            return new TournamentTeamActionResult(false, "This tournament team application is no longer available.");
        }

        var memberCount = await GetActiveTeamMemberCountAsync(application.TeamId, cancellationToken);
        application.Team.MemberCount = memberCount;
        if (memberCount >= 3)
        {
            application.Team.UpdatedAtUtc = now;
            await CancelPendingRequestsForTeamAsync(application.TournamentId, application.TeamId, now, cancellationToken);
            await SaveCommitAndPublishTournamentEventAsync(
                transaction,
                application.TournamentId,
                "TournamentTeamUpdated",
                now,
                cancellationToken);
            return new TournamentTeamActionResult(false, "That tournament team is already full.");
        }

        var participant = await _tournaments.Participants
            .FirstOrDefaultAsync(p => p.Id == application.ApplicantParticipantId, cancellationToken);
        if (participant is null || participant.TeamId.HasValue || participant.Status == TournamentParticipantStatus.Withdrawn) return null;

        participant.TeamId = application.TeamId;
        participant.IsTeamOwner = false;
        participant.UpdatedAtUtc = now;
        application.Team.MemberCount = memberCount + 1;
        application.Team.UpdatedAtUtc = now;
        await CancelPendingRequestsForParticipantAsync(application.TournamentId, participant.Id, now, cancellationToken);
        if (application.Team.MemberCount >= 3)
        {
            await CancelPendingRequestsForTeamAsync(application.TournamentId, application.TeamId, now, cancellationToken);
        }
        application.Status = TournamentTeamRequestStatus.Accepted;
        application.UpdatedAtUtc = now;

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            application.TournamentId,
            "TournamentTeamUpdated",
            now,
            cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> KickTeamMemberAsync(
        Guid characterId,
        Guid tournamentId,
        Guid teamId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var now = UtcNow();
        if (!await CanMutateTeamsAsync(tournamentId, now, cancellationToken)) return null;

        var team = await _tournaments.Teams.FirstOrDefaultAsync(t => t.Id == teamId && t.TournamentId == tournamentId, cancellationToken);
        if (team is null || !await IsTeamOwnerAsync(characterId, team, cancellationToken)) return null;
        if (team.OwnerParticipantId == participantId) return null;

        var participant = await _tournaments.Participants.FirstOrDefaultAsync(p => p.Id == participantId && p.TeamId == teamId, cancellationToken);
        if (participant is null) return null;

        participant.TeamId = null;
        participant.IsTeamOwner = false;
        participant.UpdatedAtUtc = now;
        team.MemberCount = Math.Max(0, team.MemberCount - 1);
        team.UpdatedAtUtc = now;

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournamentId,
            "TournamentTeamUpdated",
            now,
            cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<ClaimTournamentRewardsResult> ClaimRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);

        var now = UtcNow();
        var rewardTournamentId = tournamentId ?? await _tournaments.RewardGrants
            .Where(r =>
                r.CharacterId == characterId
                && r.Status == TournamentRewardStatus.Unclaimed)
            .Select(r => (Guid?)r.TournamentId)
            .FirstOrDefaultAsync(cancellationToken);
        var result = await ClaimUnclaimedRewardsAsync(
            characterId,
            tournamentId,
            now,
            cancellationToken);
        if (!result.Claimed)
        {
            return result;
        }

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            rewardTournamentId!.Value,
            "TournamentRewardsAvailable",
            now,
            cancellationToken);

        return result;
    }

    private async Task<ClaimTournamentRewardsResult> ClaimUnclaimedRewardsAsync(
        Guid characterId,
        Guid? tournamentId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {

        var character = await _tournaments.Characters
            .Include(c => c.ArenaProfile)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);
        if (character?.ArenaProfile is null)
        {
            return EmptyClaimResult();
        }

        var query = _tournaments.RewardGrants
            .Where(r => r.CharacterId == characterId && r.Status == TournamentRewardStatus.Unclaimed);
        if (tournamentId.HasValue)
        {
            query = query.Where(r => r.TournamentId == tournamentId.Value);
        }

        var rewards = await query.ToListAsync(cancellationToken);
        if (rewards.Count == 0)
        {
            return EmptyClaimResult();
        }

        var glory = rewards.Sum(r => r.ArenaGlory);
        var cinders = rewards.Sum(r => r.Cinders);
        var soulstones = rewards.Sum(r => r.Soulstones);
        var sigilFragments = rewards.Sum(r => r.SigilFragments);
        var catalystSelectionCaches = rewards.Sum(r => r.CatalystSelectionCaches);
        var blueprintSelectionBoxes = rewards.Sum(r => r.BlueprintSelectionBoxes);

        character.ArenaProfile.Glory += glory;
        character.Cinders += cinders;
        character.Soulstones += soulstones;
        character.SigilFragments += sigilFragments;

        var inventoryRewards = await CreateRewardInventoryItemsAsync(
            characterId,
            catalystSelectionCaches,
            blueprintSelectionBoxes,
            cancellationToken);
        if (inventoryRewards.Count > 0)
        {
            await _inventoryService.AddItemsToInventory(
                characterId,
                inventoryRewards,
                ItemAcquisitionSources.TournamentReward,
                cancellationToken);
        }

        foreach (var reward in rewards)
        {
            reward.Status = TournamentRewardStatus.Claimed;
            reward.ClaimedAtUtc = now;
        }

        return new ClaimTournamentRewardsResult(
            true,
            glory,
            cinders,
            soulstones,
            sigilFragments,
            catalystSelectionCaches,
            blueprintSelectionBoxes,
            inventoryRewards.Count > 0 ? Guid.NewGuid() : null,
            inventoryRewards);
    }

    private async Task<TournamentInstance?> OpenDevelopmentRegistrationAsync(
        Guid tournamentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(
            item => item.Id == tournamentId,
            cancellationToken);
        if (tournament is null
            || tournament.Status is not TournamentStatus.Scheduled and not TournamentStatus.RegistrationOpen)
        {
            return null;
        }

        var now = UtcNow();
        tournament.Status = TournamentStatus.RegistrationOpen;
        tournament.RegistrationStartsAtUtc = now.AddMinutes(-1);
        tournament.RegistrationEndsAtUtc = now.AddHours(1);
        tournament.StartsAtUtc = now.AddHours(1);
        tournament.UpdatedAtUtc = now;

        await SaveCommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            "DevelopmentRegistrationOpened",
            now,
            cancellationToken);
        return tournament;
    }

    private async Task<bool> CloseDevelopmentRegistrationAsync(
        Guid tournamentId,
        int registeredParticipantCount,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);

        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(
            item => item.Id == tournamentId,
            cancellationToken);
        if (tournament is null || tournament.Status != TournamentStatus.RegistrationOpen)
        {
            return false;
        }

        var now = UtcNow();
        tournament.RegisteredParticipantCount = registeredParticipantCount;
        tournament.RegistrationEndsAtUtc = now;
        tournament.StartsAtUtc = now;
        tournament.UpdatedAtUtc = now;

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task AdvanceTournamentAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;

        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);
        await _tournamentLockService.LockTournamentAsync(tournamentId, cancellationToken);
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null)
        {
            return;
        }

        var now = UtcNow();
        var changed = true;
        var changedAny = false;
        var stopProgression = false;
        var progressionSteps = 0;
        while (changed && !stopProgression)
        {
            progressionSteps++;
            if (progressionSteps > 100)
            {
                var roundStates = await _tournaments.Rounds
                    .Where(r => r.TournamentId == tournamentId)
                    .OrderBy(r => r.RoundNumber)
                    .Select(r => $"{r.RoundNumber}:{r.Status}")
                    .ToListAsync(cancellationToken);
                var matchStates = await _tournaments.Matches
                    .Where(m => m.TournamentId == tournamentId)
                    .OrderBy(m => m.RoundNumber)
                    .ThenBy(m => m.MatchNumber)
                    .Select(m => $"{m.RoundNumber}.{m.MatchNumber}:{m.Status}")
                    .ToListAsync(cancellationToken);

                throw new InvalidOperationException(
                    $"Tournament progression exceeded 100 state changes for tournament {tournamentId}. " +
                    $"Status={tournament.Status}; Rounds={string.Join(",", roundStates)}; Matches={string.Join(",", matchStates)}.");
            }

            changed = false;
            switch (tournament.Status)
            {
                case TournamentStatus.Scheduled when tournament.RegistrationStartsAtUtc <= now:
                    tournament.Status = TournamentStatus.RegistrationOpen;
                    changed = Touch(tournament, now);
                    break;
                case TournamentStatus.RegistrationOpen when tournament.RegistrationEndsAtUtc <= now:
                    tournament.Status = TournamentStatus.RegistrationClosed;
                    changed = Touch(tournament, now);
                    break;
                case TournamentStatus.RegistrationClosed:
                    await GenerateBracketAsync(tournament, now, cancellationToken);
                    if (tournament.Status is not TournamentStatus.Completed and not TournamentStatus.Cancelled)
                    {
                        tournament.Status = TournamentStatus.BracketGenerated;
                    }
                    changed = Touch(tournament, now);
                    if (tournament.Status == TournamentStatus.BracketGenerated)
                    {
                        await PublishTournamentEventAsync(tournament, "TournamentBracketGenerated", now, cancellationToken);
                    }
                    break;
                case TournamentStatus.BracketGenerated when tournament.StartsAtUtc <= now:
                    await AutoClaimOutstandingRewardsAsync(
                        tournament.Id,
                        now,
                        cancellationToken);
                    tournament.Status = TournamentStatus.InProgress;
                    changed = Touch(tournament, now);
                    await EnqueueTournamentChatAnnouncementAsync(
                        tournament,
                        "Tournament Grounds has started! Enter the Colosseum to follow the action.",
                        "started",
                        now,
                        cancellationToken);
                    break;
                case TournamentStatus.BracketGenerated:
                    changed = await PrepareUpcomingMatchPlaybacksAsync(
                        tournament,
                        now,
                        cancellationToken);
                    stopProgression = true;
                    break;
                case TournamentStatus.InProgress:
                    var progression = await ResolveDueRoundsAsync(tournament, now, cancellationToken);
                    changed = progression.Changed;
                    stopProgression = progression.StopProgression;
                    break;
            }

            if (changed)
            {
                changedAny = true;
                await _tournaments.SaveChangesAsync(cancellationToken);
            }
        }

        if (changedAny && _outbox is not null)
        {
            await EnqueueTournamentEventAsync(
                tournament,
                "TournamentStateChanged",
                now,
                cancellationToken);
        }

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (changedAny && _outbox is null)
        {
            await PublishTournamentEventAsync(
                tournament,
                "TournamentStateChanged",
                now,
                cancellationToken);
        }
    }

    private async Task AutoClaimOutstandingRewardsAsync(
        Guid startingTournamentId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var pendingRewardOwners = await _tournaments.RewardGrants
            .Where(reward =>
                reward.TournamentId != startingTournamentId
                && reward.Status == TournamentRewardStatus.Unclaimed)
            .Select(reward => new
            {
                reward.CharacterId,
                reward.TournamentId
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var rewardOwner in pendingRewardOwners)
        {
            await ClaimUnclaimedRewardsAsync(
                rewardOwner.CharacterId,
                rewardOwner.TournamentId,
                now,
                cancellationToken);
        }
    }

    private async Task GenerateBracketAsync(TournamentInstance tournament, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await _tournaments.Rounds.AnyAsync(r => r.TournamentId == tournament.Id, cancellationToken))
        {
            return;
        }

        var ineligibleParticipants = await _tournaments.Participants
            .Where(p => p.TournamentId == tournament.Id &&
                        p.Status != TournamentParticipantStatus.Withdrawn &&
                        !EligibleParticipants(now).Any(eligible => eligible.Id == p.Id))
            .ToListAsync(cancellationToken);
        foreach (var participant in ineligibleParticipants)
        {
            participant.Status = TournamentParticipantStatus.Withdrawn;
            participant.UpdatedAtUtc = now;
        }

        await PrepareTeamsForBracketAsync(tournament.Id, now, cancellationToken);

        var teams = await _tournaments.Teams
            .Where(t => t.TournamentId == tournament.Id && t.Status == TournamentTeamStatus.Forming && t.MemberCount > 0)
            .OrderByDescending(t =>
                _tournaments.Participants
                    .Where(p => p.TeamId == t.Id && p.Status != TournamentParticipantStatus.Withdrawn)
                    .Average(p => (double?)p.EntryArenaRating) ?? 0)
            .ThenBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (teams.Count < tournament.MinParticipants)
        {
            tournament.Status = TournamentStatus.Cancelled;
            tournament.CancelledAtUtc = now;
            tournament.CancellationReason = "Minimum team count was not met.";
            tournament.UpdatedAtUtc = now;
            await _tournaments.SaveChangesAsync(cancellationToken);
            return;
        }

        if (teams.Count > tournament.MaxParticipants)
        {
            tournament.Status = TournamentStatus.Cancelled;
            tournament.CancelledAtUtc = now;
            tournament.CancellationReason = "Maximum team count was exceeded.";
            tournament.UpdatedAtUtc = now;
            await _tournaments.SaveChangesAsync(cancellationToken);
            return;
        }

        for (var i = 0; i < teams.Count; i++)
        {
            teams[i].Seed = i + 1;
            teams[i].Status = TournamentTeamStatus.Active;
            teams[i].UpdatedAtUtc = now;
        }

        var teamIds = teams.Select(t => t.Id).ToList();
        var participants = await _tournaments.Participants
            .Where(p => p.TeamId.HasValue && teamIds.Contains(p.TeamId.Value) && p.Status == TournamentParticipantStatus.Registered)
            .ToListAsync(cancellationToken);
        var seedByTeam = teams.ToDictionary(t => t.Id, t => t.Seed);
        foreach (var participant in participants)
        {
            participant.Seed = participant.TeamId.HasValue ? seedByTeam.GetValueOrDefault(participant.TeamId.Value) : null;
            participant.Status = TournamentParticipantStatus.Active;
            participant.UpdatedAtUtc = now;
        }

        if (teams.Count == 1)
        {
            await CompleteTournamentAsync(tournament, teams[0].Id, now, cancellationToken);
            await _tournaments.SaveChangesAsync(cancellationToken);
            return;
        }

        var bracketSize = TournamentRules.GetBracketSize(teams.Count);
        var roundCount = (int)Math.Log2(bracketSize);
        var rounds = new List<TournamentRound>();
        for (var roundNumber = 1; roundNumber <= roundCount; roundNumber++)
        {
            var round = new TournamentRound
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                RoundNumber = roundNumber,
                Name = TournamentRules.GetRoundName(roundNumber, roundCount),
                Status = TournamentRoundStatus.Pending,
                StartsAtUtc = tournament.StartsAtUtc.AddMinutes((roundNumber - 1) * tournament.RoundIntervalMinutes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            rounds.Add(round);
            await _tournaments.AddAsync(round, cancellationToken);
        }

        var firstRoundMatchCount = bracketSize / 2;
        var byeCount = TournamentRules.GetByeCount(teams.Count);
        var active = teams.Skip(byeCount).ToList();

        for (var matchNumber = 1; matchNumber <= firstRoundMatchCount; matchNumber++)
        {
            TournamentTeam? playerOne;
            TournamentTeam? playerTwo;

            if (matchNumber <= byeCount)
            {
                playerOne = teams[matchNumber - 1];
                playerTwo = null;
            }
            else
            {
                var pairIndex = matchNumber - byeCount - 1;
                playerOne = active[pairIndex];
                playerTwo = active[active.Count - pairIndex - 1];
            }

            var match = CreateMatch(tournament.Id, rounds[0].Id, 1, matchNumber, playerOne?.Id, playerTwo?.Id, now);
            if (playerTwo is null && playerOne is not null)
            {
                CompleteBye(match, playerOne.Id, now);
            }

            await _tournaments.AddAsync(match, cancellationToken);
        }

        for (var roundNumber = 2; roundNumber <= roundCount; roundNumber++)
        {
            var matchCount = bracketSize / (int)Math.Pow(2, roundNumber);
            for (var matchNumber = 1; matchNumber <= matchCount; matchNumber++)
            {
                await _tournaments.AddAsync(CreateMatch(tournament.Id, rounds[roundNumber - 1].Id, roundNumber, matchNumber, null, null, now), cancellationToken);
            }
        }

        await _tournaments.SaveChangesAsync(cancellationToken);
        await AdvanceCompletedFirstRoundByesAsync(tournament.Id, now, cancellationToken);
        await ScheduleTournamentMatchesAsync(tournament.Id, tournament.StartsAtUtc, cancellationToken);
        await _tournaments.SaveChangesAsync(cancellationToken);
    }

    private async Task<TournamentProgressionResult> ResolveDueRoundsAsync(
        TournamentInstance tournament,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rounds = await _tournaments.Rounds
            .Where(r => r.TournamentId == tournament.Id)
            .OrderBy(r => r.RoundNumber)
            .ToListAsync(cancellationToken);
        var changed = await EnsureRemainingMatchScheduleAsync(
            tournament.Id,
            rounds,
            now,
            cancellationToken);
        var playbackFinalizationCutoff = now.AddSeconds(
            -_options.PlaybackCompletionGraceSeconds);

        foreach (var round in rounds)
        {
            var matches = await _tournaments.Matches
                .Where(m => m.TournamentId == tournament.Id && m.RoundNumber == round.RoundNumber)
                .OrderBy(m => m.MatchNumber)
                .ToListAsync(cancellationToken);

            changed = await PrepareUpcomingMatchPlaybacksAsync(
                tournament,
                round,
                rounds.Count,
                matches,
                now,
                cancellationToken) || changed;

            var finalizedMatch = false;
            foreach (var playingMatch in matches.Where(match =>
                         match.Status == TournamentMatchStatus.Resolving
                         && match.PlaybackEndsAtUtc <= playbackFinalizationCutoff))
            {
                await FinalizeMatchAsync(tournament, playingMatch, now, cancellationToken);
                finalizedMatch = true;
                changed = true;
            }

            if (finalizedMatch)
                StartNextSemifinalAsSoonAsAvailable(round, rounds.Count, matches, now);

            if (matches.All(match => match.Status is TournamentMatchStatus.Completed or TournamentMatchStatus.Bye))
            {
                if (round.Status != TournamentRoundStatus.Completed)
                {
                    round.Status = TournamentRoundStatus.Completed;
                    round.ResolvedAtUtc = now;
                    round.UpdatedAtUtc = now;
                    await ScheduleNextRoundAfterCooldownAsync(
                        tournament.Id,
                        round.RoundNumber,
                        rounds,
                        now,
                        cancellationToken);
                    changed = true;
                    await PublishTournamentEventAsync(tournament, "TournamentRoundResolved", now, cancellationToken);
                }

                var isFinalRound = round.RoundNumber == rounds.Count;
                if (isFinalRound
                    && matches.Count == 1
                    && matches[0].WinnerParticipantId is { } finalWinnerId)
                {
                    await CompleteTournamentAsync(
                        tournament,
                        finalWinnerId,
                        now,
                        cancellationToken);
                }

                continue;
            }

            if (round.StartsAtUtc > now)
                return new TournamentProgressionResult(changed, false);

            if (round.Status != TournamentRoundStatus.Resolving)
            {
                round.Status = TournamentRoundStatus.Resolving;
                round.UpdatedAtUtc = now;
                changed = true;
                await EnqueueTournamentChatAnnouncementAsync(
                    tournament,
                    $"Tournament Grounds: {round.Name} has started!",
                    $"round:{round.RoundNumber}",
                    now,
                    cancellationToken);
            }

            var nextDueAt = matches
                .Where(match =>
                    match.Status == TournamentMatchStatus.Ready
                    && match.ScheduledAtUtc.HasValue
                    && match.ScheduledAtUtc.Value <= now)
                .Select(match => match.ScheduledAtUtc)
                .Min();
            if (!nextDueAt.HasValue)
                return new TournamentProgressionResult(changed, false);

            var dueMatches = matches
                .Where(match =>
                    match.Status == TournamentMatchStatus.Ready
                    && match.ScheduledAtUtc == nextDueAt)
                .ToList();
            foreach (var dueMatch in dueMatches)
            {
                await StartMatchPlaybackAsync(tournament, dueMatch, now, cancellationToken);
            }

            return new TournamentProgressionResult(true, true);
        }

        return new TournamentProgressionResult(changed, false);
    }

    private async Task<bool> PrepareUpcomingMatchPlaybacksAsync(
        TournamentInstance tournament,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rounds = await _tournaments.Rounds
            .Where(round => round.TournamentId == tournament.Id)
            .OrderBy(round => round.RoundNumber)
            .ToListAsync(cancellationToken);
        var round = rounds.FirstOrDefault(candidate =>
            candidate.Status != TournamentRoundStatus.Completed);
        if (round is null) return false;

        var matches = await _tournaments.Matches
            .Where(match => match.TournamentId == tournament.Id
                            && match.RoundNumber == round.RoundNumber)
            .OrderBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);
        return await PrepareUpcomingMatchPlaybacksAsync(
            tournament,
            round,
            rounds.Count,
            matches,
            now,
            cancellationToken);
    }

    private async Task<bool> PrepareUpcomingMatchPlaybacksAsync(
        TournamentInstance tournament,
        TournamentRound round,
        int roundCount,
        IReadOnlyList<TournamentMatch> matches,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var preparationCutoff = now.AddSeconds(
            Math.Max(0, _options.MatchPreparationLeadSeconds));
        var prepareAllSemifinals = round.RoundNumber == roundCount - 1
            && matches.Any(match => match.Status == TournamentMatchStatus.Ready
                                    && match.ScheduledAtUtc <= preparationCutoff);
        var preparedMatchIds = (await _tournaments.CombatReplays
                .Where(replay => replay.TournamentId == tournament.Id)
                .Select(replay => replay.MatchId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var candidates = matches
            .Where(match => match.Status == TournamentMatchStatus.Ready
                            && match.PlayerOneParticipantId.HasValue
                            && match.PlayerTwoParticipantId.HasValue
                            && match.ScheduledAtUtc.HasValue
                            && !preparedMatchIds.Contains(match.Id)
                            && (match.ScheduledAtUtc.Value <= preparationCutoff
                                || prepareAllSemifinals))
            .OrderBy(match => match.MatchNumber)
            .ToList();

        var changed = false;
        foreach (var match in candidates)
        {
            var replay = await PrepareMatchPlaybackAsync(
                tournament,
                match,
                match.ScheduledAtUtc!.Value,
                now,
                cancellationToken);
            changed |= replay is not null;
        }

        if (changed)
            await _tournaments.SaveChangesAsync(cancellationToken);

        return changed;
    }

    private async Task<TournamentCombatReplay?> PrepareMatchPlaybackAsync(
        TournamentInstance tournament,
        TournamentMatch match,
        DateTimeOffset simulationStartsAt,
        DateTimeOffset preparedAt,
        CancellationToken cancellationToken)
    {
        var existing = await _tournaments.CombatReplays
            .FirstOrDefaultAsync(replay => replay.MatchId == match.Id, cancellationToken);
        if (existing is not null) return existing;
        if (!match.PlayerOneParticipantId.HasValue || !match.PlayerTwoParticipantId.HasValue)
            return null;

        var playerOne = await LoadTeamAsync(match.PlayerOneParticipantId.Value, cancellationToken);
        var playerTwo = await LoadTeamAsync(match.PlayerTwoParticipantId.Value, cancellationToken);
        if (playerOne is null || playerTwo is null) return null;

        var result = await ExecuteTournamentCombatAsync(
            tournament.Id,
            match.Id,
            playerOne,
            playerTwo,
            simulationStartsAt,
            cancellationToken);
        return await SaveTournamentCombatPlaybackAsync(
            match,
            playerOne,
            playerTwo,
            result.BattleId,
            result.Execution,
            simulationStartsAt,
            preparedAt,
            cancellationToken);
    }

    private async Task StartMatchPlaybackAsync(
        TournamentInstance tournament,
        TournamentMatch match,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ShiftDelayedMatchScheduleAsync(match, now, cancellationToken);
        var replay = await PrepareMatchPlaybackAsync(
            tournament,
            match,
            now,
            now,
            cancellationToken);
        if (replay is null) return;

        if (!string.IsNullOrWhiteSpace(replay.CombatResultJson)
            && JsonSerializer.Deserialize<CombatResult>(replay.CombatResultJson, ReplayJsonOptions) is { } combatResult)
        {
            combatResult.StartedAt = now;
            replay.CombatResultJson = JsonSerializer.Serialize(combatResult, ReplayJsonOptions);
        }

        replay.StartedAtUtc = now;
        match.Status = TournamentMatchStatus.Resolving;
        match.CombatSessionId = replay.CombatSessionId;
        match.PlaybackStartedAtUtc = now;
        match.PlaybackEndsAtUtc = now.AddSeconds(
            replay.Duration / (double)Services.LL.Combat.Engine.FastCombatEngine.TicksPerSecond);
        match.UpdatedAtUtc = now;
        await PublishTournamentEventAsync(tournament, "TournamentMatchStarted", now, cancellationToken);
    }

    private async Task ShiftDelayedMatchScheduleAsync(
        TournamentMatch startingMatch,
        DateTimeOffset actualStart,
        CancellationToken cancellationToken)
    {
        if (!startingMatch.ScheduledAtUtc.HasValue || startingMatch.ScheduledAtUtc.Value >= actualStart)
            return;

        var delay = actualStart - startingMatch.ScheduledAtUtc.Value;
        var remaining = await _tournaments.Matches
            .Where(match => match.TournamentId == startingMatch.TournamentId
                            && match.Status != TournamentMatchStatus.Completed
                            && match.Status != TournamentMatchStatus.Bye
                            && match.ScheduledAtUtc >= startingMatch.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
        foreach (var match in remaining)
            match.ScheduledAtUtc = match.ScheduledAtUtc!.Value.Add(delay);

        var roundNumbers = remaining.Select(match => match.RoundNumber).Distinct().ToArray();
        var rounds = await _tournaments.Rounds
            .Where(round => round.TournamentId == startingMatch.TournamentId
                            && roundNumbers.Contains(round.RoundNumber))
            .ToListAsync(cancellationToken);
        foreach (var round in rounds)
        {
            round.StartsAtUtc = remaining
                .Where(match => match.RoundNumber == round.RoundNumber)
                .Min(match => match.ScheduledAtUtc!.Value);
            round.UpdatedAtUtc = actualStart;
        }
    }

    private async Task FinalizeMatchAsync(
        TournamentInstance tournament,
        TournamentMatch match,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!match.PlayerOneParticipantId.HasValue || !match.PlayerTwoParticipantId.HasValue) return;
        var p1 = await LoadTeamAsync(match.PlayerOneParticipantId.Value, cancellationToken);
        var p2 = await LoadTeamAsync(match.PlayerTwoParticipantId.Value, cancellationToken);
        if (p1 is null || p2 is null) return;

        var replay = await _tournaments.CombatReplays
            .SingleAsync(item => item.MatchId == match.Id, cancellationToken);
        var combatResult = string.IsNullOrWhiteSpace(replay.CombatResultJson)
            ? new CombatResult
            {
                Outcome = Enum.Parse<BattleOutcome>(replay.Outcome),
                StartedAt = replay.StartedAtUtc,
                Duration = replay.Duration
            }
            : JsonSerializer.Deserialize<CombatResult>(replay.CombatResultJson, ReplayJsonOptions)
              ?? throw new InvalidOperationException("The stored Tournament combat result is invalid.");
        await EnqueueTournamentBattleEventsAsync(
            tournament.Id,
            match.Id,
            p1.Id,
            p2.Id,
            cancellationToken);
        var (p1Wins, matchOutcome) = ResolveTournamentMatchOutcome(
            combatResult,
            p1.Seed,
            p2.Seed);

        var winner = p1Wins ? p1 : p2;
        var loser = p1Wins ? p2 : p1;

        match.WinnerParticipantId = winner.Id;
        match.LoserParticipantId = loser.Id;
        match.Outcome = matchOutcome;
        match.Status = TournamentMatchStatus.Completed;
        match.ResolvedAtUtc = now;
        match.UpdatedAtUtc = now;

        match.BattleHistoryId = await SaveTournamentBattleHistoryAsync(
            match,
            p1,
            p2,
            winner,
            combatResult.Outcome,
            combatResult,
            now,
            cancellationToken);

        loser.Status = TournamentTeamStatus.Eliminated;
        loser.EliminatedInRoundNumber = match.RoundNumber;
        loser.FinalPlacement = TournamentRules.CalculatePlacement(await GetRoundCountAsync(tournament.Id, cancellationToken), match.RoundNumber);
        loser.UpdatedAtUtc = now;
        var loserMembers = await _tournaments.Participants
            .Where(p => p.TeamId == loser.Id && p.Status == TournamentParticipantStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var member in loserMembers)
        {
            member.Status = TournamentParticipantStatus.Eliminated;
            member.EliminatedInRoundNumber = match.RoundNumber;
            member.FinalPlacement = loser.FinalPlacement;
            member.UpdatedAtUtc = now;
        }

        await AdvanceWinnerAsync(tournament.Id, match, winner.Id, now, cancellationToken);
        await PublishTournamentEventAsync(tournament, "TournamentMatchCompleted", now, cancellationToken);
    }

    private static (bool PlayerOneWins, TournamentMatchOutcome MatchOutcome) ResolveTournamentMatchOutcome(
        CombatResult combatResult,
        int? playerOneSeed,
        int? playerTwoSeed)
    {
        if (combatResult.Outcome == BattleOutcome.Victory)
            return (true, TournamentMatchOutcome.PlayerOneWin);
        if (combatResult.Outcome == BattleOutcome.Defeat)
            return (false, TournamentMatchOutcome.PlayerTwoWin);

        var playerOneDamage = SumTeamDamage(combatResult, "Friendly");
        var playerTwoDamage = SumTeamDamage(combatResult, "Hostile");
        if (playerOneDamage != playerTwoDamage)
        {
            return (
                playerOneDamage > playerTwoDamage,
                TournamentMatchOutcome.DrawAdvancedByDamage);
        }

        return (
            (playerOneSeed ?? int.MaxValue) <= (playerTwoSeed ?? int.MaxValue),
            TournamentMatchOutcome.DrawAdvancedBySeed);
    }

    private static int SumTeamDamage(CombatResult combatResult, string team) =>
        combatResult.EntityStats
            .Where(stats => string.Equals(stats.Team, team, StringComparison.OrdinalIgnoreCase))
            .Sum(stats => stats.DamageDone);

    private readonly record struct TournamentProgressionResult(bool Changed, bool StopProgression);

    private Task EnqueueTournamentChatAnnouncementAsync(
        TournamentInstance tournament,
        string body,
        string announcementKey,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken)
    {
        if (_outbox is null)
        {
            return Task.CompletedTask;
        }

        return _outbox.EnqueueAsync(
            GameEventTypes.TournamentChatAnnouncement,
            new TournamentChatAnnouncementPayload(
                tournament.Id,
                CreateTournamentAnnouncementMessageId(tournament.Id, announcementKey),
                body,
                TournamentGroundsTargetUrl,
                sentAt),
            characterId: null,
            accountId: null,
            cancellationToken: cancellationToken);
    }

    private static Guid CreateTournamentAnnouncementMessageId(
        Guid tournamentId,
        string announcementKey)
    {
        var hash = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"tournament-grounds:{tournamentId:N}:{announcementKey}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private async Task EnqueueTournamentBattleEventsAsync(
        Guid tournamentId,
        Guid matchId,
        Guid playerOneTeamId,
        Guid playerTwoTeamId,
        CancellationToken cancellationToken)
    {
        if (_outbox is null)
        {
            return;
        }

        var participants = (await GetTeamMembersAsync(playerOneTeamId, cancellationToken))
            .Concat(await GetTeamMembersAsync(playerTwoTeamId, cancellationToken))
            .Select(participant => participant.CharacterId)
            .Distinct()
            .ToList();
        foreach (var characterId in participants)
        {
            await _outbox.EnqueueAsync(
                GameEventTypes.TournamentBattleCompleted,
                new TournamentBattleCompletedPayload(characterId, tournamentId, matchId),
                characterId,
                null,
                cancellationToken);
        }
    }

    private async Task<Guid> SaveTournamentBattleHistoryAsync(
        TournamentMatch match,
        TournamentTeam playerOne,
        TournamentTeam playerTwo,
        TournamentTeam winner,
        BattleOutcome outcome,
        CombatResult combatResult,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var historyId = match.Id;
        var existing = await _tournaments.FindAsync<ColosseumMatchResult>([historyId], cancellationToken);
        if (existing is not null)
        {
            await SaveTournamentCombatReplayAsync(
                match,
                playerOne,
                playerTwo,
                outcome,
                combatResult,
                existing.Id,
                now,
                cancellationToken);

            return existing.Id;
        }

        var playerOneRepresentative = await GetTeamRepresentativeAsync(playerOne.Id, cancellationToken);
        var playerTwoRepresentative = await GetTeamRepresentativeAsync(playerTwo.Id, cancellationToken);
        if (playerOneRepresentative is null || playerTwoRepresentative is null) return historyId;

        var playerOneName = playerOne.Name;
        var playerTwoName = playerTwo.Name;
        var winnerName = winner.Id == playerOne.Id ? playerOneName : playerTwoName;

        await _tournaments.AddAsync(new ColosseumMatchResult
        {
            Id = historyId,
            CharacterAId = playerOneRepresentative.CharacterId,
            CharacterAName = playerOneName,
            CharacterARatingBefore = playerOneRepresentative.EntryArenaRating,
            CharacterARatingAfter = playerOneRepresentative.EntryArenaRating,
            CharacterARatingDelta = 0,
            CharacterAGloryEarned = 0,
            CharacterAStreakBefore = 0,
            CharacterAStreakAfter = 0,

            CharacterBId = playerTwoRepresentative.CharacterId,
            CharacterBName = playerTwoName,
            CharacterBRatingBefore = playerTwoRepresentative.EntryArenaRating,
            CharacterBRatingAfter = playerTwoRepresentative.EntryArenaRating,
            CharacterBRatingDelta = 0,
            CharacterBGloryEarned = 0,

            WinnerId = winner.Id == playerOne.Id ? playerOneRepresentative.CharacterId : playerTwoRepresentative.CharacterId,
            WinnerName = winnerName,
            Outcome = ToTournamentHistoryOutcome(outcome, winner.Id == playerOne.Id),
            PlayedAt = now
        }, cancellationToken);

        await SaveTournamentCombatReplayAsync(
            match,
            playerOne,
            playerTwo,
            outcome,
            combatResult,
            historyId,
            now,
            cancellationToken);

        return historyId;
    }

    private async Task SaveTournamentCombatReplayAsync(
        TournamentMatch match,
        TournamentTeam playerOne,
        TournamentTeam playerTwo,
        BattleOutcome outcome,
        CombatResult combatResult,
        Guid historyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _tournaments.CombatReplays
            .AnyAsync(r => r.MatchId == match.Id, cancellationToken);
        if (existing) return;

        var playerOneRepresentative = await GetTeamRepresentativeAsync(playerOne.Id, cancellationToken);
        var playerTwoRepresentative = await GetTeamRepresentativeAsync(playerTwo.Id, cancellationToken);
        if (playerOneRepresentative is null || playerTwoRepresentative is null) return;

        await _tournaments.AddAsync(new TournamentCombatReplay
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            MatchId = match.Id,
            CombatSessionId = match.CombatSessionId ?? historyId,
            BattleHistoryId = historyId,
            PlayerOneCharacterId = playerOneRepresentative.CharacterId,
            PlayerTwoCharacterId = playerTwoRepresentative.CharacterId,
            Outcome = outcome.ToString(),
            StartedAtUtc = combatResult.StartedAt,
            Duration = combatResult.Duration,
            CombatResultJson = JsonSerializer.Serialize(combatResult, ReplayJsonOptions),
            CreatedAtUtc = now
        }, cancellationToken);
    }

    private async Task<TournamentCombatReplay> SaveTournamentCombatPlaybackAsync(
        TournamentMatch match,
        TournamentTeam playerOne,
        TournamentTeam playerTwo,
        Guid combatSessionId,
        CombatExecutionWithCheckpoints execution,
        DateTimeOffset playbackStartsAt,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var existing = await _tournaments.CombatReplays
            .FirstOrDefaultAsync(replay => replay.MatchId == match.Id, cancellationToken);
        if (existing is not null) return existing;

        var playerOneRepresentative = await GetTeamRepresentativeAsync(playerOne.Id, cancellationToken);
        var playerTwoRepresentative = await GetTeamRepresentativeAsync(playerTwo.Id, cancellationToken);
        if (playerOneRepresentative is null || playerTwoRepresentative is null)
            throw new InvalidOperationException("Tournament combat teams have no representative.");

        var bundle = CreateTournamentPlaybackBundle(execution);
        var uncompressedBytes = JsonSerializer.SerializeToUtf8Bytes(bundle, ReplayJsonOptions);
        if (uncompressedBytes.Length > _options.MaximumBundleUncompressedBytes)
            throw new InvalidOperationException(
                $"Tournament playback exceeded the {_options.MaximumBundleUncompressedBytes} byte uncompressed limit.");
        var compressedBytes = CompressPlaybackBundle(uncompressedBytes);
        if (compressedBytes.Length > _options.MaximumBundleCompressedBytes)
            throw new InvalidOperationException(
                $"Tournament playback exceeded the {_options.MaximumBundleCompressedBytes} byte compressed limit.");

        var hash = Convert.ToHexString(SHA256.HashData(compressedBytes)).ToLowerInvariant();
        PlaybackBundleBytes.Record(compressedBytes.Length);
        var replay = new TournamentCombatReplay
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            MatchId = match.Id,
            CombatSessionId = combatSessionId,
            BattleHistoryId = match.Id,
            PlayerOneCharacterId = playerOneRepresentative.CharacterId,
            PlayerTwoCharacterId = playerTwoRepresentative.CharacterId,
            Outcome = execution.Result.Outcome.ToString(),
            StartedAtUtc = playbackStartsAt,
            Duration = execution.Result.Duration,
            CombatResultJson = JsonSerializer.Serialize(execution.Result, ReplayJsonOptions),
            SchemaVersion = TournamentCombatReplay.CompactBundleSchemaVersion,
            TicksPerSecond = Services.LL.Combat.Engine.FastCombatEngine.TicksPerSecond,
            TicksPerFrame = _options.CombatTicksPerFrame,
            FrameCount = bundle.Frames.Count,
            BundleHash = hash,
            BundleLength = compressedBytes.Length,
            BundleContentType = "application/json",
            BundleContentEncoding = "br",
            CreatedAtUtc = createdAt
        };
        replay.Artifact = new TournamentCombatReplayArtifact
        {
            TournamentCombatReplayId = replay.Id,
            Replay = replay,
            BundleBytes = compressedBytes
        };
        await _tournaments.AddAsync(replay, cancellationToken);
        return replay;
    }

    private TournamentPlaybackBundleDto CreateTournamentPlaybackBundle(
        CombatExecutionWithCheckpoints execution)
    {
        var entityById = new Dictionary<string, TournamentPlaybackEntityDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var checkpoint in execution.Checkpoints)
        {
            AddEntities(checkpoint.Friendly, true);
            AddEntities(checkpoint.Hostile, false);
        }

        var entities = entityById.Values.OrderBy(entity => entity.Index).ToArray();
        var abilityKeys = execution.Checkpoints
            .SelectMany(checkpoint => checkpoint.EntityStats)
            .Where(entity => entityById.ContainsKey(entity.EntityId))
            .SelectMany(entity => entity.Abilities.Select(ability =>
                (EntityIndex: entityById[entity.EntityId].Index, ability.Name)))
            .Distinct()
            .OrderBy(key => key.EntityIndex)
            .ThenBy(key => key.Name, StringComparer.Ordinal)
            .ToArray();
        var abilities = abilityKeys
            .Select((key, index) => new TournamentPlaybackAbilityDto(index, key.EntityIndex, key.Name))
            .ToArray();
        var abilityIndex = abilities.ToDictionary(
            ability => (ability.EntityIndex, ability.Name),
            ability => ability.Index);

        var materializedStates = new Dictionary<int, TournamentPlaybackEntityStateDto>();
        var materializedTotals = new Dictionary<int, TournamentPlaybackEntityTotalsDto>();
        var materializedAbilityTotals = new Dictionary<int, TournamentPlaybackAbilityTotalsDto>();
        var frames = new TournamentPlaybackFrameDto[execution.Checkpoints.Count];
        var lastKeyframeTick = int.MinValue;
        for (var checkpointIndex = 0; checkpointIndex < execution.Checkpoints.Count; checkpointIndex++)
        {
            var checkpoint = execution.Checkpoints[checkpointIndex];
            var currentStates = checkpoint.Friendly
                .Concat(checkpoint.Hostile)
                .Select(entity => new TournamentPlaybackEntityStateDto(
                    entityById[entity.Id].Index,
                    entity.Health,
                    entity.Barrier))
                .OrderBy(state => state.EntityIndex)
                .ToArray();
            var currentTotals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .Select(entity => new TournamentPlaybackEntityTotalsDto(
                    entityById[entity.EntityId].Index,
                    entity.DamageDone,
                    entity.DamageTaken,
                    entity.HealingDone,
                    entity.HealingReceived,
                    entity.HealthRegenerated,
                    entity.BarrierGenerated,
                    entity.DamageBlocked,
                    entity.ThreatGenerated))
                .OrderBy(total => total.EntityIndex)
                .ToArray();
            var currentAbilityTotals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .SelectMany(entity => entity.Abilities.Select(ability =>
                    new TournamentPlaybackAbilityTotalsDto(
                        abilityIndex[(entityById[entity.EntityId].Index, ability.Name)],
                        ability.Uses,
                        ability.TotalDamage,
                        ability.TotalHealing,
                        ability.TotalBarrier,
                        ability.TotalThreat,
                        ability.DamageByType?.ToArray())))
                .OrderBy(total => total.AbilityIndex)
                .ToArray();

            var isKeyframe = checkpointIndex == 0
                || checkpoint.IsFinal
                || checkpoint.Tick - lastKeyframeTick >= PlaybackKeyframeIntervalTicks;
            var states = ApplyChanges(
                currentStates,
                materializedStates,
                state => state.EntityIndex,
                isKeyframe);
            var totals = ApplyChanges(
                currentTotals,
                materializedTotals,
                total => total.EntityIndex,
                isKeyframe);
            var abilityTotals = ApplyChanges(
                currentAbilityTotals,
                materializedAbilityTotals,
                total => total.AbilityIndex,
                isKeyframe,
                AbilityTotalsEqual);
            if (isKeyframe)
                lastKeyframeTick = checkpoint.Tick;

            frames[checkpointIndex] = new TournamentPlaybackFrameDto(
                checkpoint.Sequence,
                checkpoint.Tick,
                isKeyframe,
                states,
                totals,
                abilityTotals,
                checkpoint.IsFinal,
                checkpoint.IsFinal ? execution.Result.Outcome : null);
        }

        return new TournamentPlaybackBundleDto(
            TournamentCombatReplay.CompactBundleSchemaVersion,
            Services.LL.Combat.Engine.FastCombatEngine.TicksPerSecond,
            _options.CombatTicksPerFrame,
            execution.Result.Duration,
            entities,
            abilities,
            frames);

        static IReadOnlyList<T> ApplyChanges<T>(
            IEnumerable<T> current,
            IDictionary<int, T> materialized,
            Func<T, int> getIndex,
            bool isKeyframe,
            Func<T, T, bool>? equals = null)
        {
            equals ??= EqualityComparer<T>.Default.Equals;
            var changed = new List<T>();
            foreach (var value in current)
            {
                var index = getIndex(value);
                if (!materialized.TryGetValue(index, out var previous) || !equals(previous, value))
                    changed.Add(value);
                materialized[index] = value;
            }

            return isKeyframe
                ? materialized.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray()
                : changed;
        }

        static bool AbilityTotalsEqual(
            TournamentPlaybackAbilityTotalsDto left,
            TournamentPlaybackAbilityTotalsDto right) =>
            left.AbilityIndex == right.AbilityIndex
            && left.Uses == right.Uses
            && left.TotalDamage == right.TotalDamage
            && left.TotalHealing == right.TotalHealing
            && left.TotalBarrier == right.TotalBarrier
            && left.TotalThreat == right.TotalThreat
            && DamageByTypeEqual(left.DamageByType, right.DamageByType);

        static bool DamageByTypeEqual(
            IReadOnlyList<AbilityDamageTypeStats>? left,
            IReadOnlyList<AbilityDamageTypeStats>? right) =>
            ReferenceEquals(left, right)
            || left is not null && right is not null && left.SequenceEqual(right);

        void AddEntities(IEnumerable<SimpleCombatEntity> source, bool friendly)
        {
            foreach (var entity in source)
            {
                if (entityById.ContainsKey(entity.Id)) continue;
                entityById[entity.Id] = new TournamentPlaybackEntityDto(
                    entityById.Count,
                    entity.Id,
                    entity.Name,
                    entity.ImagePath,
                    friendly,
                    entity.MaxHealth,
                    entity.Level);
            }
        }
    }

    private static byte[] CompressPlaybackBundle(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            brotli.Write(bytes);
        return output.ToArray();
    }

    private async Task<(Guid BattleId, CombatExecutionWithCheckpoints Execution)> ExecuteTournamentCombatAsync(
        Guid tournamentId,
        Guid matchId,
        TournamentTeam playerOne,
        TournamentTeam playerTwo,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var playerOneMembers = await GetTeamMembersAsync(playerOne.Id, cancellationToken);
        var playerTwoMembers = await GetTeamMembersAsync(playerTwo.Id, cancellationToken);
        var allMembers = playerOneMembers.Concat(playerTwoMembers).ToArray();
        var characterIds = allMembers.Select(participant => participant.CharacterId).ToList();
        var entities = await _entityService.GetEntitiesByIdsForCombatAsync(characterIds, cancellationToken);
        if (entities.Count < characterIds.Count)
        {
            var fallback = CreateFallbackCombatResult(BattleOutcome.Draw, now);
            return (Guid.NewGuid(), CreateFallbackExecution(fallback));
        }

        var sourceById = entities.Cast<Character>().ToDictionary(e => e.Id);
        var snapshotIds = allMembers.Select(participant => participant.SnapshotId).ToArray();
        var snapshotQuery = _tournaments.CombatSnapshots
            .Where(snapshot => snapshotIds.Contains(snapshot.Id))
            .Include(snapshot => snapshot.CharacterSnapshot);
        var snapshotRows = await snapshotQuery
            .ThenInclude(snapshot => snapshot.BaseAttributes)
            .ToListAsync(cancellationToken);
        await snapshotQuery
            .Include(snapshot => snapshot.CharacterSnapshot)
                .ThenInclude(snapshot => snapshot.Equipment)
                    .ThenInclude(equipment => equipment.InstanceModifiers)
            .LoadAsync(cancellationToken);
        await snapshotQuery
            .Include(snapshot => snapshot.CharacterSnapshot)
                .ThenInclude(snapshot => snapshot.EquippedEssences)
            .LoadAsync(cancellationToken);
        var snapshots = snapshotRows.ToDictionary(
            snapshot => snapshot.Id,
            snapshot => snapshot.CharacterSnapshot);
        if (snapshots.Count < snapshotIds.Length)
        {
            var fallback = CreateFallbackCombatResult(BattleOutcome.Draw, now);
            return (Guid.NewGuid(), CreateFallbackExecution(fallback));
        }

        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync(
            snapshots.Values
                .SelectMany(snapshot => snapshot.Equipment)
                .Select(equipment => equipment.ItemBaseId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            cancellationToken);
        var friendlyRuntime = new List<CombatRuntimeParticipant>();
        var hostileRuntime = new List<CombatRuntimeParticipant>();
        var slots = new List<CombatParticipantSlot>();
        var combatEntities = new List<CombatEntity>();

        foreach (var participant in playerOneMembers)
        {
            var snapshot = snapshots[participant.SnapshotId];
            var source = sourceById[participant.CharacterId];
            var combat = CreateSnapshotCombatEntity(source, snapshot, itemBases);
            combatEntities.Add(combat);
            var slot = new CombatParticipantSlot(participant.CharacterId.ToString(), participant.CharacterId, CombatSide.Friendly);
            slots.Add(slot);
            friendlyRuntime.Add(new CombatRuntimeParticipant(slot, source, combat));
        }

        foreach (var participant in playerTwoMembers)
        {
            var snapshot = snapshots[participant.SnapshotId];
            var source = sourceById[participant.CharacterId];
            var combat = CreateSnapshotCombatEntity(source, snapshot, itemBases);
            combatEntities.Add(combat);
            var slot = new CombatParticipantSlot(participant.CharacterId.ToString(), participant.CharacterId, CombatSide.Hostile);
            slots.Add(slot);
            hostileRuntime.Add(new CombatRuntimeParticipant(slot, source, combat));
        }

        await _combatSetupService.PrepareEntitiesForCombat(combatEntities, EssenceCombatActivity.Tournament);

        var battleId = Guid.NewGuid();
        var encounterPlan = new CombatEncounterPlan(
            EncounterId: battleId,
            Mode: CombatMode.Pvp,
            Sequence: 1,
            StartsAt: now,
            Participants: slots,
            SourceContext: new PvpEncounterSourceContext(
                matchId,
                playerOneMembers[0].CharacterId,
                playerTwoMembers[0].CharacterId));

        var runtime = new CombatEncounterRuntime(
            encounterPlan,
            friendlyRuntime,
            hostileRuntime);

        var engineStartedAt = _timeProvider.GetTimestamp();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
        var execution = await _combatEngineExecutor.ExecuteTournamentPlaybackAsync(
            runtime,
            _options.CombatTicksPerFrame,
            new TournamentCombatSimulationOptions(
                GetCombatDurationTicks(
                    _options.RegulationDurationMinutes,
                    Services.LL.Combat.Engine.FastCombatEngine.TicksPerSecond),
                GetCombatDurationTicks(
                    _options.OvertimeDurationMinutes,
                    Services.LL.Combat.Engine.FastCombatEngine.TicksPerSecond),
                checked(
                    _options.OvertimePowerIncreaseIntervalSeconds
                    * Services.LL.Combat.Engine.FastCombatEngine.TicksPerSecond),
                _options.OvertimePowerIncreasePercent),
            cancellationToken);
        var elapsedMilliseconds = _timeProvider.GetElapsedTime(engineStartedAt).TotalMilliseconds;
        var allocatedBytes = Math.Max(
            0,
            GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore);
        CombatDurationMilliseconds.Record(elapsedMilliseconds);
        CombatAllocatedBytes.Record(allocatedBytes);
        var resolved = _combatEncounterResultFactory.Create(runtime, execution.Result).CombatResult;
        execution = execution with { Result = resolved };
        return (battleId, execution);
    }

    private static int GetCombatDurationTicks(int minutes, int ticksPerSecond) =>
        checked(minutes * 60 * ticksPerSecond);

    private static CombatExecutionWithCheckpoints CreateFallbackExecution(CombatResult result) =>
        new(result,
        [
            new CombatCheckpoint(0, 0, [], [], [], [], false),
            new CombatCheckpoint(1, result.Duration, [], [], [], [], true)
        ]);

    private static CombatResult CreateFallbackCombatResult(BattleOutcome outcome, DateTimeOffset now)
    {
        return new CombatResult
        {
            Outcome = outcome,
            StartedAt = now,
            Duration = 1
        };
    }

    private CombatEntity CreateSnapshotCombatEntity(
        Character sourceCharacter,
        CharacterSnapshot snapshot,
        IReadOnlyDictionary<string, ItemBase> itemBases)
    {
        var template = _combatSetupService.CreatePlayerCombatEntities([sourceCharacter]).Single();
        template.Name = snapshot.Name;
        template.Level = snapshot.Level;
        template.BaseAttributes = snapshot.BaseAttributes
            .Select(x => new Domain.Models.Attributes.EntityAttribute
            {
                EntityId = snapshot.CharacterId,
                AttributeType = x.AttributeType,
                Value = x.Value
            })
            .ToList();
        template.EquippedEssences = snapshot.EquippedEssences
            .OrderBy(x => x.SlotIndex)
            .Select(x => x.ToPlayerEssence(snapshot.CharacterId))
            .ToList();
        template.HasEquippedEssenceSnapshot = true;

        template.Equipment = snapshot.Equipment
            .OrderBy(x => x.Slot)
            .Where(x => itemBases.ContainsKey(x.ItemBaseId))
            .Select(x => new EquipmentInstance
            {
                Id = x.EquipmentInstanceId,
                ItemBaseId = x.ItemBaseId,
                ItemBase = itemBases[x.ItemBaseId],
                BaseRecipeId = x.BaseRecipeId,
                Rarity = x.Rarity,
                Quality = x.Quality,
                Tier = x.Tier,
                StatModelVersion = x.StatModelVersion,
                Potential = x.Potential,
                ItemXp = x.ItemXp,
                IsMasterpiece = x.IsMasterpiece,
                IsLevelingItem = x.IsLevelingItem,
                InstanceModifiers = x.InstanceModifiers
                    .Select(modifier => modifier.ToInstanceModifier(x.EquipmentInstanceId))
                    .ToList()
            })
            .ToList();

        return template;
    }

    private async Task CompleteTournamentAsync(TournamentInstance tournament, Guid championTeamId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var champion = await _tournaments.Teams.FirstAsync(t => t.Id == championTeamId, cancellationToken);
        champion.Status = TournamentTeamStatus.Champion;
        champion.FinalPlacement = 1;
        champion.UpdatedAtUtc = now;

        var championMembers = await _tournaments.Participants
            .Where(p => p.TeamId == championTeamId && p.Status != TournamentParticipantStatus.Withdrawn)
            .ToListAsync(cancellationToken);
        foreach (var member in championMembers)
        {
            member.Status = TournamentParticipantStatus.Champion;
            member.FinalPlacement = 1;
            member.UpdatedAtUtc = now;
        }

        tournament.Status = TournamentStatus.Completed;
        tournament.CompletedAtUtc = now;
        tournament.UpdatedAtUtc = now;

        var participants = await EligibleParticipants(now)
            .Where(p => p.TournamentId == tournament.Id && p.Status != TournamentParticipantStatus.Withdrawn)
            .ToListAsync(cancellationToken);

        foreach (var participant in participants)
        {
            var placement = participant.FinalPlacement ?? 99;
            var reward = BuildReward(tournament.Id, participant.CharacterId, placement, now);
            var exists = await _tournaments.RewardGrants
                .AnyAsync(r => r.TournamentId == reward.TournamentId && r.CharacterId == reward.CharacterId && r.RewardKey == reward.RewardKey, cancellationToken);
            if (!exists)
            {
                await _tournaments.AddAsync(reward, cancellationToken);
            }

            if (_achievementService is not null)
            {
                await _achievementService.RecordColosseumTournamentAsync(
                    participant.CharacterId,
                    participant.TeamId == championTeamId,
                    cancellationToken);
            }
        }

        await PublishTournamentEventAsync(tournament, "TournamentCompleted", now, cancellationToken);
    }

    private TournamentRewardGrant BuildReward(Guid tournamentId, Guid characterId, int placement, DateTimeOffset now)
    {
        var tier = GetConfiguredRewardTiers()
            .Where(t => t.MaxPlacement is null || placement <= t.MaxPlacement.Value)
            .OrderBy(t => t.MaxPlacement ?? int.MaxValue)
            .First();

        return new TournamentRewardGrant
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            CharacterId = characterId,
            RewardKey = tier.Key,
            Placement = placement == 99 ? null : placement,
            ArenaGlory = tier.ArenaGlory,
            Cinders = tier.Cinders,
            Soulstones = tier.Soulstones,
            CatalystSelectionCaches = tier.CatalystSelectionCaches,
            BlueprintSelectionBoxes = tier.BlueprintSelectionBoxes,
            SigilFragments = tier.SigilFragments,
            Status = TournamentRewardStatus.Unclaimed,
            CreatedAtUtc = now
        };
    }

    private async Task AdvanceCompletedFirstRoundByesAsync(Guid tournamentId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var byes = await _tournaments.Matches
            .Where(m => m.TournamentId == tournamentId && m.RoundNumber == 1 && m.Status == TournamentMatchStatus.Bye && m.WinnerParticipantId.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var bye in byes)
        {
            await AdvanceWinnerAsync(tournamentId, bye, bye.WinnerParticipantId!.Value, now, cancellationToken);
        }
    }

    private async Task<List<Domain.Models.Inventories.InventoryItem>> CreateRewardInventoryItemsAsync(
        Guid characterId,
        int catalystSelectionCaches,
        int blueprintSelectionBoxes,
        CancellationToken cancellationToken)
    {
        var quantities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (catalystSelectionCaches > 0)
        {
            quantities[CatalystSelectionCrateCatalog.ItemBaseId] = catalystSelectionCaches;
        }

        if (blueprintSelectionBoxes > 0)
        {
            quantities[BlueprintSelectionBoxCatalog.ItemBaseId] = blueprintSelectionBoxes;
        }

        if (quantities.Count == 0)
        {
            return [];
        }

        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync(
            quantities.Keys.ToList(),
            cancellationToken);
        var missing = quantities.Keys.Where(itemId => !itemBases.ContainsKey(itemId)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Tournament reward item bases are missing: {string.Join(", ", missing)}.");
        }

        return quantities
            .SelectMany(reward => _inventoryItemFactory.CreateForQuantity(
                itemBases[reward.Key],
                reward.Value,
                characterId))
            .ToList();
    }

    private static ClaimTournamentRewardsResult EmptyClaimResult() =>
        new(false, 0, 0, 0, 0, 0, 0, null, []);

    private async Task ScheduleTournamentMatchesAsync(
        Guid tournamentId,
        DateTimeOffset firstMatchAt,
        CancellationToken cancellationToken)
    {
        var rounds = await _tournaments.Rounds
            .Where(round => round.TournamentId == tournamentId)
            .OrderBy(round => round.RoundNumber)
            .ToListAsync(cancellationToken);
        var matches = await _tournaments.Matches
            .Where(match => match.TournamentId == tournamentId)
            .OrderBy(match => match.RoundNumber)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.MatchIntervalMinutes));
        var schedule = BuildTournamentMatchSchedule(rounds, matches, firstMatchAt, interval);
        foreach (var match in matches)
        {
            if (match.Status == TournamentMatchStatus.Bye)
            {
                match.ScheduledAtUtc = null;
                continue;
            }

            match.ScheduledAtUtc = schedule[match.Id];
        }

        foreach (var round in rounds)
        {
            var firstScheduledMatch = matches.FirstOrDefault(match =>
                match.RoundNumber == round.RoundNumber && match.ScheduledAtUtc.HasValue);
            if (firstScheduledMatch?.ScheduledAtUtc is { } startsAt)
                round.StartsAtUtc = startsAt;
        }
    }

    private async Task<bool> EnsureRemainingMatchScheduleAsync(
        Guid tournamentId,
        IReadOnlyList<TournamentRound> rounds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var matches = await _tournaments.Matches
            .Where(match => match.TournamentId == tournamentId)
            .OrderBy(match => match.RoundNumber)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);
        var unscheduled = matches.Where(match =>
                (match.Status is TournamentMatchStatus.Pending or TournamentMatchStatus.Ready)
                && !match.ScheduledAtUtc.HasValue)
            .ToList();
        if (unscheduled.Count == 0) return false;

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.MatchIntervalMinutes));
        var firstMatchAt = rounds
            .OrderBy(round => round.RoundNumber)
            .Select(round => round.StartsAtUtc)
            .FirstOrDefault();
        if (firstMatchAt == default) firstMatchAt = now;
        var schedule = BuildTournamentMatchSchedule(rounds, matches, firstMatchAt, interval);
        var earliestMissingAt = unscheduled.Min(match => schedule[match.Id]);
        var recoveryDelay = earliestMissingAt < now
            ? now - earliestMissingAt
            : TimeSpan.Zero;
        foreach (var match in unscheduled)
        {
            match.ScheduledAtUtc = schedule[match.Id].Add(recoveryDelay);
        }

        foreach (var round in rounds.Where(round => round.Status != TournamentRoundStatus.Completed))
        {
            var firstScheduledAt = matches
                .Where(match => match.RoundNumber == round.RoundNumber
                                && match.Status is not TournamentMatchStatus.Completed
                                and not TournamentMatchStatus.Bye
                                && match.ScheduledAtUtc.HasValue)
                .Select(match => match.ScheduledAtUtc!.Value)
                .DefaultIfEmpty(round.StartsAtUtc)
                .Min();
            round.StartsAtUtc = firstScheduledAt;
            round.UpdatedAtUtc = now;
        }

        return true;
    }

    private async Task ScheduleNextRoundAfterCooldownAsync(
        Guid tournamentId,
        int completedRoundNumber,
        IReadOnlyList<TournamentRound> rounds,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        var nextRound = rounds.FirstOrDefault(round =>
            round.RoundNumber == completedRoundNumber + 1);
        if (nextRound is null || nextRound.Status == TournamentRoundStatus.Completed)
            return;

        var startsAt = completedAt.AddSeconds(
            Math.Max(0, _options.RoundCompletionCooldownSeconds));
        var nextMatches = await _tournaments.Matches
            .Where(match =>
                match.TournamentId == tournamentId
                && match.RoundNumber == nextRound.RoundNumber
                && (match.Status == TournamentMatchStatus.Pending
                    || match.Status == TournamentMatchStatus.Ready))
            .ToListAsync(cancellationToken);
        var previousRoundStart = nextMatches
            .Where(match => match.ScheduledAtUtc.HasValue)
            .Select(match => match.ScheduledAtUtc!.Value)
            .DefaultIfEmpty(startsAt)
            .Min();

        foreach (var match in nextMatches)
        {
            var withinRoundOffset = match.ScheduledAtUtc.HasValue
                ? match.ScheduledAtUtc.Value - previousRoundStart
                : TimeSpan.Zero;
            match.ScheduledAtUtc = startsAt.Add(
                withinRoundOffset < TimeSpan.Zero ? TimeSpan.Zero : withinRoundOffset);
            match.UpdatedAtUtc = completedAt;
        }

        nextRound.StartsAtUtc = startsAt;
        nextRound.UpdatedAtUtc = completedAt;
    }

    private static void StartNextSemifinalAsSoonAsAvailable(
        TournamentRound round,
        int roundCount,
        IReadOnlyList<TournamentMatch> matches,
        DateTimeOffset now)
    {
        if (round.RoundNumber != roundCount - 1
            || matches.Any(match => match.Status == TournamentMatchStatus.Resolving))
            return;

        var nextMatch = matches
            .Where(match => match.Status == TournamentMatchStatus.Ready
                            && match.ScheduledAtUtc > now)
            .OrderBy(match => match.MatchNumber)
            .FirstOrDefault();
        if (nextMatch is null) return;

        nextMatch.ScheduledAtUtc = now;
        nextMatch.UpdatedAtUtc = now;
    }

    private static IReadOnlyDictionary<Guid, DateTimeOffset> BuildTournamentMatchSchedule(
        IReadOnlyList<TournamentRound> rounds,
        IReadOnlyList<TournamentMatch> matches,
        DateTimeOffset firstMatchAt,
        TimeSpan interval)
    {
        var schedule = new Dictionary<Guid, DateTimeOffset>();
        var cursor = firstMatchAt;
        var finalRoundNumber = rounds.Count == 0
            ? 0
            : rounds.Max(round => round.RoundNumber);
        var semiFinalRoundNumber = finalRoundNumber - 1;

        foreach (var round in rounds.OrderBy(round => round.RoundNumber))
        {
            var roundMatches = matches
                .Where(match =>
                    match.RoundNumber == round.RoundNumber
                    && match.Status != TournamentMatchStatus.Bye)
                .OrderBy(match => match.MatchNumber)
                .ToList();
            if (roundMatches.Count == 0) continue;

            if (round.RoundNumber == semiFinalRoundNumber)
            {
                foreach (var match in roundMatches)
                {
                    schedule[match.Id] = cursor;
                    cursor = cursor.Add(interval);
                }

                continue;
            }

            foreach (var match in roundMatches)
            {
                schedule[match.Id] = cursor;
            }

            if (round.RoundNumber < finalRoundNumber)
            {
                cursor = cursor.Add(interval);
            }
        }

        return schedule;
    }

    private async Task AdvanceWinnerAsync(Guid tournamentId, TournamentMatch match, Guid winnerParticipantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var nextRound = match.RoundNumber + 1;
        var nextMatchNumber = (match.MatchNumber + 1) / 2;
        var nextMatch = await _tournaments.Matches
            .FirstOrDefaultAsync(m => m.TournamentId == tournamentId && m.RoundNumber == nextRound && m.MatchNumber == nextMatchNumber, cancellationToken);
        if (nextMatch is null) return;

        if (match.MatchNumber % 2 == 1)
        {
            nextMatch.PlayerOneParticipantId ??= winnerParticipantId;
        }
        else
        {
            nextMatch.PlayerTwoParticipantId ??= winnerParticipantId;
        }

        if (nextMatch.PlayerOneParticipantId.HasValue && nextMatch.PlayerTwoParticipantId.HasValue)
        {
            nextMatch.Status = TournamentMatchStatus.Ready;
        }

        nextMatch.UpdatedAtUtc = now;
    }

    private static TournamentMatch CreateMatch(
        Guid tournamentId,
        Guid roundId,
        int roundNumber,
        int matchNumber,
        Guid? playerOneParticipantId,
        Guid? playerTwoParticipantId,
        DateTimeOffset now)
    {
        return new TournamentMatch
        {
            Id = Guid.NewGuid(),
            TournamentId = tournamentId,
            RoundId = roundId,
            RoundNumber = roundNumber,
            MatchNumber = matchNumber,
            PlayerOneParticipantId = playerOneParticipantId,
            PlayerTwoParticipantId = playerTwoParticipantId,
            Status = playerOneParticipantId.HasValue && playerTwoParticipantId.HasValue ? TournamentMatchStatus.Ready : TournamentMatchStatus.Pending,
            Outcome = TournamentMatchOutcome.None,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static void CompleteBye(TournamentMatch match, Guid winnerParticipantId, DateTimeOffset now)
    {
        match.WinnerParticipantId = winnerParticipantId;
        match.Status = TournamentMatchStatus.Bye;
        match.Outcome = TournamentMatchOutcome.ByeAdvanced;
        match.ResolvedAtUtc = now;
        match.UpdatedAtUtc = now;
    }

    private async Task PrepareTeamsForBracketAsync(Guid tournamentId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_options.DevelopmentToolsEnabled)
        {
            await CreateDevelopmentTeamsForUnassignedParticipantsAsync(
                tournamentId,
                now,
                cancellationToken);
        }

        await RecalculateTeamMemberCountsAsync(tournamentId, now, cancellationToken);

        while (true)
        {
            var teams = await _tournaments.Teams
                .Where(t => t.TournamentId == tournamentId &&
                    t.Status == TournamentTeamStatus.Forming &&
                    t.MemberCount > 0 &&
                    t.MemberCount < 3)
                .OrderByDescending(t => t.MemberCount)
                .ThenBy(t => t.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            TournamentTeam? target = null;
            TournamentTeam? source = null;
            var bestCombinedCount = 0;

            for (var i = 0; i < teams.Count; i++)
            {
                for (var j = i + 1; j < teams.Count; j++)
                {
                    var combinedCount = teams[i].MemberCount + teams[j].MemberCount;
                    if (combinedCount > 3 || combinedCount <= bestCombinedCount)
                    {
                        continue;
                    }

                    bestCombinedCount = combinedCount;
                    target = teams[i];
                    source = teams[j];
                }
            }

            if (target is null || source is null)
            {
                break;
            }

            await MergeTeamsAsync(target, source, now, cancellationToken);
            await _tournaments.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task CreateDevelopmentTeamsForUnassignedParticipantsAsync(
        Guid tournamentId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var participants = await _tournaments.Participants
            .Where(p => p.TournamentId == tournamentId && p.Status != TournamentParticipantStatus.Withdrawn)
            .OrderByDescending(p => p.EntryArenaRating)
            .ThenBy(p => p.RegisteredAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var participant in participants.Where(p => !p.TeamId.HasValue))
        {
            var team = new TournamentTeam
            {
                Id = Guid.NewGuid(),
                TournamentId = tournamentId,
                Name = NormalizeTeamName("", participant.CharacterId),
                OwnerParticipantId = participant.Id,
                Status = TournamentTeamStatus.Forming,
                MemberCount = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            participant.TeamId = team.Id;
            participant.Team = team;
            participant.IsTeamOwner = true;
            participant.UpdatedAtUtc = now;
            await _tournaments.AddAsync(team, cancellationToken);
        }

        await _tournaments.SaveChangesAsync(cancellationToken);
    }

    private async Task RecalculateTeamMemberCountsAsync(Guid tournamentId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var teams = await _tournaments.Teams
            .Where(t => t.TournamentId == tournamentId && t.Status != TournamentTeamStatus.Disbanded)
            .ToListAsync(cancellationToken);

        foreach (var team in teams)
        {
            team.MemberCount = await _tournaments.Participants
                .CountAsync(p => p.TeamId == team.Id && p.Status != TournamentParticipantStatus.Withdrawn, cancellationToken);
            team.UpdatedAtUtc = now;
        }
    }

    private async Task MergeTeamsAsync(
        TournamentTeam target,
        TournamentTeam source,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sourceMembers = await _tournaments.Participants
            .Where(p => p.TeamId == source.Id && p.Status != TournamentParticipantStatus.Withdrawn)
            .ToListAsync(cancellationToken);

        foreach (var member in sourceMembers)
        {
            member.TeamId = target.Id;
            member.IsTeamOwner = false;
            member.UpdatedAtUtc = now;
            await CancelPendingRequestsForParticipantAsync(target.TournamentId, member.Id, now, cancellationToken);
        }

        var sourceRequests = await _tournaments.TeamApplications
            .Where(a => a.TeamId == source.Id && a.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var request in sourceRequests)
        {
            request.Status = TournamentTeamRequestStatus.Cancelled;
            request.UpdatedAtUtc = now;
        }

        var sourceInvites = await _tournaments.TeamInvites
            .Where(i => i.TeamId == source.Id && i.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var invite in sourceInvites)
        {
            invite.Status = TournamentTeamRequestStatus.Cancelled;
            invite.UpdatedAtUtc = now;
        }

        target.MemberCount += sourceMembers.Count;
        target.UpdatedAtUtc = now;
        source.MemberCount = 0;
        source.Status = TournamentTeamStatus.Disbanded;
        source.UpdatedAtUtc = now;
    }

    private async Task<TournamentTeam?> LoadTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _tournaments.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
    }

    private async Task<IReadOnlyList<TournamentParticipant>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _tournaments.Participants
            .Where(p => p.TeamId == teamId && p.Status != TournamentParticipantStatus.Withdrawn)
            .OrderByDescending(p => p.EntryArenaRating)
            .ThenBy(p => p.RegisteredAtUtc)
            .Take(3)
            .ToListAsync(cancellationToken);
    }

    private async Task<TournamentParticipant?> GetTeamRepresentativeAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _tournaments.Participants
            .Where(p => p.TeamId == teamId && p.Status != TournamentParticipantStatus.Withdrawn)
            .OrderByDescending(p => p.IsTeamOwner)
            .ThenByDescending(p => p.EntryArenaRating)
            .ThenBy(p => p.RegisteredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> GetRoundCountAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        return await _tournaments.Rounds.CountAsync(r => r.TournamentId == tournamentId, cancellationToken);
    }

    private async Task<TournamentSummary> MapSummaryAsync(
        TournamentInstance tournament,
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var participant = await _tournaments.Participants
            .FirstOrDefaultAsync(p => p.TournamentId == tournament.Id && p.CharacterId == characterId, cancellationToken);
        var hasRewards = await _tournaments.RewardGrants
            .AnyAsync(r => r.TournamentId == tournament.Id && r.CharacterId == characterId && r.Status == TournamentRewardStatus.Unclaimed, cancellationToken);
        var reason = GetCannotRegisterReason(tournament, participant, now);
        var registeredTeamCount = await GetCurrentTeamCountAsync(tournament.Id, cancellationToken);

        return new TournamentSummary(
            tournament.Id,
            tournament.Name,
            tournament.Status.ToString(),
            tournament.RegistrationStartsAtUtc,
            tournament.RegistrationEndsAtUtc,
            tournament.StartsAtUtc,
            registeredTeamCount,
            tournament.MinParticipants,
            tournament.MaxParticipants,
            participant is { Status: not TournamentParticipantStatus.Withdrawn },
            reason is null,
            reason,
            participant?.Id,
            hasRewards,
            participant?.Status.ToString(),
            participant?.Seed,
            participant?.EntryArenaRating,
            participant?.FinalPlacement,
            tournament.CompletedAtUtc,
            tournament.CancelledAtUtc,
            tournament.CancellationReason);
    }

    private async Task<IReadOnlyList<TournamentParticipantEntry>> MapParticipantsAsync(Guid tournamentId, CancellationToken cancellationToken)
    {
        var participants = await _tournaments.Participants
            .Where(p => p.TournamentId == tournamentId)
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ToListAsync(cancellationToken);
        var characterIds = participants.Select(p => p.CharacterId).ToList();
        var names = await _tournaments.Characters
            .Where(c => characterIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return participants.Select(p => new TournamentParticipantEntry(
            p.Id,
            p.CharacterId,
            names.TryGetValue(p.CharacterId, out var name) ? name : "Unknown",
            p.TeamId,
            p.IsTeamOwner,
            p.Seed,
            p.EntryArenaRating,
            p.EntryRankTier,
            p.Status.ToString(),
            p.FinalPlacement)).ToList();
    }

    private async Task<IReadOnlyList<TournamentTeamEntry>> MapTeamsAsync(
        Guid tournamentId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var teams = await _tournaments.Teams
            .Where(t => t.TournamentId == tournamentId && t.Status != TournamentTeamStatus.Disbanded)
            .OrderBy(t => t.Seed ?? int.MaxValue)
            .ThenBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        if (teams.Count == 0) return [];

        var participants = await MapParticipantsAsync(tournamentId, cancellationToken);
        var participantById = participants.ToDictionary(p => p.ParticipantId);
        var participantsByTeam = participants
            .Where(p => p.TeamId.HasValue)
            .GroupBy(p => p.TeamId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TournamentParticipantEntry>)g.ToList());
        var playerParticipant = await GetRegisteredParticipantAsync(characterId, tournamentId, cancellationToken);

        var pendingApplications = await _tournaments.TeamApplications
            .Where(a => a.TournamentId == tournamentId && a.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        var pendingInvites = await _tournaments.TeamInvites
            .Where(i => i.TournamentId == tournamentId && i.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);

        return teams.Select(team =>
        {
            var members = participantsByTeam.GetValueOrDefault(team.Id, []);
            var owner = participantById.GetValueOrDefault(team.OwnerParticipantId);
            return new TournamentTeamEntry(
                team.Id,
                team.Name,
                team.Status.ToString(),
                team.OwnerParticipantId,
                owner?.CharacterName ?? "Unknown",
                members.Count,
                Math.Max(0, 3 - members.Count),
                team.Seed,
                team.FinalPlacement,
                members.Count < 3 && team.Status == TournamentTeamStatus.Forming,
                playerParticipant?.TeamId == team.Id,
                playerParticipant?.Id == team.OwnerParticipantId,
                members,
                pendingApplications
                    .Where(a => a.TeamId == team.Id)
                    .Select(a =>
                    {
                        var applicant = participantById.GetValueOrDefault(a.ApplicantParticipantId);
                        return new TournamentTeamApplicationEntry(
                            a.Id,
                            a.ApplicantParticipantId,
                            applicant?.CharacterId ?? Guid.Empty,
                            applicant?.CharacterName ?? "Unknown",
                            a.Status.ToString(),
                            a.CreatedAtUtc);
                    })
                    .ToList(),
                pendingInvites
                    .Where(i => i.TeamId == team.Id)
                    .Select(i =>
                    {
                        var invited = participantById.GetValueOrDefault(i.InvitedParticipantId);
                        return new TournamentTeamInviteEntry(
                            i.Id,
                            i.InvitedParticipantId,
                            invited?.CharacterId ?? Guid.Empty,
                            invited?.CharacterName ?? "Unknown",
                            i.Status.ToString(),
                            i.CreatedAtUtc);
                    })
                    .ToList());
        }).ToList();
    }

    private async Task<TournamentParticipant?> GetRegisteredParticipantAsync(
        Guid characterId,
        Guid tournamentId,
        CancellationToken cancellationToken)
        => await _tournaments.Participants.FirstOrDefaultAsync(p =>
            p.TournamentId == tournamentId &&
            p.CharacterId == characterId &&
            p.Status != TournamentParticipantStatus.Withdrawn,
            cancellationToken);

    private async Task<int> GetCurrentTeamCountAsync(Guid tournamentId, CancellationToken cancellationToken)
        => await _tournaments.Teams.CountAsync(t =>
            t.TournamentId == tournamentId &&
            t.Status != TournamentTeamStatus.Disbanded,
            cancellationToken);

    private async Task<bool> CanMutateTeamsAsync(
        Guid tournamentId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        => await _tournaments.Tournaments.AnyAsync(t =>
            t.Id == tournamentId &&
            t.Status == TournamentStatus.RegistrationOpen &&
            now < t.RegistrationEndsAtUtc,
            cancellationToken);

    private async Task<bool> IsTeamOwnerAsync(
        Guid characterId,
        TournamentTeam team,
        CancellationToken cancellationToken)
    {
        var participant = await GetRegisteredParticipantAsync(characterId, team.TournamentId, cancellationToken);
        return participant is not null && participant.Id == team.OwnerParticipantId;
    }

    private async Task CancelPendingRequestsForParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var applications = await _tournaments.TeamApplications
            .Where(a => a.TournamentId == tournamentId && a.ApplicantParticipantId == participantId && a.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var application in applications)
        {
            application.Status = TournamentTeamRequestStatus.Cancelled;
            application.UpdatedAtUtc = now;
        }

        var invites = await _tournaments.TeamInvites
            .Where(i => i.TournamentId == tournamentId && i.InvitedParticipantId == participantId && i.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var invite in invites)
        {
            invite.Status = TournamentTeamRequestStatus.Cancelled;
            invite.UpdatedAtUtc = now;
        }
    }

    private async Task CancelPendingRequestsForTeamAsync(
        Guid tournamentId,
        Guid teamId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var applications = await _tournaments.TeamApplications
            .Where(a => a.TournamentId == tournamentId && a.TeamId == teamId && a.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var application in applications)
        {
            application.Status = TournamentTeamRequestStatus.Cancelled;
            application.UpdatedAtUtc = now;
        }

        var invites = await _tournaments.TeamInvites
            .Where(i => i.TournamentId == tournamentId && i.TeamId == teamId && i.Status == TournamentTeamRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var invite in invites)
        {
            invite.Status = TournamentTeamRequestStatus.Cancelled;
            invite.UpdatedAtUtc = now;
        }
    }

    private async Task<int> GetActiveTeamMemberCountAsync(Guid teamId, CancellationToken cancellationToken)
        => await _tournaments.Participants.CountAsync(
            p => p.TeamId == teamId && p.Status != TournamentParticipantStatus.Withdrawn,
            cancellationToken);

    private static string NormalizeTeamName(string name, Guid fallbackId)
    {
        var normalized = string.IsNullOrWhiteSpace(name)
            ? $"Team {fallbackId.ToString("N")[..6]}"
            : name.Trim();

        return normalized.Length <= 80 ? normalized : normalized[..80];
    }

    private static string? GetCannotRegisterReason(TournamentInstance tournament, TournamentParticipant? participant, DateTimeOffset now)
    {
        if (participant is { Status: not TournamentParticipantStatus.Withdrawn }) return "Already registered";
        if (tournament.Status == TournamentStatus.Scheduled || now < tournament.RegistrationStartsAtUtc) return "Registration has not opened.";
        if (tournament.Status != TournamentStatus.RegistrationOpen || now >= tournament.RegistrationEndsAtUtc) return "Registration has closed.";
        if (tournament.RegisteredParticipantCount >= GetMaxRegisteredParticipants(tournament)) return "Tournament is full.";
        return null;
    }

    private static int GetMaxRegisteredParticipants(TournamentInstance tournament)
        => tournament.MaxParticipants * 3;

    private async Task<TournamentDefinition> EnsureDefaultDefinitionAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var definitionKey = string.IsNullOrWhiteSpace(_options.DefaultDefinitionKey)
            ? "weekly-open-grounds"
            : _options.DefaultDefinitionKey.Trim();
        var definition = await _tournaments.Definitions.FirstOrDefaultAsync(d => d.Key == definitionKey, cancellationToken);
        if (definition is not null)
        {
            if (definition.StartDelayAfterRegistrationMinutes != _options.DefaultStartDelayAfterRegistrationMinutes)
            {
                definition.StartDelayAfterRegistrationMinutes = _options.DefaultStartDelayAfterRegistrationMinutes;
                definition.UpdatedAtUtc = now;
                await _tournaments.SaveChangesAsync(cancellationToken);
            }

            return definition;
        }

        definition = new TournamentDefinition
        {
            Id = Guid.NewGuid(),
            Key = definitionKey,
            Name = string.IsNullOrWhiteSpace(_options.DefaultName) ? "Weekly Open Grounds" : _options.DefaultName.Trim(),
            Description = string.IsNullOrWhiteSpace(_options.DefaultDescription)
                ? "A weekly asynchronous single-elimination Colosseum bracket."
                : _options.DefaultDescription.Trim(),
            Format = TournamentFormat.SingleElimination,
            MinParticipants = _options.DefaultMinParticipants,
            MaxParticipants = _options.DefaultMaxParticipants,
            RegistrationDurationMinutes = GetDefaultRegistrationDurationMinutes(),
            StartDelayAfterRegistrationMinutes = _options.DefaultStartDelayAfterRegistrationMinutes,
            RoundIntervalMinutes = _options.DefaultRoundIntervalMinutes,
            MinimumCharacterLevel = _options.DefaultMinimumCharacterLevel,
            MinimumArenaRating = _options.DefaultMinimumArenaRating,
            MinimumRankTier = _options.DefaultMinimumRankTier,
            Enabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await _tournaments.AddAsync(definition, cancellationToken);
        await _tournaments.SaveChangesAsync(cancellationToken);
        return definition;
    }

    private async Task AlignUpcomingTournamentStartsAsync(
        TournamentDefinition definition,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tournaments = await _tournaments.Tournaments
            .Where(tournament => tournament.DefinitionId == definition.Id)
            .Where(tournament => tournament.Status == TournamentStatus.Scheduled
                                 || tournament.Status == TournamentStatus.RegistrationOpen
                                 || tournament.Status == TournamentStatus.RegistrationClosed
                                 || tournament.Status == TournamentStatus.BracketGenerated)
            .ToListAsync(cancellationToken);
        var changed = false;

        foreach (var tournament in tournaments)
        {
            var configuredStart = tournament.RegistrationEndsAtUtc
                .AddMinutes(definition.StartDelayAfterRegistrationMinutes);
            if (tournament.StartsAtUtc == configuredStart)
            {
                continue;
            }

            tournament.StartsAtUtc = configuredStart;
            tournament.UpdatedAtUtc = now;
            changed = true;

            if (tournament.Status == TournamentStatus.BracketGenerated)
            {
                await ScheduleTournamentMatchesAsync(
                    tournament.Id,
                    configuredStart,
                    cancellationToken);
            }
        }

        if (changed)
        {
            await _tournaments.SaveChangesAsync(cancellationToken);
        }
    }

    private TournamentRegistrationWindow BuildNextRegistrationWindow(DateTimeOffset now)
    {
        var weekStart = GetUtcWeekStart(now);
        var registrationStart = BuildWeeklyDateTime(
            weekStart,
            _options.DefaultRegistrationStartDayUtc,
            _options.DefaultRegistrationStartHourUtc);
        var registrationEnd = BuildWeeklyDateTime(
            weekStart,
            _options.DefaultRegistrationEndDayUtc,
            _options.DefaultRegistrationEndHourUtc);

        if (registrationEnd <= registrationStart)
        {
            registrationEnd = registrationEnd.AddDays(7);
        }

        if (now >= registrationEnd)
        {
            registrationStart = registrationStart.AddDays(7);
            registrationEnd = registrationEnd.AddDays(7);
        }

        return new TournamentRegistrationWindow(registrationStart, registrationEnd);
    }

    private DateTimeOffset UtcNow()
    {
        return _timeProvider.GetUtcNow();
    }

    private IQueryable<TournamentParticipant> EligibleParticipants(DateTimeOffset now) =>
        _tournaments.Participants.Where(participant =>
            !_tournaments.AccountRestrictions.Any(restriction =>
                restriction.AccountId == participant.AccountId &&
                restriction.RevokedAt == null &&
                (restriction.ExpiresAt == null || restriction.ExpiresAt > now) &&
                (restriction.RestrictionType == AccountRestrictionType.Ban ||
                 restriction.RestrictionType == AccountRestrictionType.MultiplayerRestriction)));

    private IReadOnlyList<TournamentRewardTierOptions> GetConfiguredRewardTiers()
    {
        var configured = _options.Rewards
            .Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .OrderBy(r => r.MaxPlacement ?? int.MaxValue)
            .ToList();

        return configured.Any(r => r.MaxPlacement is null) ? configured : DefaultRewardTiers;
    }

    private int GetDefaultRegistrationDurationMinutes()
    {
        var startMinutes = GetWeeklyMinuteOffset(
            _options.DefaultRegistrationStartDayUtc,
            _options.DefaultRegistrationStartHourUtc);
        var endMinutes = GetWeeklyMinuteOffset(
            _options.DefaultRegistrationEndDayUtc,
            _options.DefaultRegistrationEndHourUtc);

        if (endMinutes <= startMinutes)
        {
            endMinutes += 7 * 24 * 60;
        }

        return Math.Max(60, endMinutes - startMinutes);
    }

    private static int NormalizeUtcHour(int hour)
    {
        return Math.Clamp(hour, 0, 23);
    }

    private static DateTimeOffset GetUtcWeekStart(DateTimeOffset now)
    {
        var date = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    private static DateTimeOffset BuildWeeklyDateTime(DateTimeOffset weekStart, DayOfWeek day, int hour)
        => weekStart.AddDays(GetDayOffsetFromMonday(day)).AddHours(NormalizeUtcHour(hour));

    private static int GetWeeklyMinuteOffset(DayOfWeek day, int hour)
        => (GetDayOffsetFromMonday(day) * 24 * 60) + (NormalizeUtcHour(hour) * 60);

    private static int GetDayOffsetFromMonday(DayOfWeek day)
        => ((int)day - (int)DayOfWeek.Monday + 7) % 7;

    private static bool Touch(TournamentInstance tournament, DateTimeOffset now)
    {
        tournament.UpdatedAtUtc = now;
        return true;
    }

    private static bool MeetsEligibility(Character character, TournamentDefinition definition)
    {
        if (definition.MinimumCharacterLevel.HasValue && character.Level < definition.MinimumCharacterLevel.Value) return false;
        if (definition.MinimumArenaRating.HasValue && character.ArenaProfile.Rating < definition.MinimumArenaRating.Value) return false;
        if (!string.IsNullOrWhiteSpace(definition.MinimumRankTier))
        {
            var current = ArenaRank.GetTier(character.ArenaProfile.Rating);
            var required = ArenaRank.Tiers.FirstOrDefault(t => t.Id == definition.MinimumRankTier);
            if (required is null || current.SortOrder < required.SortOrder) return false;
        }

        return true;
    }

    private static string ToTournamentHistoryOutcome(BattleOutcome outcome, bool playerOneAdvanced)
    {
        if (outcome == BattleOutcome.Draw)
        {
            return playerOneAdvanced ? "TournamentDrawPlayerOneAdvanced" : "TournamentDrawPlayerTwoAdvanced";
        }

        return playerOneAdvanced ? "TournamentPlayerOneWin" : "TournamentPlayerTwoWin";
    }

    private static string BuildSnapshotJson(
        CharacterSnapshot snapshot,
        int arenaRating,
        string rankTier,
        DateTimeOffset createdAtUtc)
    {
        var payload = new TournamentSnapshotAuditPayload(
            snapshot.Id,
            snapshot.CharacterId,
            snapshot.Name,
            snapshot.Level,
            arenaRating,
            rankTier,
            createdAtUtc,
            snapshot.BaseAttributes
                .OrderBy(a => a.AttributeType)
                .Select(a => new TournamentSnapshotAttribute(a.AttributeType.ToString(), a.Value))
                .ToList(),
            snapshot.Equipment
                .OrderBy(e => e.Slot)
                .Select(e => new TournamentSnapshotEquipment(
                    e.Slot.ToString(),
                    e.EquipmentInstanceId,
                    e.ItemBaseId,
                    e.BaseRecipeId,
                    e.Rarity.ToString(),
                    e.Potential,
                    e.ItemXp,
                    e.IsMasterpiece,
                    e.IsLevelingItem,
                    e.InstanceModifiers
                        .OrderBy(m => m.AttributeType)
                        .Select(m => new TournamentSnapshotModifier(m.AttributeType.ToString(), m.Amount, m.ModifierType.ToString()))
                        .ToList()))
                .ToList(),
            snapshot.EquippedEssences
                .OrderBy(e => e.SlotIndex)
                .Select(e => new TournamentSnapshotEssence(
                    e.SlotIndex,
                    e.PlayerEssenceId,
                    e.EssenceDefinitionId,
                    e.Level,
                    e.CurrentXp,
                    e.AscensionTier,
                    e.IsEvolved))
                .ToList());

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private Task<ITournamentGroundsTransaction> BeginOwnedTransactionIfNeededAsync(CancellationToken cancellationToken)
        => _tournaments.BeginTransactionIfNeededAsync(cancellationToken);

    private async Task SaveCommitAndPublishTournamentEventAsync(
        ITournamentGroundsTransaction transaction,
        Guid tournamentId,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _tournaments.SaveChangesAsync(cancellationToken);
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(
            item => item.Id == tournamentId,
            cancellationToken);

        if (tournament is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await CommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            eventName,
            now,
            cancellationToken);
    }

    private async Task SaveCommitAndPublishTournamentEventAsync(
        ITournamentGroundsTransaction transaction,
        TournamentInstance tournament,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _tournaments.SaveChangesAsync(cancellationToken);
        await CommitAndPublishTournamentEventAsync(
            transaction,
            tournament,
            eventName,
            now,
            cancellationToken);
    }

    private async Task CommitAndPublishTournamentEventAsync(
        ITournamentGroundsTransaction transaction,
        TournamentInstance tournament,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_outbox is not null)
        {
            await EnqueueTournamentEventAsync(
                tournament,
                eventName,
                now,
                cancellationToken);
            await _tournaments.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        if (_outbox is null)
        {
            await PublishTournamentEventAsync(
                tournament,
                eventName,
                now,
                cancellationToken);
        }
    }

    private async Task PublishTournamentEventAsync(
        Guid tournamentId,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is not null)
        {
            await PublishTournamentEventAsync(tournament, eventName, now, cancellationToken);
        }
    }

    private async Task PublishTournamentEventAsync(
        TournamentInstance tournament,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_outbox is not null)
        {
            await EnqueueTournamentEventAsync(
                tournament,
                eventName,
                now,
                cancellationToken);
            await _tournaments.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var stateVersion = await AdvanceTournamentVersionAsync(eventName, cancellationToken);
            var update = await BuildTournamentUpdateAsync(
                tournament,
                stateVersion,
                eventName,
                now,
                cancellationToken);

            await _gameRealtime.PublishAsync(
                new Audience.World(),
                update,
                nameof(TournamentGroundsService),
                cancellationToken);
        }
        catch
        {
            // REST remains authoritative; realtime is only a convenience refresh signal.
        }
    }

    private async Task EnqueueTournamentEventAsync(
        TournamentInstance tournament,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var stateVersion = await AdvanceTournamentVersionAsync(eventName, cancellationToken);
        var update = await BuildTournamentUpdateAsync(
            tournament,
            stateVersion,
            eventName,
            now,
            cancellationToken);
        await _outbox!.EnqueueAsync(
            GameEventTypes.TournamentGroundsUpdated,
            update,
            characterId: null,
            accountId: null,
            cancellationToken);
    }

    private async Task<TournamentGroundsUpdated> BuildTournamentUpdateAsync(
        TournamentInstance tournament,
        long stateVersion,
        string eventName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var hasBracket = await _tournaments.Rounds
            .AnyAsync(r => r.TournamentId == tournament.Id, cancellationToken);
        var currentRound = await _tournaments.Rounds
            .Where(r => r.TournamentId == tournament.Id && r.Status != TournamentRoundStatus.Completed)
            .OrderBy(r => r.RoundNumber)
            .Select(r => new { r.RoundNumber, r.StartsAtUtc })
            .FirstOrDefaultAsync(cancellationToken);
        var activePlaybackEndsAt = await _tournaments.Matches
            .Where(match => match.TournamentId == tournament.Id
                            && match.Status == TournamentMatchStatus.Resolving)
            .Select(match => match.PlaybackEndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var nextMatchStartsAt = await _tournaments.Matches
            .Where(match => match.TournamentId == tournament.Id
                            && match.Status != TournamentMatchStatus.Completed
                            && match.Status != TournamentMatchStatus.Bye
                            && match.ScheduledAtUtc.HasValue)
            .OrderBy(match => match.ScheduledAtUtc)
            .Select(match => match.ScheduledAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var nextActionAt = tournament.Status switch
        {
            TournamentStatus.Scheduled => tournament.RegistrationStartsAtUtc,
            TournamentStatus.RegistrationOpen => tournament.RegistrationEndsAtUtc,
            TournamentStatus.RegistrationClosed or TournamentStatus.BracketGenerated => tournament.StartsAtUtc,
            TournamentStatus.InProgress => activePlaybackEndsAt ?? nextMatchStartsAt ?? currentRound?.StartsAtUtc,
            _ => null
        };

        return new TournamentGroundsUpdated(
            tournament.Id,
            stateVersion,
            tournament.TournamentNumber,
            tournament.Name,
            eventName,
            tournament.Status.ToString(),
            await GetCurrentTeamCountAsync(tournament.Id, cancellationToken),
            tournament.MinParticipants,
            tournament.MaxParticipants,
            hasBracket,
            currentRound?.RoundNumber,
            nextActionAt,
            tournament.CompletedAtUtc,
            tournament.CancelledAtUtc,
            now);
    }

    private Task<long> AdvanceTournamentVersionAsync(
        string eventName,
        CancellationToken cancellationToken) =>
        _stateSync.AdvanceWorldScopeWithRevisionAsync(
            Application.WebSockets.Contracts.StateSyncScopes.Tournament,
            eventName,
            cancellationToken);

    private sealed record TournamentRegistrationWindow(
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc);

    private sealed record TournamentSnapshotAuditPayload(
        Guid SnapshotId,
        Guid CharacterId,
        string Name,
        int Level,
        int ArenaRating,
        string RankTier,
        DateTimeOffset CreatedAtUtc,
        IReadOnlyList<TournamentSnapshotAttribute> BaseAttributes,
        IReadOnlyList<TournamentSnapshotEquipment> Equipment,
        IReadOnlyList<TournamentSnapshotEssence> EquippedEssences);

    private sealed record TournamentSnapshotAttribute(
        string AttributeType,
        float Value);

    private sealed record TournamentSnapshotEquipment(
        string Slot,
        Guid EquipmentInstanceId,
        string ItemBaseId,
        string? BaseRecipeId,
        string Rarity,
        int? Potential,
        int ItemXp,
        bool IsMasterpiece,
        bool IsLevelingItem,
        IReadOnlyList<TournamentSnapshotModifier> InstanceModifiers);

    private sealed record TournamentSnapshotModifier(
        string AttributeType,
        float Amount,
        string ModifierType);

    private sealed record TournamentSnapshotEssence(
        int SlotIndex,
        Guid PlayerEssenceId,
        string EssenceDefinitionId,
        int Level,
        int CurrentXp,
        int AscensionTier,
        bool IsEvolved);
}


