namespace Domain.Models.Items;
public class ItemInstance
{
    public Guid Id { get; set; }
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBase ItemBase { get; set; } = null!;
}