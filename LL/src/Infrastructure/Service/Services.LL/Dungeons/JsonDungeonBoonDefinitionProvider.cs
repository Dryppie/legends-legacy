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

        var invalidStacks = definitions
            .Where(x => x.MaxStacks <= 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidStacks.Count > 0)
        {
            throw new InvalidOperationException("Dungeon boon definitions require MaxStacks greater than zero: " + string.Join(", ", invalidStacks));
        }

        var invalidTiers = definitions
            .Where(x => x.Tier <= 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidTiers.Count > 0)
        {
            throw new InvalidOperationException("Dungeon boon definitions require Tier greater than zero: " + string.Join(", ", invalidTiers));
        }

        var invalidFamilyStacks = definitions
            .Where(x => x.MaxFamilyStacks < 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidFamilyStacks.Count > 0)
        {
            throw new InvalidOperationException("Dungeon boon definitions require MaxFamilyStacks to be zero or greater: " + string.Join(", ", invalidFamilyStacks));
        }

        var inconsistentFamilyStackCaps = definitions
            .GroupBy(GetFamilyId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                FamilyId = group.Key,
                Caps = group.Select(x => x.MaxFamilyStacks).Where(x => x > 0).Distinct().ToList()
            })
            .Where(x => x.Caps.Count > 1)
            .Select(x => x.FamilyId)
            .ToList();

        if (inconsistentFamilyStackCaps.Count > 0)
        {
            throw new InvalidOperationException("Dungeon boon family variants must use the same MaxFamilyStacks value: " + string.Join(", ", inconsistentFamilyStackCaps));
        }
    }

    private static string GetFamilyId(DungeonBoonDefinition definition) =>
        string.IsNullOrWhiteSpace(definition.FamilyId)
            ? definition.Id
            : definition.FamilyId;

    private sealed class DungeonBoonDefinitionDocument
    {
        public List<DungeonBoonDefinition> Boons { get; set; } = [];
    }
}
