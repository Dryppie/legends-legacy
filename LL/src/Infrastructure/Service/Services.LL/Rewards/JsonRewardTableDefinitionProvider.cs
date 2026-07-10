using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Rewards;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Rewards;

public sealed class JsonRewardTableDefinitionProvider : IRewardTableDefinitionProvider
{
    private readonly IReadOnlyList<RewardTableDefinition> _definitions;
    private readonly IReadOnlyDictionary<string, RewardTableDefinition> _byId;

    public JsonRewardTableDefinitionProvider(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options,
        IRewardTableDefinitionValidator validator)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "reward-tables.json");
        var document = JsonSerializer.Deserialize<RewardTableDefinitionDocument>(
            File.ReadAllText(path),
            options) ?? new();

        validator.ThrowIfInvalid(document.RewardTables, LoadItemIds(Path.Combine(contentRootPath, contentRoot, "items.json")));
        _definitions = document.RewardTables;
        _byId = _definitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public RewardTableDefinition GetById(string id)
    {
        if (_byId.TryGetValue(id, out var definition))
            return definition;

        throw new KeyNotFoundException($"Reward table '{id}' was not found.");
    }

    public RewardTableDefinition? FindById(string id) =>
        _byId.TryGetValue(id, out var definition) ? definition : null;

    public IReadOnlyList<RewardTableDefinition> GetAll() => _definitions;

    private static IReadOnlySet<string> LoadItemIds(string path)
    {
        if (!File.Exists(path))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .EnumerateArray()
            .Where(item => item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            .Select(item => item.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class RewardTableDefinitionDocument
    {
        public List<RewardTableDefinition> RewardTables { get; set; } = [];
    }
}
