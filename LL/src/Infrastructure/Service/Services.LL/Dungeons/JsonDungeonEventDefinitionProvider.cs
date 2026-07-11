using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Definitions.Events;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonEventDefinitionProvider : IDungeonEventDefinitionProvider
{
    private readonly IReadOnlyList<DungeonEventDefinition> _definitions;

    public JsonDungeonEventDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeons", "dungeon-events.json");
        var document = JsonSerializer.Deserialize<DungeonEventDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Events);
        _definitions = document.Events;
    }

    public IReadOnlyList<DungeonEventDefinition> GetAll() => _definitions;

    public DungeonEventDefinition GetDefinition(string dungeonDefinitionId, EventOutcomeType outcomeType)
    {
        var dungeonSpecific = _definitions
            .Where(x => x.OutcomeType == outcomeType && MatchesDungeon(x, dungeonDefinitionId))
            .OrderByDescending(x => x.DungeonDefinitionIds.Count)
            .FirstOrDefault();

        if (dungeonSpecific is not null)
        {
            return dungeonSpecific;
        }

        return _definitions.FirstOrDefault(x => x.OutcomeType == outcomeType && x.DungeonDefinitionIds.Count == 0)
            ?? _definitions.First(x => x.OutcomeType == EventOutcomeType.TreasureRoom);
    }

    private static bool MatchesDungeon(DungeonEventDefinition definition, string dungeonDefinitionId) =>
        definition.DungeonDefinitionIds.Any(id =>
            dungeonDefinitionId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
            dungeonDefinitionId.StartsWith(id + "_", StringComparison.OrdinalIgnoreCase));

    private static void ThrowIfInvalid(IReadOnlyList<DungeonEventDefinition> definitions)
    {
        var duplicates = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException("Duplicate dungeon event ids: " + string.Join(", ", duplicates));
        }

        if (definitions.Any(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name)))
        {
            throw new InvalidOperationException("Dungeon event definitions require non-empty ids and names.");
        }

        if (definitions.Any(x => x.Choices.Count == 0))
        {
            throw new InvalidOperationException("Dungeon event definitions require at least one choice.");
        }
    }

    private sealed class DungeonEventDefinitionDocument
    {
        public List<DungeonEventDefinition> Events { get; set; } = [];
    }
}
