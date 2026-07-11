using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Colosseum;

public sealed class JsonChampionMarketCatalog : IChampionMarketCatalog
{
    private readonly IReadOnlyList<ChampionMarketItem> _items;

    public JsonChampionMarketCatalog(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var path = Path.Combine(contentRootPath, contentRoot, "market", "champion-market.json");
        var document = JsonSerializer.Deserialize<ChampionMarketDocument>(
            File.ReadAllText(path),
            options) ?? new();

        ThrowIfInvalid(document.Items);
        _items = document.Items;
    }

    public IReadOnlyList<ChampionMarketItem> GetAll() => _items;

    public ChampionMarketItem? GetById(string itemId) =>
        _items.FirstOrDefault(x => x.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));

    private static void ThrowIfInvalid(IReadOnlyList<ChampionMarketItem> items)
    {
        var duplicateIds = items
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw new InvalidOperationException("Duplicate Champion's Market item ids: " + string.Join(", ", duplicateIds));
        }

        var missingRequiredFields = items
            .Where(x =>
                string.IsNullOrWhiteSpace(x.Id) ||
                string.IsNullOrWhiteSpace(x.Name) ||
                string.IsNullOrWhiteSpace(x.Category))
            .Select(x => x.Id)
            .ToList();

        if (missingRequiredFields.Count > 0)
        {
            throw new InvalidOperationException("Champion's Market items require non-empty ids, names, and categories.");
        }

        var invalidCosts = items
            .Where(x => x.GloryCost < 0 || x.CindersGranted < 0 || x.SoulstonesGranted < 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidCosts.Count > 0)
        {
            throw new InvalidOperationException("Champion's Market item costs and grants must be zero or greater: " + string.Join(", ", invalidCosts));
        }

        var invalidLimits = items
            .Where(x => x.WeeklyPurchaseLimit <= 0 || x.LifetimePurchaseLimit <= 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidLimits.Count > 0)
        {
            throw new InvalidOperationException("Champion's Market purchase limits must be greater than zero when set: " + string.Join(", ", invalidLimits));
        }
    }

    private sealed class ChampionMarketDocument
    {
        public List<ChampionMarketItem> Items { get; set; } = [];
    }
}
