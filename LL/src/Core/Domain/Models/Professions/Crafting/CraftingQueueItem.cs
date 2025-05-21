using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting;
public class CraftingQueueItem
{
    public Guid Id { get; set; }
    public byte QueueIndex { get; set; }
    public CraftingMode Mode { get; set; }
    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
    public Guid? ItemInstanceId { get; set; }
    public ItemInstance? ItemInstance { get; set; }
}