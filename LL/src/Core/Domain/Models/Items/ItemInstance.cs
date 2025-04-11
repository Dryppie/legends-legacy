namespace Domain.Models.Items;
public class ItemInstance
{
    public Guid Id { get; set; }
    public Guid ItemBaseId { get; set; }
    public ItemBase ItemBase { get; set; } = null!;
}