using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Combat;
using Application.UseCases.Colosseum.Tournaments;

namespace Application.Interfaces.Services.LL.Colosseum;

public interface ITournamentGroundsService
{
    Task EnsureUpcomingTournamentsAsync(CancellationToken cancellationToken);
    Task AdvanceDueTournamentsAsync(CancellationToken cancellationToken);
    Task<StartDevelopmentTournamentResult> StartDevelopmentTournamentAsync(Guid characterId, CancellationToken cancellationToken);
    Task<TournamentGroundsStatus> GetStatusAsync(Guid characterId, CancellationToken cancellationToken);
    Task<TournamentDetails?> GetDetailsAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentHistoryEntry>> GetHistoryAsync(Guid characterId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentHallOfFameEntry>> GetHallOfFameAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TournamentSeasonLeaderboardEntry>> GetSeasonLeaderboardAsync(CancellationToken cancellationToken);
    Task<TournamentBracket?> GetBracketAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<CombatResult?> GetMatchReplayAsync(Guid characterId, Guid tournamentId, Guid matchId, CancellationToken cancellationToken);
    Task<TournamentPlaybackManifestDto?> GetMatchPlaybackAsync(Guid characterId, Guid tournamentId, Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult<TournamentPlaybackManifestDto?>(null);
    Task<TournamentPlaybackBundleContentDto?> GetMatchPlaybackBundleAsync(Guid characterId, Guid tournamentId, Guid matchId, CancellationToken cancellationToken) =>
        Task.FromResult<TournamentPlaybackBundleContentDto?>(null);
    Task<IReadOnlyList<TournamentRewardGrantEntry>> GetRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken);
    Task<RegisterTournamentResult?> RegisterAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResult?> UpdateLoadoutAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<WithdrawTournamentResult?> WithdrawAsync(Guid characterId, Guid tournamentId, CancellationToken cancellationToken);
    Task<CreateTournamentTeamResult?> CreateTeamAsync(Guid characterId, Guid tournamentId, string name, CancellationToken cancellationToken);
    Task<TournamentTeamActionResult?> InviteToTeamAsync(Guid characterId, Guid tournamentId, Guid teamId, Guid invitedParticipantId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResult?> AcceptTeamInviteAsync(Guid characterId, Guid inviteId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResult?> ApplyToTeamAsync(Guid characterId, Guid tournamentId, Guid teamId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResult?> AcceptTeamApplicationAsync(Guid characterId, Guid applicationId, CancellationToken cancellationToken);
    Task<TournamentTeamActionResult?> KickTeamMemberAsync(Guid characterId, Guid tournamentId, Guid teamId, Guid participantId, CancellationToken cancellationToken);
    Task<ClaimTournamentRewardsResult> ClaimRewardsAsync(Guid characterId, Guid? tournamentId, CancellationToken cancellationToken);
}


