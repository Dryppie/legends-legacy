namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentMatch
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public Guid RoundId { get; set; }
    public TournamentRound Round { get; set; } = null!;
    public int RoundNumber { get; set; }
    public int MatchNumber { get; set; }
    public Guid? PlayerOneParticipantId { get; set; }
    public Guid? PlayerTwoParticipantId { get; set; }
    public Guid? WinnerParticipantId { get; set; }
    public Guid? LoserParticipantId { get; set; }
    public TournamentMatchStatus Status { get; set; }
    public TournamentMatchOutcome Outcome { get; set; }
    public Guid? CombatSessionId { get; set; }
    public Guid? BattleHistoryId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
