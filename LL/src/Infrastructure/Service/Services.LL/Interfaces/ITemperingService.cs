using Domain.Models.Professions.Crafting;

namespace Services.LL.Interfaces;
public interface ITemperingService
{
    TemperingResult HandleTempering(CraftingQueueItem current, Random rng);
}