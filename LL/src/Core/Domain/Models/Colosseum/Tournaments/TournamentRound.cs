namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentRound
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public int RoundNumber { get; set; }
    public string Name { get; set; } = null!;
    public TournamentRoundStatus Status { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<TournamentMatch> Matches { get; set; } = [];
}
