namespace Domain.Models.LootTables;
public class LootTable : LootTableEntry
{
    public ICollection<LootTableEntry> Entries { get; set; } = [];
}