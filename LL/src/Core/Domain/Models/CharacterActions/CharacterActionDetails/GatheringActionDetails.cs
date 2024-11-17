using Domain.Models.GatheringNodes;
using Domain.Models.LootTables;

namespace Domain.Models.CharacterActions.CharacterActionDetails;
public class GatheringActionDetails : ActionDetails
{
    public string Name { get; set; } = string.Empty;
    public GatheringType GatheringType { get; set; }
    public Guid LootTableId { get; set; }
    public LootTable LootTable { get; set; } = new();
}
