using Domain.Models.Entities.Creatures;

namespace Domain.Models.Essences;

public static class CreatureEssenceSource
{
    public static string GetMonsterDefinitionId(Creature creature) =>
        GetMonsterDefinitionId(creature.Name);

    public static string GetMonsterDefinitionId(string creatureName) =>
        "monster." + creatureName.Trim()
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .ToLowerInvariant();
}
