using System.Collections.Concurrent;
using System.Text.Json;
using Domain.Models.Items.Equipments.Progression;

namespace Services.LL.Items;

public sealed class JsonEquipmentCatalogProvider(string starterPath) : IEquipmentCatalogProvider
{
    private readonly ConcurrentDictionary<int, StarterEquipmentCatalog> _catalogs = new();
    public IReadOnlyList<int> Versions { get; } = JsonSerializer.Deserialize<Dictionary<int, JsonStarterEquipmentCatalog.ReleaseFiles>>(
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(starterPath)!, "equipment-releases.json")),
        new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Keys.Order().ToArray();

    public StarterEquipmentCatalog Get(int version) => _catalogs.GetOrAdd(version,
        key => JsonStarterEquipmentCatalog.Load(starterPath, key));
}
