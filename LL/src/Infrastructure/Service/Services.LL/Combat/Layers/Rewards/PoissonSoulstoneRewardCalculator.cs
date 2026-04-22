using Microsoft.Extensions.Options;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class PoissonSoulstoneRewardCalculator : ISoulstoneRewardCalculator
{
    private readonly IRandomSource _random;
    private readonly SoulstoneRewardOptions _options;

    public PoissonSoulstoneRewardCalculator(
        IRandomSource random,
        IOptions<SoulstoneRewardOptions> options)
    {
        _random = random;
        _options = options.Value;
    }

    public int Calculate(
        int durationInSeconds,
        double dropRatePercent,
        double doubleDropChancePercent)
    {
        if (durationInSeconds <= 0)
        {
            return 0;
        }

        if (_options.BaseDropRatePerSecond <= 0)
        {
            throw new InvalidOperationException(
                $"{nameof(SoulstoneRewardOptions.BaseDropRatePerSecond)} must be greater than 0.");
        }

        var effectiveRatePerSecond =
            _options.BaseDropRatePerSecond * (1d + (dropRatePercent / 100d));

        var expectedDrops = durationInSeconds * effectiveRatePerSecond;

        if (expectedDrops <= 0)
        {
            return 0;
        }

        var earned = SamplePoisson(expectedDrops);

        if (earned <= 0)
        {
            return 0;
        }

        var doubleRoll = _random.NextDouble();
        var doubleDropChance = doubleDropChancePercent / 100d;

        if (doubleRoll <= doubleDropChance)
        {
            earned *= 2;
        }

        return earned;
    }

    private int SamplePoisson(double lambda)
    {
        // Knuth's algorithm is fine for small/medium lambda.
        // If you later allow huge offline windows, replace this with a better sampler.
        var l = Math.Exp(-lambda);
        var k = 0;
        var p = 1d;

        do
        {
            k++;
            p *= _random.NextDouble();
        }
        while (p > l);

        return k - 1;
    }
}