using Common.Helpers.JsonFiles;
using Domain.Models.Soulstones.UpgradeDefinition;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.UseCases.Soulstones.Providers;
public class SoulstoneUpgradeDefinitionProvider
{
    private readonly string _filePath;
    private readonly FileSystemWatcher _watcher;

    // concurrent dictionary so readers are never blocked
    private volatile IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> _cache
        = new Dictionary<string, SoulstoneUpgradeDefinition>();

    public SoulstoneUpgradeDefinitionProvider(JsonFileResolver resolver)
    {
        _filePath = resolver.Resolve("soulstone-upgrades.json");

        Load();

        // hot-reload: watch the directory for changes to upgrades.json
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath)!)
        {
            Filter = Path.GetFileName(_filePath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += (_, __) => TryReload();
        _watcher.EnableRaisingEvents = true;
    }

    /// All definitions keyed by Id – always the *latest* snapshot.
    public IReadOnlyDictionary<string, SoulstoneUpgradeDefinition> All => _cache;

    /// Reload manually (unit tests or admin endpoint).
    public void Reload() => Load();

    /* ----------------------------------------------------------------- */

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
            _cache = new Dictionary<string, SoulstoneUpgradeDefinition>();
            return;
        }

        try
        {
            using var stream = File.OpenRead(_filePath);
            var defs = JsonSerializer.Deserialize<List<SoulstoneUpgradeDefinition>>(stream, _jsonOptions)
                       ?? new List<SoulstoneUpgradeDefinition>();

            // basic sanity check: duplicate Ids?
            var dupes = defs.GroupBy(d => d.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupes.Any())
                throw new InvalidDataException($"Duplicate upgrade IDs: {string.Join(',', dupes)}");

            _cache = new ConcurrentDictionary<string, SoulstoneUpgradeDefinition>(
                         defs.ToDictionary(d => d.Id, d => d),
                         StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            // keep old cache so the game can still run
        }
    }

    private void TryReload()
    {
        // debounce rapid successive change events (optional; simple timer here)
        Task.Delay(250).ContinueWith(_ => Load());
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
