using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Essences;

public sealed class EssenceProgressionService : IEssenceProgressionService
{
    public int GetLevelCap(int ascensionTier) =>
        EssenceProgressionConstants.GetLevelCap(ascensionTier);

    public int GetLevelCapForPotential(int potentialTier) =>
        EssenceProgressionConstants.GetLevelCapForPotential(potentialTier);

    public int GetXpRequiredForNextLevel(PlayerEssence essence, EssenceDefinition definition)
    {
        if (essence.Level >= GetLevelCapForPotential(essence.PotentialTier)) return 0;
        return EssenceProgressionConstants.GetXpRequiredForLevel(essence.Level);
    }

    public EssenceXpGrantResult GrantXp(PlayerEssence essence, EssenceDefinition definition, int requestedXp)
    {
        if (requestedXp <= 0) return new(0, 0, essence.Level >= GetLevelCapForPotential(essence.PotentialTier));

        var remaining = requestedXp;
        var gained = 0;
        var levels = 0;
        var cap = GetLevelCapForPotential(essence.PotentialTier);

        while (remaining > 0 && essence.Level < cap)
        {
            var required = GetXpRequiredForNextLevel(essence, definition);
            var needed = required - essence.CurrentXp;
            var applied = Math.Min(remaining, needed);
            essence.CurrentXp += applied;
            remaining -= applied;
            gained += applied;

            if (essence.CurrentXp < required) break;

            essence.Level++;
            levels++;
            essence.CurrentXp = 0;
        }

        if (essence.Level >= cap) essence.CurrentXp = 0;
        essence.UpdatedAt = DateTimeOffset.UtcNow;
        return new(gained, levels, essence.Level >= cap);
    }
}
