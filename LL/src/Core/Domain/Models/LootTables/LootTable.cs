namespace Domain.Models.LootTables;
public class LootTable : LootTableEntry
{
    public Guid Id;
    public ICollection<LootTableEntry> Entries { get; set; } = [];

    private static readonly Random RandomGenerator = new();

    public List<LootTableItem> GenerateLoot(int numberOfRolls)
    {
        var loot = new List<LootTableItem>();

        for (int i = 0; i < numberOfRolls; i++)
        {
            var selectedEntry = GetRandomEntry([.. Entries]);

            if (selectedEntry is null) continue;

            if (selectedEntry is LootTableItem item)
            {
                loot.Add(item);
            }
            else if (selectedEntry is LootTable table)
            {
                loot.AddRange(table.GenerateLoot(1));
            }
        }

        return loot;
    }

    private LootTableEntry? GetRandomEntry(List<LootTableEntry> entries)
    {
        float roll = (float)RandomGenerator.NextDouble() * 100f;
        float cumulativeWeight = 0f;

        foreach (var entry in entries)
        {
            cumulativeWeight += entry.Weight;
            if (roll <= cumulativeWeight)
            {
                return entry;
            }
        }

        return null;
    }
}