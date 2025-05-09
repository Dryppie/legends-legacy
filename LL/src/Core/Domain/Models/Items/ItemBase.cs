using System.Text.Json.Serialization;
using Domain.Models.LootTables;

namespace Domain.Models.Items;
public class ItemBase
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
    public Rarity Rarity { get; set; }
    [JsonIgnore]
    public ICollection<ItemInstance> ItemInstances { get; set; } = [];
    [JsonIgnore]
    public ICollection<LootTableItem> LootTablesItems { get; set; } = [];
}