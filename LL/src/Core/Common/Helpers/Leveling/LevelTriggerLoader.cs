using Domain.Components.Leveling;
using System.Text.Json;

namespace Common.Helpers.Leveling;
public sealed class LevelTriggerLoader
{
    private static readonly object _lock = new object();
    private static LevelTriggerLoader? _instance;

    private readonly List<LevelTrigger> _levelTriggers;

    private LevelTriggerLoader()
    {
        //_levelTriggers = LoadLevelTriggersFromJson();
    }

    public static LevelTriggerLoader Instance
    {
        get
        {
            // Double-checked locking for thread safety
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new LevelTriggerLoader();
                    }
                }
            }
            return _instance;
        }
    }

    public List<LevelTrigger> GetLevelTriggers()
    {
        return _levelTriggers;
    }

    /// <summary>
    /// Reads and deserializes LevelTriggers from JSON, once only.
    /// </summary>
    //private List<LevelTrigger> LoadLevelTriggersFromJson()
    //{
    //    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "levelTriggers.json");
    //    string json = File.ReadAllText(filePath);

    //    // Deserialize JSON into a list of levelTriggers
    //    return JsonSerializer.Deserialize<List<LevelTrigger>>(json, LevelTriggerJsonReader.Options)!;
    //}
}
