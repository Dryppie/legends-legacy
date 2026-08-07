using Microsoft.Extensions.Configuration;
using Services.LL.Dungeons;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class DungeonSigilAssemblySettingsProviderTests
{
    [Fact]
    public void Committed_dungeon_sigil_assembly_settings_are_valid()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var provider = new JsonDungeonSigilAssemblySettingsProvider(
            new ConfigurationBuilder().Build(),
            apiRoot,
            options);

        var settings = provider.GetSettings();

        Assert.True(settings.Enabled);
        Assert.Equal(10, settings.FragmentCost);
    }
}
