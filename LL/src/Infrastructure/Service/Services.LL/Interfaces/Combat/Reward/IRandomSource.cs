namespace Services.LL.Interfaces.Combat.Reward;

public interface IRandomSource
{
    double NextDouble();
}

public interface IResolutionRandomSource : IRandomSource
{
    IDisposable UseSeed(int seed);
    Guid NextGuid();
    int NextInt(int exclusiveMaximum);
}
