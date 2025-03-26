using System.Text.Json;
using Common.Utilities;
using Domain.Extensions;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Essences;

namespace Common.Helpers.Essences;
// TODO: Make it such that whenever I edit the json file, it'll trigger an endpoint that causes this to reload all the essences.
public sealed class EssenceLoader
{
    private static readonly object _lock = new object();
    private static EssenceLoader? _instance;

    private readonly List<Essence> _essences;
    private readonly Random _rand = new Random();

    /// <summary>
    /// Private constructor so no one can instantiate this class from the outside.
    /// </summary>
    private EssenceLoader()
    {
        _essences = LoadEssencesFromJson();
    }

    /// <summary>
    /// The global access point to this class. Ensures only one instance exists (Singleton).
    /// </summary>
    public static EssenceLoader Instance
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
                        _instance = new EssenceLoader();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Reads and deserializes Essences from JSON, once only.
    /// </summary>
    private List<Essence> LoadEssencesFromJson()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");
        string json = File.ReadAllText(filePath);

        // Deserialize JSON into a list of essences
        return JsonSerializer.Deserialize<List<Essence>>(json, EssenceJsonReader.Options)!;
    }

    public List<Essence> GetEssences()
    {
        return _essences;
    }

    /// <summary>
    /// Load all Essences that an Entity has equipped and add them to the Entity's ability list.
    /// </summary>
    public void LoadEssencesForEntity(Entity entity)
    {
        var ids = entity.EssenceSlots.ActiveSlotsWithEssences().Select(es => es.OccupiedEssence!).Select(e => e.Name).ToList();
        var entityEssences = _essences.Where(a => ids.Contains(a.Name)).ToList();

        foreach (var essence in entityEssences)
        {
            SetSourceIdForEffects(essence);
            entity.Abilities.Add(essence.Active.Clone());
            entity.Abilities.Add(essence.Passive.Clone());
        }
    }

    /// <summary>
    /// Load all Essences that a CombatEntity has equipped and add them to its ability list.
    /// </summary>
    public void LoadEssencesForCombatEntity(CombatEntity entity)
    {
        var ids = entity.EquippedEssences.Select(e => e.Name).ToList();
        var entityEssences = _essences.Where(a => ids.Contains(a.Name)).ToList();

        foreach (var essence in entityEssences)
        {
            SetSourceIdForEffects(essence);
            entity.Abilities.Add(essence.Active.Clone());
            entity.Abilities.Add(essence.Passive.Clone());
        }
    }

    /// <summary>
    /// Given an Essence (by name), return the "full" essence (with Active and Passive abilities loaded).
    /// </summary>
    public void LoadAbilitiesForEssence(Essence essence)
    {
        var essenceFromMemory = _essences.FirstOrDefault(e => e.Name.Equals(essence.Name, StringComparison.OrdinalIgnoreCase))!;

        if (essenceFromMemory == null) return;

        essence.Active = essenceFromMemory.Active;
        essence.Passive = essenceFromMemory.Passive;
    }

    /// <summary>
    /// A method for randomly picking tier-based Essences. 
    /// </summary>
    public void _Simulator_PickRandomAbilityCombinations(CombatEntity entity, int tier = 1)
    {
        // We'll work with a copy of all essences so we don't disturb the original list
        var allEssences = _essences.ToList();
        var chosenEssences = new List<Essence>();

        for (int i = 0; i < tier; i++)
        {
            if (allEssences.Count == 0)
                break;

            int pIndex = _rand.Next(allEssences.Count);
            var chosenEssence = allEssences[pIndex];
            allEssences.RemoveAt(pIndex);

            chosenEssences.Add(chosenEssence);
        }

        foreach (var essence in chosenEssences)
        {
            SetSourceIdForEffects(essence);
            entity.EquippedEssences.Add(essence);
            entity.Abilities.Add(essence.Active.Clone());
            entity.Abilities.Add(essence.Passive.Clone());
        }
    }

    /// <summary>
    /// A helper method to pick a specific Essence by name and attach it to a CombatEntity.
    /// </summary>
    public void _Simulator_PickSpecificAbility(CombatEntity entity, string essenceName)
    {
        var chosenEssence = _essences.FirstOrDefault(e => e.Name.Equals(essenceName, StringComparison.OrdinalIgnoreCase));
        if (chosenEssence != null)
        {
            SetSourceIdForEffects(chosenEssence);
            entity.EquippedEssences.Add(chosenEssence);
            entity.Abilities.Add(chosenEssence.Active.Clone());
            entity.Abilities.Add(chosenEssence.Passive.Clone());
        }
    }

    /// <summary>
    /// Helper method to set source IDs for effects. 
    /// </summary>
    private static void SetSourceIdForEffects(Essence essence)
    {
        for (int i = 0; i < essence.Active.Effects.Count; i++)
        {
            essence.Active.Effects[i].SourceId = $"{essence.Active.Id}_{i}";
        }

        for (int i = 0; i < essence.Passive.Effects.Count; i++)
        {
            essence.Passive.Effects[i].SourceId = $"{essence.Passive.Id}_{i}";
        }
    }
}