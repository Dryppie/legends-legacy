using Common.Utilities;
using Domain.Extensions;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Essences;
using System.Text.Json;

namespace Common.Helpers.Essences;
// TODO: Make it such that whenever I edit the json file, it'll trigger an endpoint that causes this to reload all the essences.
public sealed class EssenceLoader
{
    private static readonly object _lock = new();
    private static EssenceLoader? _instance;

    private readonly List<Essence> _essences;
    private readonly Random _rand = new();

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
                    _instance ??= new EssenceLoader();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Reads and deserializes Essences from JSON, once only.
    /// </summary>
    private static List<Essence> LoadEssencesFromJson()
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
        var ids = entity.EssenceSlots.ActiveSlotsWithOccupiedEssences().Select(es => es.OccupiedEssence!).Select(e => e.Name).ToList();
        var entityEssences = _essences.Where(a => ids.Contains(a.Name)).ToList();

        foreach (var essence in entityEssences)
        {
            SetSourceIdForEffects(essence);
            entity.Abilities.Add(new (essence.Active));
            entity.Abilities.Add(new (essence.Passive));
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
            entity.Abilities.Add(new(essence.Active));
            entity.Abilities.Add(new(essence.Passive));
        }
    }

    /// <summary>
    /// Given an Essence (by name), return the "full" essence (with Active and Passive abilities loaded).
    /// </summary>
    public void LoadAbilitiesForEssence(Essence essence)
    {
        var essenceFromMemory = _essences.FirstOrDefault(e => e.Name.Equals(essence.Name, StringComparison.OrdinalIgnoreCase))!;

        if (essenceFromMemory == null) return;

        essence.AttributeModifiers = essenceFromMemory.AttributeModifiers;
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
            entity.Abilities.Add(new(essence.Active));
            entity.Abilities.Add(new(essence.Passive));
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
            entity.Abilities.Add(new (chosenEssence.Active));
            entity.Abilities.Add(new (chosenEssence.Passive));
        }
    }

    private static void SetSourceIdForEffects(Essence essence)
    {
        // Helper local so we don’t repeat ourselves.
        void MarkAbility(AbilityDefinition? ability)
        {
            if (ability is null) return;
            foreach (var trigger in ability.Triggers)
            {
                foreach (var effect in trigger.Actions)
                {
                    effect.SourceName = ability.Name;   // or essence.Id.ToString()
                }
            }
        }

        MarkAbility(essence.Passive);
        MarkAbility(essence.Active);
    }
}