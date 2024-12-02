using Domain.Models.Essences;

namespace Domain.Models.Items;
public class EssenceItem : Item
{
    public Guid EssenceId { get; set; }
    public Essence Essence { get; set; } = null!;
}
