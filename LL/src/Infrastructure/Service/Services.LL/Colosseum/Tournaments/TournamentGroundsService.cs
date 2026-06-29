using Application.Interfaces.WebSockets;
using Application.Interfaces.Services.LL.Colosseum;
using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using System.Text.Json;
using System.Text.Json.Serialization;
using TournamentGroundsUpdated = Application.WebSockets.Contracts.TournamentGroundsUpdated;

namespace Services.LL.Colosseum.Tournaments;

public sealed class TournamentGroundsService : ITournamentGroundsService
{
    private static readonly IReadOnlyList<TournamentRewardTierOptions> DefaultRewardTiers =
    [
        new() { Key = "champion", MaxPlacement = 1, ArenaGlory = 120, Cinders = 600, Soulstones = 12 },
        new() { Key = "finalist", MaxPlacement = 2, ArenaGlory = 80, Cinders = 400, Soulstones = 8 },
        new() { Key = "semi-finalist", MaxPlacement = 4, ArenaGlory = 50, Cinders = 250, Soulstones = 5 },
        new() { Key = "quarter-finalist", MaxPlacement = 8, ArenaGlory = 35, Cinders = 175, Soulstones = 3 },
        new() { Key = "participant", MaxPlacement = null, ArenaGlory = 20, Cinders = 100, Soulstones = 2 }
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
    private readonly ICombatEngineExecutor _combatEngineExecutor;
    private readonly ICombatEncounterResultFactory _combatEncounterResultFactory;
    private readonly IGameRealtimeBroadcaster _gameRealtime;
    private readonly ITournamentLockService _tournamentLockService;
    private readonly TimeProvider _timeProvider;
    private readonly TournamentGroundsOptions _options;

    public TournamentGroundsService(
        ITournamentGroundsRepository tournaments,
        IEntityService entityService,
        ICombatSetupService combatSetupService,
        ICharacterSnapshotService characterSnapshotService,
        IItemBaseRepository itemBaseRepository,
        ICombatEngineExecutor combatEngineExecutor,
        ICombatEncounterResultFactory combatEncounterResultFactory,
        IGameRealtimeBroadcaster gameRealtime,
        ITournamentLockService tournamentLockService,
        TimeProvider timeProvider,
        IOptions<TournamentGroundsOptions> options)
    {
        _tournaments = tournaments;
        _entityService = entityService;
        _combatSetupService = combatSetupService;
        _characterSnapshotService = characterSnapshotService;
        _itemBaseRepository = itemBaseRepository;
        _combatEngineExecutor = combatEngineExecutor;
        _combatEncounterResultFactory = combatEncounterResultFactory;
        _gameRealtime = gameRealtime;
        _tournamentLockService = tournamentLockService;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task EnsureUpcomingTournamentsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;

        var now = UtcNow();
        var definition = await EnsureDefaultDefinitionAsync(now, cancellationToken);

        var hasUpcoming = await _tournaments.Tournaments.AnyAsync(t =>
            t.DefinitionId == definition.Id &&
            t.Status != TournamentStatus.Completed &&
            t.Status != TournamentStatus.Cancelled &&
            t.RegistrationEndsAtUtc >= now.AddDays(-7),
            cancellationToken);

        if (hasUpcoming) return;

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
    }

    public async Task AdvanceDueTournamentsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return;

        var now = UtcNow();
        var dueIds = await _tournaments.Tournaments
            .Where(t => t.Status != TournamentStatus.Completed && t.Status != TournamentStatus.Cancelled)
            .Where(t =>
                (t.Status == TournamentStatus.Scheduled && t.RegistrationStartsAtUtc <= now) ||
                (t.Status == TournamentStatus.RegistrationOpen && t.RegistrationEndsAtUtc <= now) ||
                (t.Status == TournamentStatus.RegistrationClosed) ||
                (t.Status == TournamentStatus.BracketGenerated && t.StartsAtUtc <= now) ||
                (t.Status == TournamentStatus.InProgress &&
                    _tournaments.Rounds.Any(r => r.TournamentId == t.Id &&
                        r.Status != TournamentRoundStatus.Completed &&
                        r.StartsAtUtc <= now)))
            .Select(t => t.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var id in dueIds)
        {
            await AdvanceTournamentAsync(id, cancellationToken);
        }
    }

    public async Task<TournamentGroundsStatus> GetStatusAsync(Guid characterId, CancellationToken cancellationToken)
    {
        await EnsureUpcomingTournamentsAsync(cancellationToken);
        await AdvanceDueTournamentsAsync(cancellationToken);

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
            .Where(t =>
                _tournaments.Participants.Any(p => p.TournamentId == t.Id && p.CharacterId == characterId) ||
                _tournaments.RewardGrants.Any(r => r.TournamentId == t.Id && r.CharacterId == characterId))
            .OrderByDescending(t => t.CompletedAtUtc ?? t.CancelledAtUtc ?? t.UpdatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentSummaries = new List<TournamentSummary>();
        foreach (var tournament in recentTournaments)
        {
            recentSummaries.Add(await MapSummaryAsync(tournament, characterId, now, cancellationToken));
        }

        return new TournamentGroundsStatus(now, summaries.FirstOrDefault(), summaries.Skip(1).ToList(), recentSummaries);
    }

    public async Task<TournamentDetails?> GetDetailsAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        await AdvanceTournamentAsync(tournamentId, cancellationToken);

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
        var owners = await _tournaments.Participants
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

        var placements = await _tournaments.Participants
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
                    Points = group.Sum(p => CalculateTournamentPoints(p.FinalPlacement)),
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
        await AdvanceTournamentAsync(tournamentId, cancellationToken);

        var tournament = await _tournaments.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId, cancellationToken);
        if (tournament is null) return null;

        var teamMap = (await MapTeamsAsync(tournamentId, characterId, cancellationToken))
            .ToDictionary(t => t.TeamId);

        var rounds = await _tournaments.Rounds
            .Where(r => r.TournamentId == tournamentId)
            .Include(r => r.Matches)
            .OrderBy(r => r.RoundNumber)
            .ToListAsync(cancellationToken);

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
                        m.BattleHistoryId))
                    .ToList()))
                .ToList());
    }

    public async Task<CombatResult?> GetMatchReplayAsync(
        Guid characterId,
        Guid tournamentId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var replay = await _tournaments.CombatReplays
            .FirstOrDefaultAsync(r => r.TournamentId == tournamentId && r.MatchId == matchId, cancellationToken);
        if (replay is null) return null;

        var canView = await _tournaments.Participants
            .AnyAsync(p => p.TournamentId == tournamentId && p.CharacterId == characterId, cancellationToken);
        if (!canView)
        {
            canView = await _tournaments.RewardGrants
                .AnyAsync(r => r.TournamentId == tournamentId && r.CharacterId == characterId, cancellationToken);
        }

        if (!canView) return null;

        return JsonSerializer.Deserialize<CombatResult>(replay.CombatResultJson, ReplayJsonOptions);
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
                r.Status.ToString(),
                r.CreatedAtUtc,
                r.ClaimedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<RegisterTournamentResult?> RegisterAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return null;

        await AdvanceTournamentAsync(tournamentId, cancellationToken);
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

        var snapshot = await _characterSnapshotService.CreateAsync(characterId, cancellationToken);
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
        tournamentSnapshot.SnapshotJson = BuildSnapshotJson(snapshot, character, tier.Id, now);
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

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(tournament, "TournamentRegistrationUpdated", now, cancellationToken);

        return new RegisterTournamentResult(
            true,
            participant.Id,
            tournamentSnapshot.Id,
            participant.EntryArenaRating,
            participant.EntryRankTier,
            "Registered. Your current combat setup has been locked for this tournament.");
    }

    public async Task<WithdrawTournamentResult?> WithdrawAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.AllowWithdrawDuringRegistration) return null;

        await AdvanceTournamentAsync(tournamentId, cancellationToken);
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
        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(tournament, "TournamentRegistrationUpdated", now, cancellationToken);

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
        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(tournament, "TournamentTeamUpdated", now, cancellationToken);

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

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(tournamentId, "TournamentTeamUpdated", now, cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> AcceptTeamInviteAsync(Guid characterId, Guid inviteId, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);

        var invite = await _tournaments.TeamInvites
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);
        if (invite is null || invite.Status != TournamentTeamRequestStatus.Pending || invite.Team.MemberCount >= 3) return null;

        await _tournamentLockService.LockTournamentAsync(invite.TournamentId, cancellationToken);
        var now = UtcNow();
        if (!await CanMutateTeamsAsync(invite.TournamentId, now, cancellationToken)) return null;

        var participant = await GetRegisteredParticipantAsync(characterId, invite.TournamentId, cancellationToken);
        if (participant is null || participant.Id != invite.InvitedParticipantId || participant.TeamId.HasValue) return null;

        participant.TeamId = invite.TeamId;
        participant.IsTeamOwner = false;
        participant.UpdatedAtUtc = now;
        invite.Team.MemberCount++;
        invite.Team.UpdatedAtUtc = now;
        invite.Status = TournamentTeamRequestStatus.Accepted;
        invite.UpdatedAtUtc = now;
        await CancelPendingRequestsForParticipantAsync(invite.TournamentId, participant.Id, now, cancellationToken);

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(invite.TournamentId, "TournamentTeamUpdated", now, cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> ApplyToTeamAsync(
        Guid characterId,
        Guid tournamentId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
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
        await _tournaments.SaveChangesAsync(cancellationToken);
        await PublishTournamentEventAsync(tournamentId, "TournamentTeamUpdated", now, cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<TournamentTeamActionResult?> AcceptTeamApplicationAsync(
        Guid characterId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);

        var application = await _tournaments.TeamApplications
            .Include(a => a.Team)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
        if (application is null || application.Status != TournamentTeamRequestStatus.Pending || application.Team.MemberCount >= 3) return null;

        await _tournamentLockService.LockTournamentAsync(application.TournamentId, cancellationToken);
        var now = UtcNow();
        if (!await CanMutateTeamsAsync(application.TournamentId, now, cancellationToken)) return null;

        if (!await IsTeamOwnerAsync(characterId, application.Team, cancellationToken)) return null;

        var participant = await _tournaments.Participants
            .FirstOrDefaultAsync(p => p.Id == application.ApplicantParticipantId, cancellationToken);
        if (participant is null || participant.TeamId.HasValue || participant.Status == TournamentParticipantStatus.Withdrawn) return null;

        participant.TeamId = application.TeamId;
        participant.IsTeamOwner = false;
        participant.UpdatedAtUtc = now;
        application.Team.MemberCount++;
        application.Team.UpdatedAtUtc = now;
        application.Status = TournamentTeamRequestStatus.Accepted;
        application.UpdatedAtUtc = now;
        await CancelPendingRequestsForParticipantAsync(application.TournamentId, participant.Id, now, cancellationToken);

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(application.TournamentId, "TournamentTeamUpdated", now, cancellationToken);
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

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(tournamentId, "TournamentTeamUpdated", now, cancellationToken);
        return new TournamentTeamActionResult(true);
    }

    public async Task<ClaimTournamentRewardsResult> ClaimRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken)
    {
        await using var transaction = await BeginOwnedTransactionIfNeededAsync(cancellationToken);

        var character = await _tournaments.Characters
            .Include(c => c.ArenaProfile)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);
        if (character?.ArenaProfile is null)
        {
            return new ClaimTournamentRewardsResult(false, 0, 0, 0);
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
            return new ClaimTournamentRewardsResult(false, 0, 0, 0);
        }

        var now = UtcNow();
        var glory = rewards.Sum(r => r.ArenaGlory);
        var cinders = rewards.Sum(r => r.Cinders);
        var soulstones = rewards.Sum(r => r.Soulstones);

        character.ArenaProfile.Glory += glory;
        character.Cinders += cinders;
        character.Soulstones += soulstones;

        foreach (var reward in rewards)
        {
            reward.Status = TournamentRewardStatus.Claimed;
            reward.ClaimedAtUtc = now;
        }

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await PublishTournamentEventAsync(rewards[0].TournamentId, "TournamentRewardsAvailable", now, cancellationToken);

        return new ClaimTournamentRewardsResult(true, glory, cinders, soulstones);
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
        var progressionSteps = 0;
        while (changed)
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
                    tournament.Status = TournamentStatus.InProgress;
                    changed = Touch(tournament, now);
                    break;
                case TournamentStatus.InProgress:
                    changed = await ResolveDueRoundsAsync(tournament, now, cancellationToken);
                    break;
            }

            if (changed)
            {
                changedAny = true;
                await _tournaments.SaveChangesAsync(cancellationToken);
            }
        }

        await _tournaments.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (changedAny)
        {
            await PublishTournamentEventAsync(tournament, "TournamentStateChanged", now, cancellationToken);
        }
    }

    private async Task GenerateBracketAsync(TournamentInstance tournament, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await _tournaments.Rounds.AnyAsync(r => r.TournamentId == tournament.Id, cancellationToken))
        {
            return;
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
    }

    private async Task<bool> ResolveDueRoundsAsync(TournamentInstance tournament, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var round = await _tournaments.Rounds
            .Where(r => r.TournamentId == tournament.Id && r.Status != TournamentRoundStatus.Completed && r.StartsAtUtc <= now)
            .OrderBy(r => r.RoundNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (round is null) return false;

        var changed = false;
        if (round.Status != TournamentRoundStatus.Resolving)
        {
            round.Status = TournamentRoundStatus.Resolving;
            round.UpdatedAtUtc = now;
            changed = true;
        }

        var matches = await _tournaments.Matches
            .Where(m => m.TournamentId == tournament.Id && m.RoundNumber == round.RoundNumber)
            .OrderBy(m => m.MatchNumber)
            .ToListAsync(cancellationToken);

        var readyMatches = matches.Where(m => m.Status == TournamentMatchStatus.Ready).ToList();
        foreach (var match in readyMatches)
        {
            await ResolveMatchAsync(tournament, match, now, cancellationToken);
            changed = true;
        }

        if (matches.All(m => m.Status is TournamentMatchStatus.Completed or TournamentMatchStatus.Bye))
        {
            round.Status = TournamentRoundStatus.Completed;
            round.ResolvedAtUtc = now;
            round.UpdatedAtUtc = now;
            changed = true;
            await PublishTournamentEventAsync(tournament, "TournamentRoundResolved", now, cancellationToken);

            var finalMatch = matches.Count == 1 && round.RoundNumber == await GetRoundCountAsync(tournament.Id, cancellationToken);
            if (finalMatch && matches[0].WinnerParticipantId.HasValue)
            {
                await CompleteTournamentAsync(tournament, matches[0].WinnerParticipantId!.Value, now, cancellationToken);
            }
        }

        return changed;
    }

    private async Task ResolveMatchAsync(TournamentInstance tournament, TournamentMatch match, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!match.PlayerOneParticipantId.HasValue || !match.PlayerTwoParticipantId.HasValue) return;

        match.Status = TournamentMatchStatus.Resolving;
        var p1 = await LoadTeamAsync(match.PlayerOneParticipantId.Value, cancellationToken);
        var p2 = await LoadTeamAsync(match.PlayerTwoParticipantId.Value, cancellationToken);
        if (p1 is null || p2 is null) return;

        var result = await ExecuteTournamentCombatAsync(tournament.Id, match.Id, p1, p2, now, cancellationToken);
        var p1Wins = result.Outcome switch
        {
            BattleOutcome.Victory => true,
            BattleOutcome.Defeat => false,
            _ => (p1.Seed ?? int.MaxValue) <= (p2.Seed ?? int.MaxValue)
        };

        var winner = p1Wins ? p1 : p2;
        var loser = p1Wins ? p2 : p1;

        match.WinnerParticipantId = winner.Id;
        match.LoserParticipantId = loser.Id;
        match.Outcome = result.Outcome == BattleOutcome.Draw
            ? TournamentMatchOutcome.DrawAdvancedBySeed
            : p1Wins ? TournamentMatchOutcome.PlayerOneWin : TournamentMatchOutcome.PlayerTwoWin;
        match.Status = TournamentMatchStatus.Completed;
        match.CombatSessionId = result.BattleId;
        match.ResolvedAtUtc = now;
        match.UpdatedAtUtc = now;

        match.BattleHistoryId = await SaveTournamentBattleHistoryAsync(
            match,
            p1,
            p2,
            winner,
            result.Outcome,
            result.CombatResult,
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

    private async Task<(Guid BattleId, BattleOutcome Outcome, CombatResult CombatResult)> ExecuteTournamentCombatAsync(
        Guid tournamentId,
        Guid matchId,
        TournamentTeam playerOne,
        TournamentTeam playerTwo,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var playerOneMembers = await GetTeamMembersAsync(playerOne.Id, cancellationToken);
        var playerTwoMembers = await GetTeamMembersAsync(playerTwo.Id, cancellationToken);
        var characterIds = playerOneMembers.Concat(playerTwoMembers).Select(p => p.CharacterId).ToList();
        var entities = await _entityService.GetEntitiesByIdsForCombatAsync(characterIds, cancellationToken);
        if (entities.Count < characterIds.Count)
        {
            var fallback = CreateFallbackCombatResult(BattleOutcome.Draw, now);
            return (Guid.NewGuid(), fallback.Outcome, fallback);
        }

        var sourceById = entities.Cast<Character>().ToDictionary(e => e.Id);
        var friendlyRuntime = new List<CombatRuntimeParticipant>();
        var hostileRuntime = new List<CombatRuntimeParticipant>();
        var slots = new List<CombatParticipantSlot>();
        var combatEntities = new List<CombatEntity>();

        foreach (var participant in playerOneMembers)
        {
            var snapshot = await LoadSnapshotAsync(participant.SnapshotId, cancellationToken);
            if (snapshot is null)
            {
                var fallback = CreateFallbackCombatResult(BattleOutcome.Draw, now);
                return (Guid.NewGuid(), fallback.Outcome, fallback);
            }

            var source = sourceById[participant.CharacterId];
            var combat = await CreateSnapshotCombatEntityAsync(source, snapshot, cancellationToken);
            combatEntities.Add(combat);
            var slot = new CombatParticipantSlot(participant.CharacterId.ToString(), participant.CharacterId, CombatSide.Friendly);
            slots.Add(slot);
            friendlyRuntime.Add(new CombatRuntimeParticipant(slot, source, combat));
        }

        foreach (var participant in playerTwoMembers)
        {
            var snapshot = await LoadSnapshotAsync(participant.SnapshotId, cancellationToken);
            if (snapshot is null)
            {
                var fallback = CreateFallbackCombatResult(BattleOutcome.Draw, now);
                return (Guid.NewGuid(), fallback.Outcome, fallback);
            }

            var source = sourceById[participant.CharacterId];
            var combat = await CreateSnapshotCombatEntityAsync(source, snapshot, cancellationToken);
            combatEntities.Add(combat);
            var slot = new CombatParticipantSlot(participant.CharacterId.ToString(), participant.CharacterId, CombatSide.Hostile);
            slots.Add(slot);
            hostileRuntime.Add(new CombatRuntimeParticipant(slot, source, combat));
        }

        await _combatSetupService.PrepareEntitiesForCombat(combatEntities);

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

        var combatResult = await _combatEngineExecutor.ExecuteAsync(runtime, cancellationToken);
        combatResult = _combatEncounterResultFactory.Create(runtime, combatResult).CombatResult;
        return (battleId, combatResult.Outcome, combatResult);
    }

    private static CombatResult CreateFallbackCombatResult(BattleOutcome outcome, DateTimeOffset now)
    {
        return new CombatResult
        {
            Outcome = outcome,
            StartedAt = now,
            Duration = 1
        };
    }

    private async Task<CharacterSnapshot?> LoadSnapshotAsync(Guid tournamentSnapshotId, CancellationToken cancellationToken)
    {
        return await _tournaments.CombatSnapshots
            .Include(x => x.CharacterSnapshot)
                .ThenInclude(x => x.BaseAttributes)
            .Include(x => x.CharacterSnapshot)
                .ThenInclude(x => x.Equipment)
                    .ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.CharacterSnapshot)
                .ThenInclude(x => x.EquippedEssences)
            .Where(x => x.Id == tournamentSnapshotId)
            .Select(x => x.CharacterSnapshot)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<CombatEntity> CreateSnapshotCombatEntityAsync(Character sourceCharacter, CharacterSnapshot snapshot, CancellationToken cancellationToken)
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

        var itemBases = await _itemBaseRepository.GetItemBasesByIdsAsync(
            snapshot.Equipment.Select(x => x.ItemBaseId).Distinct().ToArray(),
            cancellationToken);

        template.Equipment = snapshot.Equipment
            .OrderBy(x => x.Slot)
            .Where(x => itemBases.ContainsKey(x.ItemBaseId))
            .Select(x => new EquipmentInstance
            {
                Id = x.EquipmentInstanceId,
                ItemBaseId = x.ItemBaseId,
                ItemBase = itemBases[x.ItemBaseId],
                Rarity = x.Rarity,
                Potential = x.Potential,
                ItemXp = x.ItemXp,
                IsMasterpiece = x.IsMasterpiece,
                IsLevelingItem = x.IsLevelingItem,
                InstanceModifiers = x.InstanceModifiers.ToList()
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

        var participants = await _tournaments.Participants
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
        }

        await PublishTournamentEventAsync(tournament, "TournamentCompleted", now, cancellationToken);
    }

    private TournamentRewardGrant BuildReward(Guid tournamentId, Guid characterId, int placement, DateTimeOffset now)
    {
        var tier = GetRewardTiers()
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
            Status = TournamentRewardStatus.Unclaimed,
            CreatedAtUtc = now
        };
    }

    private static int CalculateTournamentPoints(int? placement)
    {
        return placement switch
        {
            1 => 100,
            2 => 60,
            <= 4 => 35,
            <= 8 => 20,
            null => 0,
            _ => 10
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
        if (definition is not null) return definition;

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

    private IReadOnlyList<TournamentRewardTierOptions> GetRewardTiers()
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
        Character character,
        string rankTier,
        DateTimeOffset createdAtUtc)
    {
        var payload = new TournamentSnapshotAuditPayload(
            snapshot.Id,
            snapshot.CharacterId,
            snapshot.Name,
            snapshot.Level,
            character.ArenaProfile.Rating,
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
        try
        {
            var hasBracket = await _tournaments.Rounds
                .AnyAsync(r => r.TournamentId == tournament.Id, cancellationToken);
            var currentRound = await _tournaments.Rounds
                .Where(r => r.TournamentId == tournament.Id && r.Status != TournamentRoundStatus.Completed)
                .OrderBy(r => r.RoundNumber)
                .Select(r => new { r.RoundNumber, r.StartsAtUtc })
                .FirstOrDefaultAsync(cancellationToken);
            var nextActionAt = tournament.Status switch
            {
                TournamentStatus.Scheduled => tournament.RegistrationStartsAtUtc,
                TournamentStatus.RegistrationOpen => tournament.RegistrationEndsAtUtc,
                TournamentStatus.RegistrationClosed or TournamentStatus.BracketGenerated => tournament.StartsAtUtc,
                TournamentStatus.InProgress => currentRound?.StartsAtUtc,
                _ => null
            };

            await _gameRealtime.PublishAsync(
                new Audience.World(),
                new TournamentGroundsUpdated(
                    tournament.Id,
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
                    now),
                nameof(TournamentGroundsService),
                cancellationToken);
        }
        catch
        {
            // REST remains authoritative; realtime is only a convenience refresh signal.
        }
    }

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


