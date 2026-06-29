namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentCombatReplay
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public Guid MatchId { get; set; }
    public TournamentMatch Match { get; set; } = null!;
    public Guid CombatSessionId { get; set; }
    public Guid BattleHistoryId { get; set; }
    public Guid PlayerOneCharacterId { get; set; }
    public Guid PlayerTwoCharacterId { get; set; }
    public string Outcome { get; set; } = null!;
    public DateTimeOffset StartedAtUtc { get; set; }
    public int Duration { get; set; }
    public string CombatResultJson { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
