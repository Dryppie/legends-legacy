using Common.Utilities;
using Common.Utilities.EnumConverters;
using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Statuses;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL.Combat.Statuses;
public class JsonStatusService : IStatusDefinitionService, IDisposable
{
    private ImmutableDictionary<string, StatusDefinition> _cache = ImmutableDictionary<string, StatusDefinition>.Empty;
    private FileSystemWatcher? _watcher;
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new EffectConverter(),
            new EffectModificationTypeConverter(),
            new InterfaceConverterFactory(),
            new ResourceTypeConverter(),
            new CombatTargetingConverter(),
            new TriggerEventConverter(),
            new JsonStringEnumConverter(),
            new TriggerFilterConverter(),
        },
    };
    public JsonStatusService()     // inject the same options you use everywhere else
    {
        Reload();                                // initial load
        StartWatcher();
    }

    public bool TryGetById(string id, out StatusDefinition def)
        => _cache.TryGetValue(id, out def);

    public IReadOnlyCollection<StatusDefinition> GetAll() => [.. _cache.Values];

    /* ---------- private helpers ---------- */

    private void Reload()
    {
        var sw = Stopwatch.StartNew();
        var builder = ImmutableDictionary.CreateBuilder<string, StatusDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName("Data/Statuses/*.json") ?? ".",
                                                      Path.GetFileName("Data/Statuses/*.json"),
                                                      SearchOption.TopDirectoryOnly))
        {
            using var fs = File.OpenRead(path);
            var doc = JsonDocument.Parse(fs).RootElement;

            // support either a single object or an array of objects in one file
            if (doc.ValueKind == JsonValueKind.Array)
                foreach (var element in doc.EnumerateArray())
                    Add(element, path);
            else
                Add(doc, path);
        }

        // atomically swap cache
        Interlocked.Exchange(ref _cache, builder.ToImmutable());

        /* ---------- local function ---------- */
        void Add(JsonElement element, string path)
        {
            var def = element.Deserialize<StatusDefinition>(_jsonOpts)
                      ?? throw new InvalidDataException($"Could not deserialize status in {path}.");

            if (!builder.TryAdd(def.Id, def))
                throw new InvalidDataException($"Duplicate status id '{def.Id}' found in {path}.");
        }
    }

    private void StartWatcher()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Statuses", "*.json");

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath)!)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        _watcher.Changed += OnFilesChanged;
        _watcher.Created += OnFilesChanged;
        _watcher.Renamed += OnFilesChanged;
        _watcher.Deleted += OnFilesChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFilesChanged(object? s, FileSystemEventArgs e)
    {
        try
        {
            // Small debounce to avoid double-trigger on some editors
            Task.Delay(200).ContinueWith(_ => Reload());
        }
        catch (Exception ex)
        {
            //_log.LogError(ex, "Hot-reload of status definitions failed.");
        }
    }

    public void Dispose() => _watcher?.Dispose();
}
