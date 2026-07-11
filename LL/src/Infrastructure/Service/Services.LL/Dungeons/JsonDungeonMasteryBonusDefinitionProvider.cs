using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Mastery;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Dungeons;

public sealed class JsonDungeonMasteryBonusDefinitionProvider : IDungeonMasteryBonusDefinitionProvider
{
    private readonly IReadOnlyList<DungeonMasteryBonusDefinition> _definitions;

    public JsonDungeonMasteryBonusDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeons", "dungeon-mastery-bonuses.json");
        var document = JsonSerializer.Deserialize<DungeonMasteryBonusDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Bonuses);
        _definitions = document.Bonuses
            .OrderBy(x => x.RequiredLevel)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public IReadOnlyList<DungeonMasteryBonusDefinition> GetAll() => _definitions;

    private static void ThrowIfInvalid(IReadOnlyList<DungeonMasteryBonusDefinition> definitions)
    {
        var duplicates = definitions
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException("Duplicate dungeon mastery bonus ids: " + string.Join(", ", duplicates));
        }

        if (definitions.Any(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Description)))
        {
            throw new InvalidOperationException("Dungeon mastery bonuses require non-empty ids and descriptions.");
        }
    }

    private sealed class DungeonMasteryBonusDefinitionDocument
    {
        public List<DungeonMasteryBonusDefinition> Bonuses { get; set; } = [];
    }
}
