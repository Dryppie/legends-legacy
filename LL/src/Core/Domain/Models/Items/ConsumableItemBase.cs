namespace Domain.Models.Items;

public sealed class ConsumableItemBase : ItemBase
{
    public ConsumableItemBase()
    {
        ItemType = ItemType.Consumable;
    }
}
