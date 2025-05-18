using Domain.Models.LootTables;
using Domain.Models.Professions;

namespace Domain.Models.CharacterActions.CharacterActionDetails;
public class GatheringActionDetails : ActionDetails
{
    public string Name { get; set; } = string.Empty;
    public ProfessionType ProfessionType { get; set; }
    public Guid LootTableId { get; set; }
    public LootTable? LootTable { get; set; }
}
