using Domain.Models.LootTables;

namespace Domain.Models.Entities.Creatures;
public class Creature : Entity
{
    public Guid LootTableId { get; set; }
    public LootTable LootTable { get; set; } = null!;
}