using System.Text.Json;
using Domain.Models.Items.Equipments.Progression;

namespace Services.LL.Items;

public static class JsonEquipmentBlueprintCatalog
{
    public static EquipmentBlueprintCatalog Load(string path, StarterEquipmentCatalog equipment)
    {
        var catalog = JsonSerializer.Deserialize<EquipmentBlueprintCatalog>(File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Missing equipment blueprints.");
        catalog.Validate(equipment);
        var root = Path.GetDirectoryName(Path.GetDirectoryName(path))!;
        using var items = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "items", "items.json")));
        var ids = items.RootElement.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToHashSet(StringComparer.Ordinal);
        if (catalog.Blueprints.Select(x => x.ItemId).Concat(catalog.Sources.Select(x => x.SelectionItemId)).Any(x => !ids.Contains(x)))
            throw new InvalidOperationException("A blueprint or choice container has no item definition.");
        using var dungeons = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "dungeons", "dungeons.json")));
        foreach (var source in catalog.Sources)
            if (!dungeons.RootElement.GetProperty("families").EnumerateArray().Any(x =>
                x.GetProperty("id").GetString() == source.FamilyId && x.GetProperty("region").GetInt32() == source.Region))
                throw new InvalidOperationException($"Blueprint source '{source.FamilyId}' has no matching dungeon family.");
        return catalog;
    }
}
