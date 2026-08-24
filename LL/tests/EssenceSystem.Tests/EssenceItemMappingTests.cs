using System.Text.Json;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Items;

namespace EssenceSystem.Tests;

public sealed class EssenceItemMappingTests
{
    [Fact]
    public void Every_real_essence_has_exactly_one_resolved_item_mapping()
    {
        var dataRoot = Path.Combine(FindApiContentRoot(), "Data");
        using var essenceDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(dataRoot, "essences", "essences.json")));
        using var itemDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(dataRoot, "items", "items.json")));

        var definitionIds = essenceDocument.RootElement
            .GetProperty("essences")
            .EnumerateArray()
            .Select(essence => essence.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mappings = itemDocument.RootElement
            .EnumerateArray()
            .Where(item =>
                item.GetProperty("itemType").GetString()!.Equals("Essence", StringComparison.OrdinalIgnoreCase))
            .Select(item => new
            {
                ItemId = item.GetProperty("id").GetString()!,
                DefinitionId = EssenceItemBase.ResolveDefinitionId(
                    item.GetProperty("id").GetString(),
                    item.TryGetProperty("essenceDefinitionId", out var explicitDefinitionId)
                        ? explicitDefinitionId.GetString()
                        : null)
            })
            .ToList();

        Assert.Equal(70, definitionIds.Count);
        Assert.Equal(definitionIds.Count, mappings.Count);
        Assert.DoesNotContain(mappings, mapping => string.IsNullOrWhiteSpace(mapping.DefinitionId));
        Assert.DoesNotContain(
            mappings.GroupBy(mapping => mapping.DefinitionId, StringComparer.OrdinalIgnoreCase),
            group => group.Count() > 1);
        Assert.Equal(
            definitionIds.Order(StringComparer.OrdinalIgnoreCase),
            mappings.Select(mapping => mapping.DefinitionId).Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Repository_resolves_explicit_and_conventional_mappings()
    {
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(
            CreateEssenceItem("item.essence.goblin"),
            CreateEssenceItem("item.custom.variant", "essence.custom"));
        await db.SaveChangesAsync();

        var mappings = await new ItemBaseRepository(db)
            .GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken.None);

        Assert.Equal("item.essence.goblin", mappings["essence.goblin"]);
        Assert.Equal("item.custom.variant", mappings["essence.custom"]);
    }

    [Fact]
    public async Task Repository_rejects_duplicate_resolved_mappings()
    {
        await using var db = CreateDbContext();
        db.ItemBases.AddRange(
            CreateEssenceItem("item.essence.goblin"),
            CreateEssenceItem("item.custom.goblin", "essence.goblin"));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ItemBaseRepository(db)
                .GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken.None));

        Assert.Contains("essence.goblin", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("item.essence.goblin", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("item.custom.goblin", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static EssenceItemBase CreateEssenceItem(
        string itemId,
        string essenceDefinitionId = "") =>
        new()
        {
            Id = itemId,
            Name = itemId,
            ItemType = ItemType.Essence,
            EssenceDefinitionId = essenceDefinitionId
        };

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var apiPath in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (File.Exists(Path.Combine(apiPath, "Data", "essences", "essences.json")))
                    return apiPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data.");
    }
}
