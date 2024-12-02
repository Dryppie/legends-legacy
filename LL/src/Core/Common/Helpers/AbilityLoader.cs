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

    public static async Task<List<Ability>> DeserializeAbilities()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");

        string json = await File.ReadAllTextAsync(filePath);

        // Deserialize JSON into a list of abilities
        return JsonSerializer.Deserialize<List<Ability>>(json, AbilityJsonReader.Options)!;
    }

}