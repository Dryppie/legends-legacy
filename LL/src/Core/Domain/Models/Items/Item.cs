using Domain.Models.LootTables;

namespace Domain.Models.Items;
public class Item
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    public ICollection<LootTableItem> LootTablesItems { get; set; } = [];
}