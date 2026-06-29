using Application.UseCases.Colosseum.Tournaments;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;

namespace Application.Interfaces.Services.LL.Colosseum;

public interface ITournamentGroundsService
{
    Task EnsureUpcomingTournamentsAsync(CancellationToken cancellationToken);
    Task AdvanceDueTournamentsAsync(CancellationToken cancellationToken);
    Task<TournamentGroundsStatusDto> GetStatusAsync(Guid characterId, CancellationToken cancellationToken);
    Task<TournamentDetailsDto?> GetDetailsAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentHistoryEntryDto>> GetHistoryAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentHallOfFameEntryDto>> GetHallOfFameAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentSeasonLeaderboardEntryDto>> GetSeasonLeaderboardAsync(CancellationToken cancellationToken);
    Task<TournamentBracketDto?> GetBracketAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<CombatResultDto?> GetMatchReplayAsync(Guid characterId, Guid tournamentId, Guid matchId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentRewardGrantDto>> GetRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken);
    Task<RegisterTournamentResponseDto?> RegisterAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<WithdrawTournamentResponseDto?> WithdrawAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<CreateTournamentTeamResponseDto?> CreateTeamAsync(Guid characterId, Guid tournamentId, string name, CancellationToken cancellationToken);
    Task<TournamentTeamActionResponseDto?> InviteToTeamAsync(Guid characterId, Guid tournamentId, Guid teamId, Guid invitedParticipantId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResponseDto?> AcceptTeamInviteAsync(Guid characterId, Guid inviteId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResponseDto?> ApplyToTeamAsync(Guid characterId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResponseDto?> AcceptTeamApplicationAsync(Guid characterId, Guid applicationId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResponseDto?> KickTeamMemberAsync(Guid characterId, Guid tournamentId, Guid teamId, Guid participantId, CancellationToken cancellationToken);
    Task<ClaimTournamentRewardsResponseDto> ClaimRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken);
}
