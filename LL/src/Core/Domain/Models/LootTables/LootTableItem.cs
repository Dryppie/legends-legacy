using Domain.Models.Items;

namespace Domain.Models.LootTables;
public class LootTableItem : LootTableEntry
{
    public string ItemId { get; set; } = string.Empty;
    public ItemBase Item { get; set; } = null!;
}