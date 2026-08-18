using API.LL.Benchmarking;

namespace EssenceSystem.Tests;

public sealed class IdleCombatBenchmarkTests
{
    [Fact]
    public void Fixed_time_provider_normalizes_and_returns_the_configured_instant()
    {
        var configured = DateTimeOffset.Parse("2026-08-18T14:00:00+02:00");
        var provider = new FixedTimeProvider(configured);

        Assert.Equal(DateTimeOffset.Parse("2026-08-18T12:00:00Z"), provider.GetUtcNow());
    }
}
