namespace Domain.Models.CombatStyles;

public sealed class PlayerCombatStyleNode
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string StyleId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public int Rank { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
