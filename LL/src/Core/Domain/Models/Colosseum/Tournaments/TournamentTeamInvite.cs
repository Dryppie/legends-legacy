namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentTeamInvite
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;
    public Guid InviterParticipantId { get; set; }
    public Guid InvitedParticipantId { get; set; }
    public TournamentTeamRequestStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
