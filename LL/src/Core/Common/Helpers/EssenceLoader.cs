using Common.Utilities;
using Domain.Models.Entities;
using Domain.Models.Essences;
using System.Text.Json;

namespace Domain.Helpers;
public static class EssenceLoader
{
    public static async Task LoadEssencesForEntity(Entity entity)
    {
        // Deserialize JSON into a list of essences
        List<Essence> essences = await DeserializeEssences();

        var ids = new List<string>();
        foreach (var essence in entity.EquippedEssences)
        {
            ids.Add(essence.Name);
        }

        var entityEssences = essences.Where(a => ids.Contains(a.Name)).ToList();
        foreach (var entityEssence in entityEssences)
        {
            entity.Abilities.Add(entityEssence.Active);
            entity.Abilities.Add(entityEssence.Passive);
        }
    }

    public static async Task LoadAbilitiesForEssence(Essence essence)
    {
        // Deserialize JSON into a list of essences
        List<Essence> essences = await DeserializeEssences();

        essence = essences.FirstOrDefault(e => e.Name.Equals(essence.Name))!;
    }

    private static readonly Random _rand = new Random();

    /// <summary>
    /// Every increase in 'tier' increases the amount of abilities selected for an entity
    /// </summary>
    /// <param name="tier"></param>
    /// <returns></returns>
    public static async Task _Simulator_PickRandomAbilityCombinations(Entity entity, int tier = 1)
    {
        List<Essence> allEssences = await DeserializeEssences();

        var chosenEssences = new List<Essence>();

        for (int i = 0; i < tier; i++)
        {
            // If we run out of either passives or actives, we can't form more combos
            if (!allEssences.Any())
                break;

            // Randomly pick one passive
            int pIndex = _rand.Next(allEssences.Count);
            var chosenEssence = allEssences[pIndex];
            allEssences.RemoveAt(pIndex);

            // Add both to the chosen list
            chosenEssences.Add(chosenEssence);
        }
        foreach (var essence in chosenEssences)
        {
            entity.EquippedEssences.Add(essence);
            entity.Abilities.Add(essence.Active);
            entity.Abilities.Add(essence.Passive);
        }
    }

    public static async Task _Simulator_PickSpecificAbility(Entity entity, string essenceName)
    {
        List<Essence> allEssences = await DeserializeEssences();

        var chosenEssences = new List<Essence>();

        var chosenEssence = allEssences.FirstOrDefault(e => e.Name.Equals(essenceName));

        entity.EquippedEssences.Add(chosenEssence!);
        entity.Abilities.Add(chosenEssence!.Active);
        entity.Abilities.Add(chosenEssence!.Passive);
    }

    public static async Task<List<Essence>> DeserializeEssences()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");

        string json = await File.ReadAllTextAsync(filePath);

        // Deserialize JSON into a list of essences
        return JsonSerializer.Deserialize<List<Essence>>(json, AbilityJsonReader.Options)!;
    }
}