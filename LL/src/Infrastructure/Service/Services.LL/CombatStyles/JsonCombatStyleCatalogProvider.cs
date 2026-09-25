using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.CombatStyles;
using System.Text.Json;
using System.Text.Json.Serialization;
using Services.LL.Content;

namespace Services.LL.CombatStyles;

public sealed class JsonCombatStyleCatalogProvider : ICombatStyleCatalogProvider
{
    public CombatStyleCatalog Catalog { get; }
    public JsonCombatStyleCatalogProvider(string path, ContentJsonReader? reader = null)
    {
        reader ??= ContentJsonReader.Default;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        Catalog = reader.Deserialize<CombatStyleCatalog>(reader.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Combat Style content is empty.");
        CombatStyleRules.ValidateCatalog(Catalog);
    }
}
