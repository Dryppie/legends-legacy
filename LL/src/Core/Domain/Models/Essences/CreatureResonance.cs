namespace Domain.Models.Essences;

public class CreatureResonance
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string CreatureId { get; set; } = string.Empty;
    public double ResonanceValue { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
