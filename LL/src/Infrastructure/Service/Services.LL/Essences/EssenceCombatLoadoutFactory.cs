using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;

namespace Services.LL.Essences;

/// <summary>Resolves an already selected loadout without accessing player persistence.</summary>
public static class EssenceCombatLoadoutFactory
{
    public static EssenceCombatLoadout Create(
        IEssenceDefinitionRepository definitions,
        Guid characterId,
        IEnumerable<PlayerEssence> equippedEssences)
    {
        var essences = equippedEssences.ToList();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var essence in essences)
        {
            var definition = definitions.GetById(essence.EssenceDefinitionId);
            if (definition is null) continue;

            foreach (var tag in definition.Tags.Concat(essence.IsEvolved ? definition.Evolution.AddsTags : []))
                tags.Add(tag);
        }

        return new EssenceCombatLoadout(characterId, essences, [], tags);
    }
}
