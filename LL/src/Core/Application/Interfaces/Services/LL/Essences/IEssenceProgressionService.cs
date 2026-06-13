using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Essences;

public interface IEssenceProgressionService
{
    int GetLevelCap(int ascensionTier);
    int GetXpRequiredForNextLevel(PlayerEssence essence, EssenceDefinition definition);
    EssenceXpGrantResult GrantXp(PlayerEssence essence, EssenceDefinition definition, int requestedXp);
}

public sealed record EssenceXpGrantResult(int XpGained, int LevelsGained, bool ReachedTierCap);
