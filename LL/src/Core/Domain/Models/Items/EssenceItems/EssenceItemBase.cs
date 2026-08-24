namespace Domain.Models.Items.EssenceItems;
public class EssenceItemBase : ItemBase
{
    private const string ItemIdPrefix = "item.";
    private const string ConventionalItemIdPrefix = "item.essence.";

    public string EssenceDefinitionId { get; set; } = string.Empty;
    public int DismantleDustAmount { get; set; } = 1;

    public string ResolveDefinitionId() => ResolveDefinitionId(Id, EssenceDefinitionId);

    public static string ResolveDefinitionId(string? itemBaseId, string? explicitDefinitionId)
    {
        if (!string.IsNullOrWhiteSpace(explicitDefinitionId))
            return explicitDefinitionId.Trim();

        return !string.IsNullOrWhiteSpace(itemBaseId) &&
               itemBaseId.StartsWith(ConventionalItemIdPrefix, StringComparison.OrdinalIgnoreCase)
            ? itemBaseId[ItemIdPrefix.Length..]
            : string.Empty;
    }
}
