using Domain.Models.Guilds.Buildings;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL.Providers;
public class GuildBuildingUpgradeDefinitionProvider : IDisposable
{
    private readonly string _filePath;
    private readonly FileSystemWatcher _watcher;

    private volatile IReadOnlyDictionary<string, BuildingUpgradeDefinition> _cache
        = new Dictionary<string, BuildingUpgradeDefinition>();

    public GuildBuildingUpgradeDefinitionProvider()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "guild-building-upgrades.json");

        Load();

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath)!)
        {
            Filter = Path.GetFileName(_filePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += (_, __) => TryReload();
        _watcher.EnableRaisingEvents = true;
    }

    public IReadOnlyDictionary<string, BuildingUpgradeDefinition> All => _cache;

    public void Reload() => Load();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _cache = new Dictionary<string, BuildingUpgradeDefinition>();
            return;
        }

        try
        {
            using var stream = File.OpenRead(_filePath);
            var defs = JsonSerializer.Deserialize<List<BuildingUpgradeDefinition>>(stream, _jsonOptions)
                       ?? new List<BuildingUpgradeDefinition>();

            var dupes = defs.GroupBy(d => d.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupes.Any())
                throw new InvalidDataException($"Duplicate building upgrade IDs: {string.Join(',', dupes)}");

            _cache = new ConcurrentDictionary<string, BuildingUpgradeDefinition>(
                         defs.ToDictionary(d => d.Id, d => d),
                         StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // optional: log the error
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