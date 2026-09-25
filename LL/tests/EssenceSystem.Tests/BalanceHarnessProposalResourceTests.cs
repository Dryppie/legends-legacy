using BalanceHarness;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProposalResourceTests
{
    [Theory]
    [InlineData(null, 9600, 1200)]
    [InlineData(S.ResourceV1, 9600, 1200)]
    [InlineData(S.ResourceV2, 9000, 1800)]
    public void Envelope_is_fixed_and_keeps_the_scientific_total(string? version, int native, int audit)
    {
        var limits = S.Resources(version);
        Assert.Equal(native, limits.NativeSeconds); Assert.Equal(audit, limits.AuditSeconds);
        Assert.Equal(10800, limits.NativeSeconds + limits.AuditSeconds);
        var start = DateTimeOffset.UtcNow;
        var launch = new ExplorationLaunch(S.Version, new string('a', 64), start, start.AddSeconds(native), start.AddSeconds(10800),
            10800, 6442450944, native, 5905580032, 1, "suspended-owned-job-v1");
        S.ValidateLaunch(launch, new string('a', 64), version);
        foreach (var bad in new[] { launch with { NativeMaximumSeconds = native + 1 },
            launch with { NativeDeadline = start.AddSeconds(native + 1) }, launch with { Deadline = start.AddSeconds(10801) },
            launch with { NativeMaximumBytes = 5905580033 }, launch with { MaximumBytes = 6442450945 } })
            Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(bad, new string('a', 64), version));
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch, new string('b', 64), version));
    }

    [Theory]
    [InlineData("")]
    [InlineData("tower-proposal-resource-envelope-v3")]
    [InlineData("9000/1800")]
    public void Unknown_envelope_cannot_fall_back_to_latest(string version) =>
        Assert.Throws<InvalidDataException>(() => S.Resources(version));

    [Fact]
    public void Removing_or_changing_version_cannot_reinterpret_an_amended_launch()
    {
        var start = DateTimeOffset.UtcNow;
        var launch = new ExplorationLaunch(S.Version, new string('a', 64), start, start.AddSeconds(9000), start.AddSeconds(10800),
            10800, 6442450944, 9000, 5905580032, 1, "suspended-owned-job-v1");
        foreach (var version in new string?[] { null, S.ResourceV1 })
            Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch, new string('a', 64), version));
        var old = launch with { NativeMaximumSeconds = 9600, NativeDeadline = start.AddSeconds(9600) };
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(old, new string('a', 64), S.ResourceV2));
    }
}
