namespace Domain.Models.Items.EssenceItems;
public class EssenceItemBase : ItemBase
{
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public int DismantleDustAmount { get; set; } = 1;
}
