using Domain.Models.LootTables;

namespace Domain.Models.CharacterActions.CharacterActionDetails;
public class GatheringActionDetails : ActionDetails
{
    public LootTable LootTable { get; set; } = new();
}
