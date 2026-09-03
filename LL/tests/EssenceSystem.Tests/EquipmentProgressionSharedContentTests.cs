using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Prophecies;
using Microsoft.Extensions.Configuration;
using Services.LL.Prophecies;
namespace EssenceSystem.Tests;
public sealed class EquipmentProgressionSharedContentTests
{
    internal static string ApiRoot() => TestContentPaths.FindApiRoot();
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    [Fact]
    public void Current_prophecies_fill_every_slot_without_profession_objectives()
    {
        var definitions = new JsonProphecyDefinitionProvider(new ConfigurationBuilder().Build(), ApiRoot(), Json).GetAll();
        Assert.DoesNotContain(definitions, x => x.ObjectiveType is "GatherResources" or "TemperItems" or "SpendPotential");
        foreach (var level in new[] { 1, 5, 30, 45, 50, 100 })
        foreach (var slot in Enum.GetValues<ProphecySlotType>())
        {
            var scope = slot == ProphecySlotType.Greater ? ProphecyScope.Weekly : ProphecyScope.Daily;
            Assert.NotNull(ProphecyOfferSelector.Pick(definitions, scope, slot, Guid.Empty, DateTimeOffset.UnixEpoch, "coverage", characterLevel: level));
        }
    }
    [Fact]
    public void Authored_rewards_cannot_reintroduce_removed_items()
    {
        var root = Path.Combine(ApiRoot(), "Data");
        using var itemDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "items/items.json")));
        var items = itemDoc.RootElement.EnumerateArray().ToArray();
        var ids = items.Select(x => x.GetProperty("id").GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var cacheDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "prophecies/caches.json")));
        ids.UnionWith(cacheDoc.RootElement.GetProperty("caches").EnumerateArray().Select(x => x.GetProperty("itemId").GetString()!));
        Assert.DoesNotContain(items, x => x.TryGetProperty("equipmentType", out var t) && t.GetString() == "Tool");
        Assert.DoesNotContain("item.catalyst_selection_crate", ids);
        foreach (var folder in new[] { "rewards", "dungeons", "world", "quests", "raids", "guilds", "market", "prophecies", "event-quests" })
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, folder), "*.json", SearchOption.AllDirectories))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            Visit(doc.RootElement, file);
        }
        void Visit(JsonElement node, string file)
        {
            if (node.ValueKind == JsonValueKind.Array) { foreach (var child in node.EnumerateArray()) Visit(child, file); }
            if (node.ValueKind != JsonValueKind.Object) return;
            foreach (var prop in node.EnumerateObject())
            {
                Assert.DoesNotContain(prop.Name, new[] { "gatheringNodes", "gatheringBonusRewardTableIds", "requiresEquipmentProgression", "requiredQuestVersions" });
                if (prop.Name is "itemId" or "rewardItemId" && prop.Value.ValueKind == JsonValueKind.String)
                    Assert.True(ids.Contains(prop.Value.GetString()!), $"{file}: missing item {prop.Value}");
                Visit(prop.Value, file);
            }
        }
    }
}
