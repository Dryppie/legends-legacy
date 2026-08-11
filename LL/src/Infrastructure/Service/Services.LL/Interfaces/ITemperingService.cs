using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;
using Application.Interfaces.Services.LL.Professions;

namespace Services.LL.Interfaces;
public interface ITemperingService
{
    bool CanTemper(CraftingQueueItem current);
    TemperingAttemptResult? HandleTempering(
        CraftingQueueItem current,
        TemperingSummary temperingSummary,
        Random rng,
        double craftingExperienceGainBps,
        double negativeOutcomeReductionBps);
}
