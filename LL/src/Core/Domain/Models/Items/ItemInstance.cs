namespace Domain.Models.Items;
public class ItemInstance
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsBound => ItemBase?.IsBound == true || this is Equipments.EquipmentInstance
    {
        ProgressionData: { State.Ownership.CanTradeOrDonate: false }
    };
    public Guid Id { get; set; }
    public string ItemBaseId { get; set; } = string.Empty;
    public ItemBase ItemBase { get; set; } = null!;
    public DateTimeOffset AcquiredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string AcquisitionSource { get; set; } = ItemAcquisitionSources.Unknown;
}
