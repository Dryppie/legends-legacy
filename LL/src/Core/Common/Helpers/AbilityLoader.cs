using Common.Utilities;
using Domain.Models.Abilities;
using Domain.Models.Entities;
using System.Text.Json;

namespace Domain.Helpers;
public static class AbilityLoader
{
    public static async Task LoadAbilitiesForEntity(Entity entity)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");

        string json = await File.ReadAllTextAsync(filePath);

        // Deserialize JSON into a list of abilities
        List<Ability> abilities = JsonSerializer.Deserialize<List<Ability>>(json, AbilityJsonReader.Options)!;
        var ids = entity.AbilityIds;

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

}