namespace Domain.Models.Colosseum.Tournaments;

public sealed record TournamentGroundsStatusModel(
    DateTimeOffset NowUtc,
    TournamentSummaryModel? CurrentTournament,
    IReadOnlyList<TournamentSummaryModel> UpcomingTournaments,
    IReadOnlyList<TournamentSummaryModel> RecentTournaments);

public sealed record TournamentSummaryModel(
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

public sealed record TournamentDetailsModel(
    TournamentSummaryModel Summary,
    IReadOnlyList<TournamentParticipantModel> Participants,
    IReadOnlyList<TournamentTeamModel> Teams,
    IReadOnlyList<TournamentRewardGrantModel> Rewards);

public sealed record TournamentHistoryEntryModel(
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

public sealed record TournamentHallOfFameEntryModel(
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

public sealed record TournamentSeasonLeaderboardEntryModel(
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

public sealed record TournamentBracketModel(
    Guid TournamentId,
    string Status,
    IReadOnlyList<TournamentRoundModel> Rounds);

public sealed record TournamentParticipantModel(
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

public sealed record TournamentTeamModel(
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
    IReadOnlyList<TournamentParticipantModel> Members,
    IReadOnlyList<TournamentTeamApplicationModel> Applications,
    IReadOnlyList<TournamentTeamInviteModel> Invites);

public sealed record TournamentTeamApplicationModel(
    Guid ApplicationId,
    Guid ApplicantParticipantId,
    Guid ApplicantCharacterId,
    string ApplicantName,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record TournamentTeamInviteModel(
    Guid InviteId,
    Guid InvitedParticipantId,
    Guid InvitedCharacterId,
    string InvitedName,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record TournamentRoundModel(
    Guid Id,
    int RoundNumber,
    string Name,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    IReadOnlyList<TournamentMatchModel> Matches);

public sealed record TournamentMatchModel(
    Guid Id,
    int RoundNumber,
    int MatchNumber,
    string Status,
    string Outcome,
    TournamentTeamModel? PlayerOne,
    TournamentTeamModel? PlayerTwo,
    Guid? WinnerTeamId,
    Guid? CombatSessionId,
    Guid? BattleHistoryId);

public sealed record RegisterTournamentResultModel(
    bool Registered,
    Guid ParticipantId,
    Guid SnapshotId,
    int EntryArenaRating,
    string EntryRankTier,
    string Message);

public sealed record WithdrawTournamentResultModel(bool Withdrawn);

public sealed record CreateTournamentTeamResultModel(bool Created, Guid TeamId);

public sealed record TournamentTeamActionResultModel(bool Succeeded);

public sealed record TournamentRewardGrantModel(
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

public sealed record ClaimTournamentRewardsResultModel(
    bool Claimed,
    int ArenaGlory,
    int Cinders,
    int Soulstones);
