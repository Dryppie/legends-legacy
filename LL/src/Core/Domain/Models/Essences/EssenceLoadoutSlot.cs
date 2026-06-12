namespace Domain.Models.Essences;

public class EssenceLoadoutSlot
{
    public Guid Id { get; set; }
    public Guid EssenceLoadoutId { get; set; }
    public EssenceLoadout EssenceLoadout { get; set; } = null!;
    public int SlotIndex { get; set; }
    public Guid? PlayerEssenceId { get; set; }
    public PlayerEssence? PlayerEssence { get; set; }
}
