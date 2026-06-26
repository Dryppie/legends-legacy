using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items;

namespace Services.LL.Professions.Craftings;

public class ItemQualityRollService : IItemQualityRollService
{
    private static readonly QualityOdds[] OddsByMastery =
    [
        new(0, 25, 60, 14, 1, 0),
        new(25, 15, 58, 23, 4, 0),
        new(50, 8, 52, 31, 8, 1),
        new(75, 3, 43, 39, 13, 2),
        new(100, 0, 35, 45, 16, 4)
    ];

    public ItemQuality RollQuality(string recipeId, int masteryLevel, Random rng)
    {
        var odds = ResolveOdds(Math.Clamp(masteryLevel, 0, 100));
        var roll = rng.NextDouble() * 100d;

        if (roll < odds.Crude) return ItemQuality.Crude;
        roll -= odds.Crude;
        if (roll < odds.Standard) return ItemQuality.Standard;
        roll -= odds.Standard;
        if (roll < odds.Fine) return ItemQuality.Fine;
        roll -= odds.Fine;
        if (roll < odds.Exceptional) return ItemQuality.Exceptional;

        return ItemQuality.Masterwork;
    }

    private static QualityOdds ResolveOdds(int masteryLevel)
    {
        var lower = OddsByMastery[0];
        var upper = OddsByMastery[^1];

        for (var i = 0; i < OddsByMastery.Length - 1; i++)
        {
            if (masteryLevel < OddsByMastery[i].MasteryLevel ||
                masteryLevel > OddsByMastery[i + 1].MasteryLevel)
                continue;

            lower = OddsByMastery[i];
            upper = OddsByMastery[i + 1];
            break;
        }

        if (lower.MasteryLevel == upper.MasteryLevel) return lower;

        var ratio = (masteryLevel - lower.MasteryLevel) / (double)(upper.MasteryLevel - lower.MasteryLevel);
        return new QualityOdds(
            masteryLevel,
            Lerp(lower.Crude, upper.Crude, ratio),
            Lerp(lower.Standard, upper.Standard, ratio),
            Lerp(lower.Fine, upper.Fine, ratio),
            Lerp(lower.Exceptional, upper.Exceptional, ratio),
            Lerp(lower.Masterwork, upper.Masterwork, ratio));
    }

    private static double Lerp(double from, double to, double ratio) =>
        from + ((to - from) * ratio);

    private sealed record QualityOdds(
        int MasteryLevel,
        double Crude,
        double Standard,
        double Fine,
        double Exceptional,
        double Masterwork);
}
