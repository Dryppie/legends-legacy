namespace Domain.Models.Essences;

public sealed class CharacterCreatureArchiveEntry
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string CreatureDefinitionId { get; set; } = string.Empty;
    public string CreatureName { get; set; } = string.Empty;
    public int KillCount { get; set; }
    public bool IsCreatureFocus { get; set; }
    public DateTimeOffset? CreatureFocusSetAtUtc { get; set; }
    public long CreatureFocusTotalDurationSeconds { get; set; }
    public DateTimeOffset FirstDefeatedAtUtc { get; set; }
    public DateTimeOffset LastDefeatedAtUtc { get; set; }
}
