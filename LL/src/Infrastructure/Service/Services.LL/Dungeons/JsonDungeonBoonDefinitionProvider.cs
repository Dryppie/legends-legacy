using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Boons;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonBoonDefinitionProvider : IDungeonBoonDefinitionProvider
{
    private readonly IReadOnlyList<DungeonBoonDefinition> _definitions;

    public JsonDungeonBoonDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeon-boons.json");
        var document = JsonSerializer.Deserialize<DungeonBoonDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Boons);
        _definitions = document.Boons;
    }

    public IReadOnlyList<DungeonBoonDefinition> GetAll() => _definitions;

    public DungeonBoonDefinition? GetById(string boonId) =>
        _definitions.FirstOrDefault(x => x.Id.Equals(boonId, StringComparison.OrdinalIgnoreCase));

    private static void ThrowIfInvalid(IReadOnlyList<DungeonBoonDefinition> definitions)
    {
        var duplicates = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException("Duplicate dungeon boon ids: " + string.Join(", ", duplicates));
        }

        var missingIds = definitions
            .Where(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name))
            .ToList();

        if (missingIds.Count > 0)
        {
            throw new InvalidOperationException("Dungeon boon definitions require non-empty ids and names.");
        }
    }

    private sealed class DungeonBoonDefinitionDocument
    {
        public List<DungeonBoonDefinition> Boons { get; set; } = [];
    }
}
