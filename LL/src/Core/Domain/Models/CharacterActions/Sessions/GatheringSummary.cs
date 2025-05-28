using Domain.Models.Inventories;
using Domain.Models.Professions;

namespace Domain.Models.CharacterActions.Sessions;
public class GatheringSummary
{
    public ProfessionType ProfessionType { get; set; }
    public List<InventoryItem> Loot { get; set; } = [];
    public int TotalActions { get; set; }
    public int TotalExperience { get; set; }
}
