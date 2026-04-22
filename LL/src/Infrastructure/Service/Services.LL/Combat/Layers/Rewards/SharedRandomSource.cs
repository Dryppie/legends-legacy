using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class SharedRandomSource : IRandomSource
{
    public double NextDouble() => Random.Shared.NextDouble();
}
