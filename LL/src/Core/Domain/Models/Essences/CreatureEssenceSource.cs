using Domain.Models.Entities.Creatures;

namespace Domain.Models.Essences;

public static class CreatureEssenceSource
{
    public static string GetMonsterDefinitionId(Creature creature) =>
        "monster." + creature.Name.Trim()
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToLowerInvariant();
}
