using Domain.Models.Items;

namespace Application.Interfaces.Services.LL.Professions;

public interface IItemQualityRollService
{
    ItemQuality RollQuality(string recipeId, int masteryLevel, Random rng);
    IReadOnlyDictionary<ItemQuality, double> GetQualityChances(int masteryLevel);
}
