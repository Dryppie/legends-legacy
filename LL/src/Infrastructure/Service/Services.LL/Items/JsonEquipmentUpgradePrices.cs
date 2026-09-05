using System.Text.Json;
using Domain.Models.Items.Equipments.Progression;

namespace Services.LL.Items;

public static class JsonEquipmentUpgradePrices
{
    public static EquipmentUpgradePrices Load(string path) =>
        JsonSerializer.Deserialize<EquipmentUpgradePrices>(
            File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Missing equipment-upgrade prices.");
}
