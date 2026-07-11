using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Definitions.Routes;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonRouteDefinitionProvider : IDungeonRouteDefinitionProvider
{
    private readonly IReadOnlyList<DungeonRouteDefinition> _definitions;

    public JsonDungeonRouteDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeons", "dungeon-routes.json");
        var document = JsonSerializer.Deserialize<DungeonRouteDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Routes);
        _definitions = document.Routes;
    }

    public IReadOnlyList<DungeonRouteDefinition> GetAll() => _definitions;

    public IReadOnlyList<DungeonRouteDefinition> GetDefinitions(string dungeonDefinitionId, RoomType roomType) =>
        _definitions
            .Where(x => x.RoomType == roomType && MatchesDungeon(x, dungeonDefinitionId))
            .OrderByDescending(x => x.DungeonDefinitionIds.Count)
            .ToList();

    private static bool MatchesDungeon(DungeonRouteDefinition definition, string dungeonDefinitionId) =>
        definition.DungeonDefinitionIds.Any(id =>
            dungeonDefinitionId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
            dungeonDefinitionId.StartsWith(id + "_", StringComparison.OrdinalIgnoreCase));

    private static void ThrowIfInvalid(IReadOnlyList<DungeonRouteDefinition> definitions)
    {
        var duplicates = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException("Duplicate dungeon route ids: " + string.Join(", ", duplicates));
        }

        if (definitions.Any(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.DisplayName)))
        {
            throw new InvalidOperationException("Dungeon route definitions require non-empty ids and display names.");
        }

        if (definitions.Any(x => x.DungeonDefinitionIds.Count == 0))
        {
            throw new InvalidOperationException("Dungeon route definitions require at least one dungeonDefinitionIds entry.");
        }
    }

    private sealed class DungeonRouteDefinitionDocument
    {
        public List<DungeonRouteDefinition> Routes { get; set; } = [];
    }
}
