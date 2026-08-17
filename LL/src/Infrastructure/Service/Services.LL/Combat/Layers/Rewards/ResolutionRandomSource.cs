using Application.Interfaces.Services.LL.Essences;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

/// <summary>
/// Scoped random source that can be pinned to a stable action-resolution seed.
/// Outside a resolution scope it retains the existing system-random behavior.
/// </summary>
public sealed class ResolutionRandomSource : IResolutionRandomSource, IRandomProvider
{
    private Random? _current;

    public double NextDouble() => _current?.NextDouble() ?? Random.Shared.NextDouble();

    public Guid NextGuid()
    {
        if (_current is null) return Guid.NewGuid();
        Span<byte> bytes = stackalloc byte[16];
        _current.NextBytes(bytes);
        return new Guid(bytes);
    }

    public int NextInt(int exclusiveMaximum) =>
        _current?.Next(exclusiveMaximum) ?? Random.Shared.Next(exclusiveMaximum);

    public IDisposable UseSeed(int seed)
    {
        var previous = _current;
        _current = new Random(seed);
        return new Scope(this, previous);
    }

    private sealed class Scope(ResolutionRandomSource owner, Random? previous) : IDisposable
    {
        private ResolutionRandomSource? _owner = owner;

        public void Dispose()
        {
            var currentOwner = Interlocked.Exchange(ref _owner, null);
            if (currentOwner is not null)
                currentOwner._current = previous;
        }
    }
}
