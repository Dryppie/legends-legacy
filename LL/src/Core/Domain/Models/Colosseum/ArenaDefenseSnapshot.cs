using Domain.Models.Snapshots;

namespace Domain.Models.Colosseum;

public sealed class ArenaDefenseSnapshot
{
    public Guid CharacterId { get; set; }
    public Guid CharacterSnapshotId { get; set; }
    public CharacterSnapshot CharacterSnapshot { get; set; } = null!;
    public string LoadoutHash { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsOutdated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
