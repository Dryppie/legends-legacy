namespace EssenceSystem.Tests;

public sealed class CraftingMigrationCoverageTests
{
    [Fact]
    public void BaseMigrationIncludesRecipeScopedBlueprintUnlocks()
    {
        var root = FindRepositoryRoot();
        var migration = Directory
            .GetFiles(
                Path.Combine(root, "LL", "src", "Infrastructure", "Persistence", "Persistence.LL", "Migrations"),
                "*_BaseMigration.cs")
            .Single();
        var source = File.ReadAllText(migration);

        Assert.Contains("name: \"CharacterRecipeUnlocks\"", source);
        Assert.Contains("RecipeId = table.Column<string>", source);
        Assert.Contains("nullable: true", source);
        Assert.Contains("IX_CharacterRecipeUnlocks_CharacterId_RecipeId_BlueprintId", source);
        Assert.Contains("columns: new[] { \"CharacterId\", \"RecipeId\", \"BlueprintId\" }", source);
        Assert.Contains("unique: true", source);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "LL", "src")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
