using Domain.Models.Essences;

namespace Domain.Models.Items.EssenceItems;
public class EssenceItemBase : ItemBase
{
    public Essence Essence { get; set; } = null!;
}
