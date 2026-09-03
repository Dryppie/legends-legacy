using Application.Common.Mappings;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Colosseum.Tournaments;

namespace Application.UseCases.Colosseum.Tournaments;

public sealed record TournamentGroundsStatusDto(
    DateTimeOffset NowUtc,
    TournamentSummaryDto? CurrentTournament,
    IReadOnlyList<TournamentSummaryDto> UpcomingTournaments,
    IReadOnlyList<TournamentSummaryDto> RecentTournaments,
    bool DevelopmentToolsEnabled) : IMapFrom<TournamentGroundsStatus>
{
    public TournamentGroundsStatusDto()
        : this(default, null, [], [], false)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentGroundsStatus, TournamentGroundsStatusDto>();
    }
}

public sealed record TournamentSummaryDto(
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
    string? CancellationReason) : IMapFrom<TournamentSummary>
{
    public TournamentSummaryDto()
        : this(Guid.Empty, string.Empty, string.Empty, default, default, default, 0, 0, 0, false, false, null, null, false, null, null, null, null, null, null, null)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentSummary, TournamentSummaryDto>();
    }
}

public sealed record TournamentDetailsDto(
    TournamentSummaryDto Summary,
    IReadOnlyList<TournamentParticipantDto> Participants,
    IReadOnlyList<TournamentTeamDto> Teams,
    IReadOnlyList<TournamentRewardGrantDto> Rewards) : IMapFrom<TournamentDetails>
{
    public TournamentDetailsDto()
        : this(new TournamentSummaryDto(), [], [], [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentDetails, TournamentDetailsDto>();
    }
}

public sealed record TournamentHistoryEntryDto(
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
    int ReplayCount) : IMapFrom<TournamentHistoryEntry>
{
    public TournamentHistoryEntryDto()
        : this(Guid.Empty, 0, string.Empty, string.Empty, null, null, null, Guid.Empty, null, 0, string.Empty, string.Empty, null, null, 0)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentHistoryEntry, TournamentHistoryEntryDto>();
    }
}

public sealed record TournamentHallOfFameEntryDto(
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
    int ReplayCount) : IMapFrom<TournamentHallOfFameEntry>
{
    public TournamentHallOfFameEntryDto()
        : this(Guid.Empty, 0, string.Empty, default, 0, Guid.Empty, Guid.Empty, string.Empty, null, 0, string.Empty, 0)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentHallOfFameEntry, TournamentHallOfFameEntryDto>();
    }
}

public sealed record TournamentSeasonLeaderboardEntryDto(
    int Rank,
    Guid CharacterId,
    string CharacterName,
    int Points,
    int TournamentsEntered,
    int Championships,
    int FinalistFinishes,
    int? BestPlacement,
    DateTimeOffset? LatestCompletedAtUtc,
    string SeasonKey) : IMapFrom<TournamentSeasonLeaderboardEntry>
{
    public TournamentSeasonLeaderboardEntryDto()
        : this(0, Guid.Empty, string.Empty, 0, 0, 0, 0, null, null, string.Empty)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentSeasonLeaderboardEntry, TournamentSeasonLeaderboardEntryDto>();
    }
}

public sealed record TournamentBracketDto(
    Guid TournamentId,
    string Status,
    IReadOnlyList<TournamentRoundDto> Rounds) : IMapFrom<TournamentBracket>
{
    public TournamentBracketDto()
        : this(Guid.Empty, string.Empty, [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentBracket, TournamentBracketDto>();
    }
}

public sealed record TournamentParticipantDto(
    Guid ParticipantId,
    Guid CharacterId,
    string CharacterName,
    Guid? TeamId,
    bool IsTeamOwner,
    int? Seed,
    int EntryArenaRating,
    string EntryRankTier,
    string Status,
    int? FinalPlacement) : IMapFrom<TournamentParticipantEntry>
{
    public TournamentParticipantDto()
        : this(Guid.Empty, Guid.Empty, string.Empty, null, false, null, 0, string.Empty, string.Empty, null)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentParticipantEntry, TournamentParticipantDto>();
    }
}

public sealed record TournamentTeamDto(
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
    IReadOnlyList<TournamentParticipantDto> Members,
    IReadOnlyList<TournamentTeamApplicationDto> Applications,
    IReadOnlyList<TournamentTeamInviteDto> Invites) : IMapFrom<TournamentTeamEntry>
{
    public TournamentTeamDto()
        : this(Guid.Empty, string.Empty, string.Empty, Guid.Empty, string.Empty, 0, 0, null, null, false, false, false, [], [], [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentTeamEntry, TournamentTeamDto>();
    }
}

public sealed record TournamentTeamApplicationDto(
    Guid ApplicationId,
    Guid ApplicantParticipantId,
    Guid ApplicantCharacterId,
    string ApplicantName,
    string Status,
    DateTimeOffset CreatedAtUtc) : IMapFrom<TournamentTeamApplicationEntry>
{
    public TournamentTeamApplicationDto()
        : this(Guid.Empty, Guid.Empty, Guid.Empty, string.Empty, string.Empty, default)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentTeamApplicationEntry, TournamentTeamApplicationDto>();
    }
}

public sealed record TournamentTeamInviteDto(
    Guid InviteId,
    Guid InvitedParticipantId,
    Guid InvitedCharacterId,
    string InvitedName,
    string Status,
    DateTimeOffset CreatedAtUtc) : IMapFrom<TournamentTeamInviteEntry>
{
    public TournamentTeamInviteDto()
        : this(Guid.Empty, Guid.Empty, Guid.Empty, string.Empty, string.Empty, default)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentTeamInviteEntry, TournamentTeamInviteDto>();
    }
}

public sealed record TournamentRoundDto(
    Guid Id,
    int RoundNumber,
    string Name,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    IReadOnlyList<TournamentMatchDto> Matches) : IMapFrom<TournamentBracketRound>
{
    public TournamentRoundDto()
        : this(Guid.Empty, 0, string.Empty, string.Empty, default, null, [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentBracketRound, TournamentRoundDto>();
    }
}

public sealed record TournamentMatchDto(
    Guid Id,
    int RoundNumber,
    int MatchNumber,
    string Status,
    string Outcome,
    TournamentTeamDto? PlayerOne,
    TournamentTeamDto? PlayerTwo,
    Guid? WinnerTeamId,
    Guid? CombatSessionId,
    Guid? BattleHistoryId,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset? PlaybackStartedAtUtc,
    DateTimeOffset? PlaybackEndsAtUtc,
    bool HasPlayback) : IMapFrom<TournamentBracketMatch>
{
    public TournamentMatchDto()
        : this(Guid.Empty, 0, 0, string.Empty, string.Empty, null, null, null, null, null, null, null, null, false)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentBracketMatch, TournamentMatchDto>();
    }
}

public sealed record RegisterTournamentResponseDto(
    bool Registered,
    Guid ParticipantId,
    Guid SnapshotId,
    int EntryArenaRating,
    string EntryRankTier,
    string Message) : IMapFrom<RegisterTournamentResult>
{
    public RegisterTournamentResponseDto()
        : this(false, Guid.Empty, Guid.Empty, 0, string.Empty, string.Empty)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<RegisterTournamentResult, RegisterTournamentResponseDto>();
    }
}

public sealed record WithdrawTournamentResponseDto(bool Withdrawn) : IMapFrom<WithdrawTournamentResult>
{
    public WithdrawTournamentResponseDto()
        : this(false)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<WithdrawTournamentResult, WithdrawTournamentResponseDto>();
    }
}

public sealed record CreateTournamentTeamResponseDto(bool Created, Guid TeamId) : IMapFrom<CreateTournamentTeamResult>
{
    public CreateTournamentTeamResponseDto()
        : this(false, Guid.Empty)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<CreateTournamentTeamResult, CreateTournamentTeamResponseDto>();
    }
}

public sealed record TournamentTeamActionResponseDto(bool Succeeded) : IMapFrom<TournamentTeamActionResult>
{
    public TournamentTeamActionResponseDto()
        : this(false)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentTeamActionResult, TournamentTeamActionResponseDto>();
    }
}

public sealed record TournamentRewardGrantDto(
    Guid Id,
    Guid TournamentId,
    string TournamentName,
    string RewardKey,
    int? Placement,
    int ArenaGlory,
    int Cinders,
    int Soulstones,
    int SigilFragments,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClaimedAtUtc) : IMapFrom<TournamentRewardGrantEntry>
{
    public TournamentRewardGrantDto()
        : this(Guid.Empty, Guid.Empty, string.Empty, string.Empty, null, 0, 0, 0, 0, string.Empty, default, null)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentRewardGrantEntry, TournamentRewardGrantDto>();
    }
}

public sealed record TournamentRewardTierDto(
    string Key,
    int? MaxPlacement,
    int ArenaGlory,
    int Cinders,
    int Soulstones,
    int SigilFragments) : IMapFrom<TournamentRewardTier>
{
    public TournamentRewardTierDto()
        : this(string.Empty, null, 0, 0, 0, 0)
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TournamentRewardTier, TournamentRewardTierDto>();
    }
}

public sealed record ClaimTournamentRewardsResponseDto(
    bool Claimed,
    int ArenaGlory,
    int Cinders,
    int Soulstones,
    int SigilFragments,
    IReadOnlyList<InventoryItemDto> InventoryRewards) : IMapFrom<ClaimTournamentRewardsResult>
{
    public ClaimTournamentRewardsResponseDto()
        : this(false, 0, 0, 0, 0, [])
    {
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ClaimTournamentRewardsResult, ClaimTournamentRewardsResponseDto>();
    }
}
