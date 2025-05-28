using Domain.Models.Soulstones.UpgradeDefinition;

namespace Domain.Extensions.Soulstones;
public static class CostCurveExtensions
{
    /// cost of *that* level (1-based)
    public static int CostOfLevel(this CostCurve c, int level)
    {
        if (level <= 0) throw new ArgumentOutOfRangeException(nameof(level));
        // simple linear
        if (c.IncrementCap is null)
            return c.Base + (level - 1) * c.Increment;

        // capped increment - e.g. incremental diff grows +1 each level until it hits the cap
        var cap = c.IncrementCap.Value;
        if (level <= cap) return level;                      // 1,2,3,…,cap

        return cap;                                          // flat cost once cap reached
    }

    /// total to reach *targetLevel* from 0
    public static int TotalCost(this CostCurve c, int targetLevel)
    {
        var total = 0;
        for (var lvl = 1; lvl <= targetLevel; lvl++)
            total += c.CostOfLevel(lvl);
        return total;
    }
}
