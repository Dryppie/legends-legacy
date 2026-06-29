using Domain.Models.Snapshots;

namespace Domain.Models.Colosseum.Tournaments;

public sealed class TournamentCombatSnapshot
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentInstance Tournament { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public CharacterSnapshot CharacterSnapshot { get; set; } = null!;
    public string SnapshotVersion { get; set; } = null!;
    public string SnapshotJson { get; set; } = null!;
    public int? PowerScore { get; set; }
    public int ArenaRatingAtSnapshot { get; set; }
    public string RankTierAtSnapshot { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
