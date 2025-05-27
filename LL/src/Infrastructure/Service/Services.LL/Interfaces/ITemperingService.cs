using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Professions.Crafting;

namespace Services.LL.Interfaces;
public interface ITemperingService
{
    void HandleTempering(CraftingQueueItem current, TemperingSummary temperingSummary, Random rng);
}