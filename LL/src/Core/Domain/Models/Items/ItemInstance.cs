namespace Domain.Models.Items;
public class ItemInstance
{
    public Guid Id { get; set; }
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBase ItemBase { get; set; } = null!;
    public DateTimeOffset AcquiredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string AcquisitionSource { get; set; } = ItemAcquisitionSources.Unknown;
}
