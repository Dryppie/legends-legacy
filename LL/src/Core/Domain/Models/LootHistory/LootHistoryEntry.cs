using Domain.Models.Entities.Characters;

namespace Domain.Models.LootHistory;

public sealed class LootHistoryEntry
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public string ItemSnapshotJson { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
