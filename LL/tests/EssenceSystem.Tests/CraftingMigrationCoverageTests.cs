namespace EssenceSystem.Tests;

public sealed class CraftingMigrationCoverageTests
{
    [Fact]
    public void MigrationPreservesCraftedItemsAndCollapsesLegacyUnlockDuplicates()
    {
        var root = FindRepositoryRoot();
        var migration = Directory
            .GetFiles(
                Path.Combine(root, "LL", "src", "Infrastructure", "Persistence", "Persistence.LL", "Migrations"),
                "*_EquipmentRecipesReusableBlueprintsDirectedTempering.cs")
            .Single();
        var source = File.ReadAllText(migration);

        Assert.Contains("UPDATE \"ItemInstances\"", source);
        Assert.Contains("WHEN \"ItemBaseId\" = 'heavy_helm'", source);
        Assert.Contains("'recipe.armor.head.heavy_helm'", source);
        Assert.Contains("DELETE FROM \"CharacterRecipeUnlocks\" duplicate", source);
        Assert.Contains("duplicate.ctid > retained.ctid", source);
        Assert.Contains("IX_CharacterRecipeUnlocks_CharacterId_BlueprintId", source);
        Assert.DoesNotContain("DropColumn(\n                name: \"BlueprintId\"", source);
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
