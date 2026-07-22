using Domain.Models.Dungeons;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Application.Interfaces.Services.LL.Dungeons;

namespace Services.LL.JsonDefinitions;

public sealed class JsonDungeonDefinitions : IDungeonDefinitions
{
    private static readonly HashSet<string> RetiredDungeonIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "hives_abyss",
        "hives_abyss_ii",
        "hives_abyss_iii"
    };

    private readonly Dictionary<string, DungeonDefinition> _byId;

    public JsonDungeonDefinitions(
        JsonDocumentReader<DungeonCatalogDocument> reader,
        DungeonDefinitionMaterializer materializer,
        IDungeonDefinitionValidator validator)
    {
        var definitions = materializer.Materialize(reader.Value);
        validator.ThrowIfInvalid(definitions);
        _byId = definitions
            .Where(d => !RetiredDungeonIds.Contains(d.Id))
            .ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
    }

    public DungeonDefinition GetByKey(string id)
        => _byId[id];

    public IReadOnlyList<DungeonDefinition> GetAll()
        => _byId.Values.ToList();
}
