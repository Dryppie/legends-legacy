using Domain.Models.LootTables;

namespace Domain.Models.Items;
public class Item
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<LootTable> LootTables { get; set; } = [];
}