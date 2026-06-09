using Domain.Models.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Application.Interfaces.Services.LL.Dungeons;

namespace Services.LL.JsonDefinitions;

public sealed class JsonDungeonDefinitions : IDungeonDefinitions
{
    private readonly Dictionary<string, DungeonDefinition> _byId;

    public JsonDungeonDefinitions(JsonDefinitionReader<DungeonDefinition> reader)
    {
        _byId = reader.All.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
    }

    public DungeonDefinition GetByKey(string id)
        => _byId[id];

    public IReadOnlyList<DungeonDefinition> GetAll()
        => _byId.Values.ToList();
}
