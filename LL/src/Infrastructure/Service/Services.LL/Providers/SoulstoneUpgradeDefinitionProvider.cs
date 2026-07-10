using Domain.Models.Soulstones.UpgradeDefinition;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL.Providers;

public sealed class SoulstoneUpgradeDefinitionProvider : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly FileSystemWatcher _watcher;
    private volatile IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> _cache
        = new Dictionary<string, SoulstoneUpgradeDefinition>(StringComparer.OrdinalIgnoreCase);

    public SoulstoneUpgradeDefinitionProvider()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "soulstone-upgrades.json");
        Load();

        var directory = Path.GetDirectoryName(_filePath);
        if (directory is null)
        {
            throw new InvalidOperationException("Soulstone upgrade definition path is invalid.");
        }

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = Path.GetFileName(_filePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += (_, _) => TryReload();
        _watcher.EnableRaisingEvents = true;
    }

    public IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> All => _cache;

    public void Reload() => Load();

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Soulstone upgrade definition file was not found.", _filePath);
        }

        using var stream = File.OpenRead(_filePath);
        var defs = JsonSerializer.Deserialize<List<SoulstoneUpgradeDefinition>>(stream, JsonOptions)
                   ?? throw new InvalidDataException("Soulstone upgrade definition file was empty.");

        Validate(defs);

        _cache = new ConcurrentDictionary<string, SoulstoneUpgradeDefinition>(
            defs.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void Validate(IReadOnlyList<SoulstoneUpgradeDefinition> defs)
    {
        var dupes = defs
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (dupes.Count != 0)
        {
            throw new InvalidDataException($"Duplicate Soulstone upgrade IDs: {string.Join(", ", dupes)}");
        }

        foreach (var def in defs)
        {
            if (string.IsNullOrWhiteSpace(def.Id))
                throw new InvalidDataException("Soulstone upgrade ID is required.");
            if (string.IsNullOrWhiteSpace(def.DisplayName))
                throw new InvalidDataException($"Soulstone upgrade '{def.Id}' is missing displayName.");
            if (def.MaxRank < 1)
                throw new InvalidDataException($"Soulstone upgrade '{def.Id}' must have maxRank >= 1.");
            if (def.CostsByRank.Count != def.MaxRank)
                throw new InvalidDataException($"Soulstone upgrade '{def.Id}' must define one cost per rank.");
            if (def.CostsByRank.Any(cost => cost < 0))
                throw new InvalidDataException($"Soulstone upgrade '{def.Id}' has a negative cost.");

            foreach (var effect in def.Effects)
            {
                if (effect.ValuesByRank.Count != def.MaxRank)
                    throw new InvalidDataException($"Soulstone upgrade '{def.Id}' effect '{effect.Kind}' must define one value per rank.");
            }
        }
    }

    private void TryReload()
    {
        Task.Delay(250).ContinueWith(_ => Load());
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
