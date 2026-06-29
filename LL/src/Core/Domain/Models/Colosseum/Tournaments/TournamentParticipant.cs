namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentParticipant
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid AccountId { get; set; }
    public Guid? TeamId { get; set; }
    public TournamentTeam? Team { get; set; }
    public bool IsTeamOwner { get; set; }
    public Guid SnapshotId { get; set; }
    public TournamentCombatSnapshot Snapshot { get; set; } = null!;
    public int? Seed { get; set; }
    public int EntryArenaRating { get; set; }
    public string EntryRankTier { get; set; } = null!;
    public TournamentParticipantStatus Status { get; set; }
    public int? EliminatedInRoundNumber { get; set; }
    public int? FinalPlacement { get; set; }
    public DateTimeOffset RegisteredAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
