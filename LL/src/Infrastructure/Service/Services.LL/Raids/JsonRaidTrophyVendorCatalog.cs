using System.Text.Json;
using Application.Interfaces.Services.LL.Raids;
using Domain.Models.Raids;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Raids;

public sealed class JsonRaidTrophyVendorCatalog : IRaidTrophyVendorCatalog
{
    private readonly IReadOnlyList<RaidTrophyVendorItemDefinition> items;
    private readonly IReadOnlyDictionary<string, RaidTrophyVendorItemDefinition> byId;

    public JsonRaidTrophyVendorCatalog(
        IConfiguration configuration,
        string contentRootPath,
        JsonSerializerOptions jsonOptions)
    {
        var contentRoot = configuration["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "raids", "trophy-vendor.json");
        var document = JsonSerializer.Deserialize<RaidTrophyVendorCatalogDocument>(
            File.ReadAllText(path),
            jsonOptions) ?? throw new InvalidOperationException("Raid Trophy vendor content could not be loaded.");
        items = document.Items;
        Validate(items);
        byId = items.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<RaidTrophyVendorItemDefinition> GetForBoss(string raidBossId) =>
        items.Where(x => x.IsEnabled && x.RaidBossId.Equals(raidBossId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public RaidTrophyVendorItemDefinition? Get(string itemId) => byId.GetValueOrDefault(itemId);

    private static void Validate(IReadOnlyList<RaidTrophyVendorItemDefinition> definitions)
    {
        var duplicate = definitions.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate raid Trophy vendor item '{duplicate.Key}'.");

        foreach (var item in definitions)
        {
            if (string.IsNullOrWhiteSpace(item.Id)
                || string.IsNullOrWhiteSpace(item.RaidBossId)
                || string.IsNullOrWhiteSpace(item.Name)
                || string.IsNullOrWhiteSpace(item.Category)
                || string.IsNullOrWhiteSpace(item.RewardItemId)
                || item.TrophyCost <= 0
                || item.RewardQuantity <= 0
                || item.RequiredTier <= 0
                || item.WeeklyPurchaseLimit is <= 0
                || item.LifetimePurchaseLimit is <= 0)
                throw new InvalidOperationException($"Raid Trophy vendor item '{item.Id}' is invalid.");
        }
    }
}
