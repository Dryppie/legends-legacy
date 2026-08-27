namespace LegendsLegacy.Balance;

public static class BalancePathLocator
{
    private static readonly string[] RequiredProductionFiles =
    [
        Path.Combine("Data", "combat", "abilities.json"),
        Path.Combine("Data", "combat", "statuses.json"),
        Path.Combine("Data", "combat", "summons.json"),
        Path.Combine("Data", "essences", "essences.json")
    ];

    public static string FindApiContentRoot(string? configuredRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var explicitRoot = Path.GetFullPath(configuredRoot);
            if (ContainsProductionData(explicitRoot))
                return explicitRoot;

            throw new DirectoryNotFoundException(
                $"Content root '{explicitRoot}' does not contain the required production combat and Essence data.");
        }

        foreach (var startingPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startingPath);
            while (directory is not null)
            {
                foreach (var candidate in new[]
                {
                    directory.FullName,
                    Path.Combine(directory.FullName, "src", "API", "API.LL"),
                    Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
                })
                {
                    if (ContainsProductionData(candidate))
                        return Path.GetFullPath(candidate);
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate LL/src/API/API.LL/Data. Pass --content-root with the API.LL directory.");
    }

    public static string FindRepositoryRoot(string startingPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startingPath));
        while (directory is not null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
                return directory.FullName;

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool ContainsProductionData(string path) =>
        RequiredProductionFiles.All(relativePath => File.Exists(Path.Combine(path, relativePath)));
}
