using Services.LL.Providers;

namespace EssenceSystem.Tests;

public sealed class SoulstoneUpgradeDefinitionProviderTests
{
    [Fact]
    public void Provider_loads_definitions_from_the_supplied_content_root()
    {
        var contentRoot = Path.Combine(
            Path.GetTempPath(),
            $"ll-soulstone-provider-{Guid.NewGuid():N}");
        var definitionDirectory = Path.Combine(contentRoot, "Data", "progression");

        try
        {
            Directory.CreateDirectory(definitionDirectory);
            File.WriteAllText(
                Path.Combine(definitionDirectory, "soulstone-upgrades.json"),
                "[]");

            using var provider = new SoulstoneUpgradeDefinitionProvider(contentRoot);

            Assert.Empty(provider.All);
        }
        finally
        {
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }
    }
}
