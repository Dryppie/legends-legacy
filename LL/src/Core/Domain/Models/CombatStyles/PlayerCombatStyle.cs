namespace Domain.Models.CombatStyles;

public sealed class PlayerCombatStyle
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string StyleId { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public string? SelectedFocusId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
