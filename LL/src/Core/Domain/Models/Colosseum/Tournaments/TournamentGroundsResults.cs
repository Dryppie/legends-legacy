namespace Domain.Models.Colosseum.Tournaments;

public sealed record TournamentGroundsStatus(
    DateTimeOffset NowUtc,
    TournamentSummary? CurrentTournament,
    IReadOnlyList<TournamentSummary> UpcomingTournaments,
    IReadOnlyList<TournamentSummary> RecentTournaments);

public sealed record TournamentSummary(
    Guid Id,
    string Name,
    string Status,
    DateTimeOffset RegistrationStartsAtUtc,
    DateTimeOffset RegistrationEndsAtUtc,
    DateTimeOffset StartsAtUtc,
    int RegisteredParticipantCount,
    int MinParticipants,
    int MaxParticipants,
    bool IsRegistered,
    bool CanRegister,
    string? CannotRegisterReason,
    Guid? PlayerParticipantId,
    bool HasUnclaimedRewards,
    string? PlayerStatus,
    int? PlayerSeed,
    int? PlayerEntryArenaRating,
    int? PlayerFinalPlacement,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

public sealed record TournamentDetails(
    TournamentSummary Summary,
    IReadOnlyList<TournamentParticipantEntry> Participants,
    IReadOnlyList<TournamentTeamEntry> Teams,
    IReadOnlyList<TournamentRewardGrantEntry> Rewards);

public sealed record TournamentHistoryEntry(
    Guid TournamentId,
    int TournamentNumber,
    string TournamentName,
    string Status,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason,
    Guid ParticipantId,
    int? Seed,
    int EntryArenaRating,
    string EntryRankTier,
    string ParticipantStatus,
    int? FinalPlacement,
    string? RewardStatus,
    int ReplayCount);

public sealed record TournamentHallOfFameEntry(
    Guid TournamentId,
    int TournamentNumber,
    string TournamentName,
    DateTimeOffset CompletedAtUtc,
    int ParticipantCount,
    Guid ChampionParticipantId,
    Guid ChampionCharacterId,
    string ChampionName,
    int? ChampionSeed,
    int ChampionEntryArenaRating,
    string ChampionEntryRankTier,
    int ReplayCount);

public sealed record TournamentSeasonLeaderboardEntry(
    int Rank,
    Guid CharacterId,
    string CharacterName,
    int Points,
    int TournamentsEntered,
    int Championships,
    int FinalistFinishes,
    int? BestPlacement,
    DateTimeOffset? LatestCompletedAtUtc,
    string SeasonKey);

public sealed record TournamentBracket(
    Guid TournamentId,
    string Status,
    IReadOnlyList<TournamentBracketRound> Rounds);

public sealed record TournamentParticipantEntry(
    Guid ParticipantId,
    Guid CharacterId,
    string CharacterName,
    Guid? TeamId,
    bool IsTeamOwner,
    int? Seed,
    int EntryArenaRating,
    string EntryRankTier,
    string Status,
    int? FinalPlacement);

public sealed record TournamentTeamEntry(
    Guid TeamId,
    string Name,
    string Status,
    Guid OwnerParticipantId,
    string OwnerName,
    int MemberCount,
    int MissingParticipantCount,
    int? Seed,
    int? FinalPlacement,
    bool IsOpen,
    bool IsPlayerTeam,
    bool IsPlayerOwner,
    IReadOnlyList<TournamentParticipantEntry> Members,
    IReadOnlyList<TournamentTeamApplicationEntry> Applications,
    IReadOnlyList<TournamentTeamInviteEntry> Invites);

public sealed record TournamentTeamApplicationEntry(
    Guid ApplicationId,
    Guid ApplicantParticipantId,
    Guid ApplicantCharacterId,
    string ApplicantName,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record TournamentTeamInviteEntry(
    Guid InviteId,
    Guid InvitedParticipantId,
    Guid InvitedCharacterId,
    string InvitedName,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record TournamentBracketRound(
    Guid Id,
    int RoundNumber,
    string Name,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    IReadOnlyList<TournamentBracketMatch> Matches);

public sealed record TournamentBracketMatch(
    Guid Id,
    int RoundNumber,
    int MatchNumber,
    string Status,
    string Outcome,
    TournamentTeamEntry? PlayerOne,
    TournamentTeamEntry? PlayerTwo,
    Guid? WinnerTeamId,
    Guid? CombatSessionId,
    Guid? BattleHistoryId,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? PlaybackStartedAtUtc,
    DateTimeOffset? PlaybackEndsAtUtc,
    bool HasPlayback);

public sealed record RegisterTournamentResult(
    bool Registered,
    Guid ParticipantId,
    Guid SnapshotId,
    int EntryArenaRating,
    string EntryRankTier,
    string Message);

public sealed record WithdrawTournamentResult(bool Withdrawn);

public sealed record CreateTournamentTeamResult(bool Created, Guid TeamId);

public sealed record TournamentTeamActionResult(bool Succeeded, string? ErrorMessage = null);

public sealed record TournamentRewardGrantEntry(
    Guid Id,
    Guid TournamentId,
    string TournamentName,
    string RewardKey,
    int? Placement,
    int ArenaGlory,
    int Cinders,
    int Soulstones,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClaimedAtUtc);

public sealed record ClaimTournamentRewardsResult(
    bool Claimed,
    int ArenaGlory,
    int Cinders,
    int Soulstones);

