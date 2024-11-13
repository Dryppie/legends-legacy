namespace Domain.Models.LootTables;
public abstract class LootTableEntry
{
    public Guid Id { get; set; }
    public float Weight { get; set; }
}