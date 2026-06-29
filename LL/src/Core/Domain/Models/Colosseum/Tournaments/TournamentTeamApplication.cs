namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentTeamApplication
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public Guid TeamId { get; set; }
    public TournamentTeam Team { get; set; } = null!;
    public Guid ApplicantParticipantId { get; set; }
    public TournamentTeamRequestStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
