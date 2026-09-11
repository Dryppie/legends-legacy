namespace Domain.Models.Essences;

public class EssenceLoadout
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PresetSlot { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsUsable { get => _isUsable ?? PresetSlot <= 3; set => _isUsable = value; }
    private bool? _isUsable;
    public EssenceCombatActivity AutoUseActivities { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<EssenceLoadoutSlot> Slots { get; set; } = [];
}
