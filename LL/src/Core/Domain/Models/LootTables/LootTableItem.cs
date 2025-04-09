using Domain.Models.Items;

namespace Domain.Models.LootTables;
public class LootTableItem : LootTableEntry
{
    public Guid ItemId { get; set; }
    public ItemBase Item { get; set; } = null!;
}