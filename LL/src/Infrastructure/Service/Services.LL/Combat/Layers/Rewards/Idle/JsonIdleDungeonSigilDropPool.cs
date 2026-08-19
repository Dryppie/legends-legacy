using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class JsonIdleDungeonSigilDropPool : IIdleDungeonSigilDropPool
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _additionalSigilIdsByArea;

    public JsonIdleDungeonSigilDropPool(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "dungeons", "sigil-drops.json");
        var document = JsonSerializer.Deserialize<SigilDropPoolDocument>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Dungeon sigil drop settings could not be loaded.");

        _additionalSigilIdsByArea = document.AdditionalSigilIdsByArea
            .ToDictionary(
                entry => entry.Key,
                entry => ValidateSigilIds(entry.Key, entry.Value),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> GetAdditionalSigilIds(string areaId) =>
        _additionalSigilIdsByArea.GetValueOrDefault(areaId) ?? [];

    private static IReadOnlyList<string> ValidateSigilIds(string areaId, IReadOnlyList<string> sigilIds)
    {
        if (string.IsNullOrWhiteSpace(areaId))
        {
            throw new InvalidOperationException("Dungeon sigil drop settings contain an empty area ID.");
        }

        if (sigilIds.Count == 0 || sigilIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Dungeon sigil drop settings for area '{areaId}' must contain non-empty sigil IDs.");
        }

        return sigilIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class SigilDropPoolDocument
    {
        public Dictionary<string, List<string>> AdditionalSigilIdsByArea { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
