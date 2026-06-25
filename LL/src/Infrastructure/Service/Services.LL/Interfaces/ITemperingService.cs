using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;

namespace Services.LL.Interfaces;
public interface ITemperingService
{
    bool CanTemper(CraftingQueueItem current);
    bool HandleTempering(CraftingQueueItem current, TemperingSummary temperingSummary, Random rng, Dictionary<TemperingOutcome, double> temperingBonuses);
}
