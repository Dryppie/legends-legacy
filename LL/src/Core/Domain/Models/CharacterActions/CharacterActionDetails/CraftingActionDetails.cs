using Domain.Models.Professions.Crafting;

namespace Domain.Models.CharacterActions.CharacterActionDetails;
public class CraftingActionDetails : ActionDetails
{
    public ICollection<CraftingQueueItem> CraftingQueueItems { get; set; } = [];
}