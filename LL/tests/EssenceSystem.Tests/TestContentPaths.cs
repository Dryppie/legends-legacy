namespace EssenceSystem.Tests;

internal static class TestContentPaths
{
    public static string FindApiRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("LL_TEST_API_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot)
            && Directory.Exists(Path.Combine(configuredRoot, "Data")))
        {
            return configuredRoot;
        }

        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startPath);
            while (current is not null)
            {
                foreach (var relativePath in new[]
                         {
                             Path.Combine("src", "API", "API.LL"),
                             Path.Combine("LL", "src", "API", "API.LL")
                         })
                {
                    var candidate = Path.Combine(current.FullName, relativePath);
                    if (Directory.Exists(Path.Combine(candidate, "Data")))
                        return candidate;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the API.LL content root.");
    }
}
