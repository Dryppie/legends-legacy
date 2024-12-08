using Common.Utilities;
using Domain.Models.Abilities;
using Domain.Models.Entities;
using Domain.Models.Essences;
using System.Text.Json;

namespace Domain.Helpers;
public static class AbilityLoader
{
    public static async Task LoadAbilitiesForEntity(Entity entity)
    {
        // Deserialize JSON into a list of abilities
        List<Ability> abilities = await DeserializeAbilities();

        var ids = new List<string>();
        foreach (var essence in entity.EquippedEssences)
        {
            ids.Add(essence.PassiveAbilityId);
            ids.Add(essence.ActiveAbilityId);
        }

        var entityAbilities = abilities.Where(a => ids.Contains(a.Id)).ToList();
        entity.Abilities = entityAbilities;
        foreach (var ability in entity.Abilities)
        {
            ability.RemainingTimeUntilUse = ability.Cooldown;
            if (ability.Type.Equals(AbilityType.Passive))
            {
                foreach(var effect in ability.Effects)
                {
                    entity.ActiveEffects.Add(effect);
                }
            }
        }

    }

    public static async Task LoadAbilitiesForEssence(Essence essence)
    {
        // Deserialize JSON into a list of abilities
        List<Ability> abilities = await DeserializeAbilities();

        essence.ActiveAbility = abilities.FirstOrDefault(a => a.Id.Equals(essence.ActiveAbilityId))!;
        essence.PassiveAbility = abilities.FirstOrDefault(a => a.Id.Equals(essence.PassiveAbilityId))!;
    }

    /// <summary>
    /// Every increase in 'tier' increases the amount of abilities selected for an entity
    /// </summary>
    /// <param name="tier"></param>
    /// <returns></returns>
    public static async Task<List<Ability>> _Simulator_PickRandomAbilityCombinations(int tier = 1)
    {
        
        // Get all abilities from your data source
        List<Ability> allAbilities = await DeserializeAbilities();

        // Separate abilities by type
        var passiveAbilities = allAbilities.Where(a => a.Type == AbilityType.Passive).ToList();
        var activeAbilities = allAbilities.Where(a => a.Type == AbilityType.Active).ToList();

        var rand = new Random();

        // Determine how many combinations (1 to 3)
        int combinationCount = rand.Next(1, tier);

        var chosenAbilities = new List<Ability>();

        for (int i = 0; i < combinationCount; i++)
        {
            // If we run out of either passives or actives, we can't form more combos
            if (!passiveAbilities.Any() || !activeAbilities.Any())
                break;

            // Randomly pick one passive
            int pIndex = rand.Next(passiveAbilities.Count);
            var chosenPassive = passiveAbilities[pIndex];
            passiveAbilities.RemoveAt(pIndex);

            // Randomly pick one active
            int aIndex = rand.Next(activeAbilities.Count);
            var chosenActive = activeAbilities[aIndex];
            activeAbilities.RemoveAt(aIndex);

            // Add both to the chosen list
            chosenAbilities.Add(chosenPassive);
            chosenAbilities.Add(chosenActive);
        }

        return chosenAbilities;
    }


    public static async Task<List<Ability>> DeserializeAbilities()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");

        string json = await File.ReadAllTextAsync(filePath);

        // Deserialize JSON into a list of abilities
        return JsonSerializer.Deserialize<List<Ability>>(json, AbilityJsonReader.Options)!;
    }

}