using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Items;
using Services.LL.JsonDefinitions.Reader;

namespace Services.LL.JsonDefinitions.Dungeons;

public sealed class JsonDungeonInventoryItemCatalog : IDungeonInventoryItemCatalog
{
    private readonly HashSet<string> _itemIds;

    public JsonDungeonInventoryItemCatalog(JsonDocumentReader<DungeonCatalogDocument> reader)
    {
        _itemIds = reader.Value.Families
            .SelectMany(family => family.EntryCosts
                .Where(cost => cost.Amount > 0)
                .Select(cost => cost.ItemId)
                .Append(family.SigilItemId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Append(SigilFragmentItem.ItemBaseId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool AffectsDungeonAvailability(string itemBaseId) => _itemIds.Contains(itemBaseId);
}
