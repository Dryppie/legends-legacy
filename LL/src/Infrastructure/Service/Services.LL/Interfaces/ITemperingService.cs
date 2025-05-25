using Domain.Models.Professions.Crafting;

namespace Services.LL.Interfaces;
public interface ITemperingService
{
    void HandleTempering(CraftingQueueItem current, Random rng);
}