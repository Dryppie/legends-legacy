using System.Text.Json;
using Domain.Models.Items.Equipments.Progression;

namespace Services.LL.Items;

public static class JsonTowerEquipmentSupplyCatalog
{
    public static TowerEquipmentSupplyCatalog Load(string path, EquipmentCatalog equipment)
    {
        var supplies = JsonSerializer.Deserialize<TowerEquipmentSupply[]>(File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Missing Tower equipment supplies.");
        return new(equipment, supplies);
    }
}
