namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentTeam
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Guid OwnerParticipantId { get; set; }
    public int? Seed { get; set; }
    public TournamentTeamStatus Status { get; set; }
    public int MemberCount { get; set; }
    public int? EliminatedInRoundNumber { get; set; }
    public int? FinalPlacement { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
