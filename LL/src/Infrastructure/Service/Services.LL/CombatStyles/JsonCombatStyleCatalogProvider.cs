using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.CombatStyles;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL.CombatStyles;

public sealed class JsonCombatStyleCatalogProvider : ICombatStyleCatalogProvider
{
    public CombatStyleCatalog Catalog { get; }
    public JsonCombatStyleCatalogProvider(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        Catalog = JsonSerializer.Deserialize<CombatStyleCatalog>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Combat Style content is empty.");
        CombatStyleRules.ValidateCatalog(Catalog);
    }
}
