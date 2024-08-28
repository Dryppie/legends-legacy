using Domain.Models.Items;

namespace Domain.Models.LootTables;
public class LootTable
{
    public Guid Id;
    public ICollection<Item> Items { get; set; } = [];
}