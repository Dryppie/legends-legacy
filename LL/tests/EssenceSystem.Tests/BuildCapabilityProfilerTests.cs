using LegendsLegacy.Balance;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class BuildCapabilityProfilerTests
{
    [Fact]
    public void Specialist_fixture_normalizes_relative_strength_without_collapsing_dimensions()
    {
        var normalized = BuildCapabilityNormalization.NormalizePercentiles(
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["defensive"] = 10,
                ["balanced"] = 50,
                ["damage-specialist"] = 100
            });

        Assert.Equal(0, normalized["defensive"]);
        Assert.Equal(50, normalized["balanced"]);
        Assert.Equal(100, normalized["damage-specialist"]);
    }

    [Fact]
    public void Tied_specialists_receive_the_same_midrank()
    {
        var normalized = BuildCapabilityNormalization.NormalizePercentiles(
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["control-specialist"] = 75,
                ["cleanse-specialist"] = 75,
                ["baseline"] = 25
            });

        Assert.Equal(0, normalized["baseline"]);
        Assert.Equal(75, normalized["control-specialist"]);
        Assert.Equal(75, normalized["cleanse-specialist"]);
    }

    [Fact]
    public void Persistent_probe_cache_round_trips_support_and_wave_measurements()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"capability-cache-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "probes.json");
        try
        {
            var document = new CapabilityProbeCacheDocument
            {
                SupportProbes = new Dictionary<string, CapabilitySupportProbeCacheEntry>(StringComparer.Ordinal)
                {
                    ["support-key"] = new(600, 150, 75, true, 0.8, 2, 1, 0, 0, 0, 0, 5)
                },
                WaveProbes = new Dictionary<string, CapabilityWaveProbeCacheEntry>(StringComparer.Ordinal)
                {
                    ["wave-key"] = new(900, 5_000, 9, 9, 3, true)
                }
            };

            CapabilityProbeCacheStore.Save(path, document);
            var loaded = Assert.IsType<CapabilityProbeCacheDocument>(CapabilityProbeCacheStore.Load(path));

            Assert.Equal(document.SupportProbes["support-key"], loaded.SupportProbes["support-key"]);
            Assert.Equal(document.WaveProbes["wave-key"], loaded.WaveProbes["wave-key"]);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Persistent_probe_cache_ignores_an_unknown_schema()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"capability-cache-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "probes.json");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { schemaVersion = 999 }));

            Assert.Null(CapabilityProbeCacheStore.Load(path));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void Capability_options_reject_out_of_range_seed_panels(int seedCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BuildCapabilityOptions(seedCount).Validate());
    }
}
