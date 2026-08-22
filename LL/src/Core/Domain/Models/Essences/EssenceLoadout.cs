namespace Domain.Models.Essences;

public class EssenceLoadout
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EssenceCombatActivity AutoUseActivities { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<EssenceLoadoutSlot> Slots { get; set; } = [];
}
