using Domain.Models.Items;

namespace Domain.Models.LootTables;
public class LootTableItem : LootTableEntry
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
}