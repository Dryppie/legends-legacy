using Application.Interfaces.Services.LL.Colosseum;
using Domain.Models.Colosseum;
using Microsoft.Extensions.Configuration;
using Services.LL.Guilds;
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

    public IReadOnlyList<ChampionMarketItem> GetActive(DateTimeOffset now)
    {
        var weekKey = ArenaCalendar
            .GetCurrentWeeklyResetStart(now)
            .ToString("yyyyMMdd");
        var fixedItems = _items.Where(x => x.IsEnabled && !x.RotatesWeekly);
        var rotatingItems = _items
            .Where(x => x.IsEnabled && x.RotatesWeekly)
            .GroupBy(x => x.RotationGroup!, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => GuildContentHelpers.PickWeeklyRotation(
                group,
                weekKey,
                count: 1,
                x => x.Id));

        return fixedItems
            .Concat(rotatingItems)
            .OrderBy(x => x.SortOrder)
            .ToList();
    }

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
            .Where(x =>
                x.GloryCost < 0 ||
                x.CindersGranted < 0 ||
                x.SoulstonesGranted < 0 ||
                x.SigilFragmentsGranted < 0 ||
                x.RewardItemQuantity < 0)
            .Select(x => x.Id)
            .ToList();

        if (invalidCosts.Count > 0)
        {
            throw new InvalidOperationException("Champion's Market item costs and grants must be zero or greater: " + string.Join(", ", invalidCosts));
        }

        var invalidItemRewards = items
            .Where(x =>
                (x.RewardItemQuantity > 0 && string.IsNullOrWhiteSpace(x.RewardItemId)) ||
                (x.RewardItemQuantity == 0 && !string.IsNullOrWhiteSpace(x.RewardItemId)))
            .Select(x => x.Id)
            .ToList();

        if (invalidItemRewards.Count > 0)
        {
            throw new InvalidOperationException("Champion's Market inventory rewards require both an item id and a positive quantity: " + string.Join(", ", invalidItemRewards));
        }

        var invalidRotations = items
            .Where(x => x.RotatesWeekly && string.IsNullOrWhiteSpace(x.RotationGroup))
            .Select(x => x.Id)
            .ToList();

        if (invalidRotations.Count > 0)
        {
            throw new InvalidOperationException("Rotating Champion's Market items require a rotation group: " + string.Join(", ", invalidRotations));
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
